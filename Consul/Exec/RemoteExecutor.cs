using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Consul;
using Newtonsoft.Json;
using System.Threading;
using System.IO;

namespace Consul.Exec
{
    // rExecConf is used to pass around configuration
    public class rExecConf
    {
        public string Datacenter { get; set; }
        public string Prefix { get; set; }
        public string Token { get; set; }
        public bool ForeignDC { get; set; }
        public string LocalDC { get; set; }
        public string LocalNode { get; set; }
        public string Node { get; set; }
        public string Service { get; set; }
        public string Tag { get; set; }
        [JsonConverter(typeof(NanoSecTimespanConverter))]
        public TimeSpan Wait { get; set; }
        [JsonConverter(typeof(NanoSecTimespanConverter))]
        public TimeSpan ReplWait { get; set; }
        public string Cmd { get; set; }
        public byte[] Script { get; set; }
        public bool Verbose { get; set; }
        public rExecConf()
        {
            Prefix = RemoteExecutor.rExecPrefix;
            ReplWait = RemoteExecutor.rExecReplicationWait;
            Wait = RemoteExecutor.rExecQuietWait;
            Verbose = false;
        }
    }

    // rExecEvent is the event we broadcast using a user-event
    internal class rExecEvent
    {
        public string Prefix { get; set; }
        public string Session { get; set; }
    }
    // rExecSpec is the file we upload to specify the parameters
    // of the remote execution.
    internal class rExecSpec
    {
        // Command is a single command to run directly in the shell
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Command { get; set; }
        // Script should be spilled to a file and executed
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public byte[] Script { get; set; }
        // Wait is how long we are waiting on a quiet period to terminate
        [JsonConverter(typeof(NanoSecTimespanConverter))]
        public TimeSpan Wait { get; set; }
    }
    // rExecAck is used to transmit an acknowledgement
    internal class rExecAck
    {
        public string Node { get; set; }
    }
    // rExecHeart is used to transmit a heartbeat
    internal class rExecHeart
    {
        public string Node { get; set; }
    }
    // rExecOutput is used to transmit a chunk of output
    internal class rExecOutput
    {
        public string Node { get; set; }
        public byte[] Output { get; set; }
    }
    // rExecExit is used to transmit an exit code
    internal class rExecExit
    {
        public string Node { get; set; }
        public int Code { get; set; }
    }
    // ExecCommand is a Command implementation that is used to
    // do remote execution of commands
    public class ExecCommand
    {
        CancellationTokenSource cts;
        public CancellationToken Shutdown { get; private set; }
        public rExecConf Conf { get; private set; }
        Client client { get; set; }
        public string SessionID { get; private set; }
        public ExecCommand(rExecConf config, CancellationToken ct) :
            this(config, new Client(), ct) { }
        public ExecCommand(rExecConf config, Client client, CancellationToken ct)
        {
            Conf = config;
            this.client = client;
            Shutdown = ct;
            validate(config);
        }

        public void Run()
        {
            using (cts = CancellationTokenSource.CreateLinkedTokenSource(Shutdown))
            {
                try
                {
                    var selfRequest = client.Agent.Self();
                    var info = selfRequest.Response;
                    var didUpload = false;

                    if (!string.IsNullOrEmpty(Conf.Datacenter) && Conf.Datacenter != info["Config"]["Datacenter"].ToString())
                    {
                        // Remote exec in foreign datacenter, using Session TTL
                        Conf.ForeignDC = true;
                        Conf.LocalDC = info["Config"]["Datacenter"].ToString();
                        Conf.LocalNode = info["Config"]["NodeName"].ToString();
                    }
                    var spec = JsonConvert.SerializeObject(new rExecSpec() { Command = Conf.Cmd, Script = Conf.Script, Wait = Conf.Wait });
                    try
                    {
                        SessionID = createSession();

                        didUpload = uploadPayload(System.Text.Encoding.UTF8.GetBytes(spec));

                        Task.Delay(Conf.ReplWait, cts.Token).Wait();

                        var eventId = fireEvent();

                        waitForJob();
                    }
                    finally
                    {
                        if (!string.IsNullOrEmpty(SessionID))
                        {
                            client.Session.Destroy(SessionID);
                        }
                        if (didUpload)
                        {
                            client.KV.DeleteTree(string.Join("/", Conf.Prefix, SessionID));
                        }
                    }
                }
                finally
                {
                    cts.Cancel();
                }
            }
        }

        private void waitForJob()
        {
            throw new NotImplementedException();
        }

        private string fireEvent()
        {
            var msg = new rExecEvent()
            {
                Prefix = Conf.Prefix,
                Session = SessionID
            };
            var buf = JsonConvert.SerializeObject(msg);
            var ev = new UserEvent()
            {
                Name = "_rexec",
                Payload = System.Text.Encoding.UTF8.GetBytes(buf),
                NodeFilter = Conf.Node,
                ServiceFilter = Conf.Service,
                TagFilter = Conf.Tag
            };
            return client.Event.Fire(ev).Response;
        }

        private bool uploadPayload(byte[] spec)
        {
            var pair = new KVPair(string.Join("/", Conf.Prefix, SessionID, RemoteExecutor.rExecFileName))
            {
                Value = spec,
                Session = SessionID
            };

            if (!client.KV.Acquire(pair).Response)
            {
                throw new LockNotHeldException("failed to acquire key " + pair.Key);
            }
            return true;
        }

        private string createSession()
        {
            string id;
            if (Conf.ForeignDC)
            {
                id = createSessionForeign();
            }
            else
            {
                id = createSessionLocal();
            }
            client.Session.RenewPeriodic(RemoteExecutor.rExecTTL, id, cts.Token);
            return id;
        }

        private string createSessionLocal()
        {
            var se = new SessionEntry
            {
                Name = "Remote Exec",
                Behavior = SessionBehavior.Delete,
                TTL = RemoteExecutor.rExecTTL
            };
            return client.Session.Create(se).Response;
        }

        private string createSessionForeign()
        {
            var services = client.Health.Service("consul", "", true).Response;
            if (services.Length == 0)
            {
                throw new InvalidOperationException("Failed to find Consul server in remote datacenter");
            }
            var node = services[0].Node.Name;
            var se = new SessionEntry
            {
                Name = string.Format("Remote Exec via {0}@{1}", Conf.LocalNode, Conf.LocalDC),
                Node = node,
                Behavior = SessionBehavior.Delete,
                TTL = RemoteExecutor.rExecTTL
            };
            return client.Session.CreateNoChecks(se).Response;
        }

        private void validate(rExecConf config)
        {
            var exceptions = new List<Exception>();
            if ((config.Script == null || config.Script.Length == 0) && string.IsNullOrEmpty(config.Cmd))
            {
                exceptions.Add(new ArgumentException("Must specify a command to execute", "config"));
            }

            if ((!string.IsNullOrEmpty(config.Service) && string.IsNullOrEmpty(config.Tag)) || (!string.IsNullOrEmpty(config.Tag) && string.IsNullOrEmpty(config.Service)))
            {
                exceptions.Add(new ArgumentException("Cannot provide tag filter without service filter", "config"));
            }

            if (!string.IsNullOrEmpty(config.Node))
            {
                try
                {
                    new Regex(config.Node);
                }
                catch (Exception ex)
                {
                    exceptions.Add(new ArgumentException("Failed to compile node filter regexp", "config", ex));
                }
            }

            if (!string.IsNullOrEmpty(config.Service))
            {
                try
                {
                    new Regex(config.Service);
                }
                catch (Exception ex)
                {
                    exceptions.Add(new ArgumentException("Failed to compile service filter regexp", "config", ex));
                }
            }

            if (!string.IsNullOrEmpty(config.Tag))
            {
                try
                {
                    new Regex(config.Tag);
                }
                catch (Exception ex)
                {
                    exceptions.Add(new ArgumentException("Failed to compile tag filter regexp", "config", ex));
                }
            }
            if (exceptions.Count > 0)
            {
                throw new AggregateException("Remote execution configuration validation failed", exceptions);
            }
        }
    }

    public class RemoteExecutor
    {
        // rExecPrefix is the prefix in the KV store used to
        // store the remote exec data
        internal const string rExecPrefix = "_rexec";
        // rExecFileName is the name of the file we append to
        // the path, e.g. _rexec/session_id/job
        internal const string rExecFileName = "job";
        // rExecAck is the suffix added to an ack path
        internal const string rExecAckSuffix = "/ack";
        // rExecAck is the suffix added to an exit code
        internal const string rExecExitSuffix = "/exit";
        // rExecOutputDivider is used to namespace the output
        internal const string rExecOutputDivider = "/out/";
        // rExecReplicationWait is how long we wait for replication
        internal static readonly TimeSpan rExecReplicationWait = TimeSpan.FromMilliseconds(200);
        // rExecQuietWait is how long we wait for no responses
        // before assuming the job is done.
        internal static readonly TimeSpan rExecQuietWait = TimeSpan.FromSeconds(2);
        // rExecTTL is how long we default the session TTL to
        internal static readonly TimeSpan rExecTTL = TimeSpan.FromSeconds(15);
        // rExecRenewInterval is how often we renew the session TTL
        // when doing an exec in a foreign DC.
        internal static readonly TimeSpan rExecRenewInterval = TimeSpan.FromSeconds(5);
    }
}
