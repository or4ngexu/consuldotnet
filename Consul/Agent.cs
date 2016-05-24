﻿// -----------------------------------------------------------------------
//  <copyright file="Agent.cs" company="PlayFab Inc">
//    Copyright 2015 PlayFab Inc.
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//  </copyright>
// -----------------------------------------------------------------------

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Consul
{
    /// <summary>
    /// The status of a TTL check
    /// </summary>
    public class TTLStatus : IEquatable<TTLStatus>
    {
        private static readonly TTLStatus passingStatus = new TTLStatus() { Status = "passing", LegacyStatus = "pass" };
        private static readonly TTLStatus warningStatus = new TTLStatus() { Status = "warning", LegacyStatus = "warn" };
        private static readonly TTLStatus criticalStatus = new TTLStatus() { Status = "critical", LegacyStatus = "fail" };

        public string Status { get; private set; }
        internal string LegacyStatus { get; private set; }

        public static TTLStatus Pass
        {
            get { return passingStatus; }
        }

        public static TTLStatus Warn
        {
            get { return warningStatus; }
        }

        public static TTLStatus Critical
        {
            get { return criticalStatus; }
        }

        [Obsolete("Use TTLStatus.Critical instead. This status will be an error in 0.7.0+", false)]
        public static TTLStatus Fail
        {
            get { return criticalStatus; }
        }

        public bool Equals(TTLStatus other)
        {
            return other != null && ReferenceEquals(this, other);
        }

        public override bool Equals(object other)
        {
            // other could be a reference type, the is operator will return false if null
            return other is TTLStatus && Equals(other as TTLStatus);
        }

        public override int GetHashCode()
        {
            return Status.GetHashCode();
        }
    }

    public class TTLStatusConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, ((TTLStatus)value).Status);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var status = (string)serializer.Deserialize(reader, typeof(string));
            switch (status)
            {
                case "pass":
                    return TTLStatus.Pass;
                case "passing":
                    return TTLStatus.Pass;
                case "warn":
                    return TTLStatus.Warn;
                case "warning":
                    return TTLStatus.Warn;
                case "fail":
                    return TTLStatus.Critical;
                case "critical":
                    return TTLStatus.Critical;
                default:
                    throw new ArgumentException("Invalid TTL status value during deserialization");
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TTLStatus);
        }
    }

    /// <summary>
    /// The status of a health check
    /// </summary>
    public class CheckStatus : IEquatable<CheckStatus>
    {
        private static readonly CheckStatus passing = new CheckStatus() { Status = "passing" };
        private static readonly CheckStatus warning = new CheckStatus() { Status = "warning" };
        private static readonly CheckStatus critical = new CheckStatus() { Status = "critical" };
        private static readonly CheckStatus any = new CheckStatus() { Status = "any" };
        private static readonly CheckStatus unknown = new CheckStatus() { Status = "unknown" };

        public string Status { get; private set; }

        public static CheckStatus Passing
        {
            get { return passing; }
        }

        public static CheckStatus Warning
        {
            get { return warning; }
        }

        public static CheckStatus Critical
        {
            get { return critical; }
        }

        public static CheckStatus Any
        {
            get { return any; }
        }

        public static CheckStatus Unknown
        {
            get { return unknown; }
        }

        public bool Equals(CheckStatus other)
        {
            return other != null && ReferenceEquals(this, other);
        }

        public override bool Equals(object other)
        {
            // other could be a reference type, the is operator will return false if null
            return other is CheckStatus && Equals(other as CheckStatus);
        }

        public override int GetHashCode()
        {
            return Status.GetHashCode();
        }
    }

    public class CheckStatusConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, ((CheckStatus)value).Status);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var status = (string)serializer.Deserialize(reader, typeof(string));
            switch (status)
            {
                case "passing":
                    return CheckStatus.Passing;
                case "warning":
                    return CheckStatus.Warning;
                case "critical":
                    return CheckStatus.Critical;
                default:
                    throw new ArgumentException("Invalid Check status value during deserialization");
            }
        }

        public override bool CanConvert(Type objectType)
        {
            if (objectType == typeof(CheckStatus))
            {
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// AgentCheck represents a check known to the agent
    /// </summary>
    public class AgentCheck
    {
        public string Node { get; set; }
        public string CheckID { get; set; }
        public string Name { get; set; }

        [JsonConverter(typeof(CheckStatusConverter))]
        public CheckStatus Status { get; set; }

        public string Notes { get; set; }
        public string Output { get; set; }
        public string ServiceID { get; set; }
        public string ServiceName { get; set; }
    }

    /// <summary>
    /// AgentService represents a service known to the agent
    /// </summary>
    public class AgentService
    {
        public string ID { get; set; }
        public string Service { get; set; }
        public string[] Tags { get; set; }
        public int Port { get; set; }
        public string Address { get; set; }
        public bool EnableTagOverride { get; set; }
    }

    /// <summary>
    /// AgentMember represents a cluster member known to the agent
    /// </summary>
    public class AgentMember
    {
        public string Name { get; set; }
        public string Addr { get; set; }
        public ushort Port { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public int Status { get; set; }
        public byte ProtocolMin { get; set; }
        public byte ProtocolMax { get; set; }
        public byte ProtocolCur { get; set; }
        public byte DelegateMin { get; set; }
        public byte DelegateMax { get; set; }
        public byte DelegateCur { get; set; }

        public AgentMember()
        {
            Tags = new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// AgentServiceRegistration is used to register a new service
    /// </summary>
    public class AgentServiceRegistration
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ID { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[] Tags { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int Port { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Address { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool EnableTagOverride { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public AgentServiceCheck Check { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public AgentServiceCheck[] Checks { get; set; }
    }

    /// <summary>
    /// AgentCheckRegistration is used to register a new check
    /// </summary>
    public class AgentCheckRegistration : AgentServiceCheck
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ID { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Notes { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ServiceID { get; set; }
    }

    /// <summary>
    /// AgentServiceCheck is used to create an associated check for a service
    /// </summary>
    public class AgentServiceCheck
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Script { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string DockerContainerID { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Shell { get; set; } // Only supported for Docker.

        [JsonConverter(typeof(DurationTimespanConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? Interval { get; set; }

        [JsonConverter(typeof(DurationTimespanConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? Timeout { get; set; }

        [JsonConverter(typeof(DurationTimespanConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? TTL { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string HTTP { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string TCP { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(CheckStatusConverter))]
        public CheckStatus Status { get; set; }
    }

    /// <summary>
    /// Agent can be used to query the Agent endpoints
    /// </summary>
    public class Agent : IAgentEndpoint
    {
        private class CheckUpdate
        {
            public string Status { get; set; }
            public string Output { get; set; }
        }
        private readonly ConsulClient _client;

        // cache the node name
        private string _nodeName;

        internal Agent(ConsulClient c)
        {
            _client = c;
        }

        /// <summary>
        /// Self is used to query the agent we are speaking to for information about itself
        /// </summary>
        /// <returns>A somewhat dynamic object representing the various data elements in Self</returns>
        public Task<QueryResult<Dictionary<string, Dictionary<string, dynamic>>>> Self()
        {
            return _client.Get<Dictionary<string, Dictionary<string, dynamic>>>("/v1/agent/self").Execute();
        }

        /// <summary>
        /// NodeName is used to get the node name of the agent
        /// </summary>
        public string NodeName
        {
            get
            {
                if (_nodeName == null)
                {
                    var res = Self().GetAwaiter().GetResult();
                    _nodeName = (string)res.Response["Config"]["NodeName"];
                }
                return _nodeName;
            }
        }

        /// <summary>
        /// Checks returns the locally registered checks
        /// </summary>
        /// <returns>A map of the registered check names and check data</returns>
        public Task<QueryResult<Dictionary<string, AgentCheck>>> Checks()
        {
            return _client.Get<Dictionary<string, AgentCheck>>("/v1/agent/checks").Execute();
        }

        /// <summary>
        /// Services returns the locally registered services
        /// </summary>
        /// <returns>A map of the registered services and service data</returns>
        public Task<QueryResult<Dictionary<string, AgentService>>> Services()
        {
            return _client.Get<Dictionary<string, AgentService>>("/v1/agent/services").Execute();
        }

        /// <summary>
        /// Members returns the known gossip members. The WAN flag can be used to query a server for WAN members.
        /// </summary>
        /// <returns>An array of gossip peers</returns>
        public Task<QueryResult<AgentMember[]>> Members(bool wan)
        {
            var req = _client.Get<AgentMember[]>("/v1/agent/members");
            if (wan)
            {
                req.Params["wan"] = "1";
            }
            return req.Execute();
        }

        /// <summary>
        /// ServiceRegister is used to register a new service with the local agent
        /// </summary>
        /// <param name="service">A service registration object</param>
        /// <returns>An empty write result</returns>
        public Task<WriteResult> ServiceRegister(AgentServiceRegistration service)
        {
            return _client.Put("/v1/agent/service/register", service).Execute();
        }

        /// <summary>
        /// ServiceRegister is used to register a new service with the local agent
        /// </summary>
        /// <param name="serviceID">The service ID</param>
        /// <returns>An empty write result</returns>
        public Task<WriteResult> ServiceDeregister(string serviceID)
        {
            return _client.Put(string.Format("/v1/agent/service/deregister/{0}", serviceID)).Execute();
        }

        /// <summary>
        /// PassTTL is used to set a TTL check to the passing state
        /// </summary>
        /// <param name="checkID">The check ID</param>
        /// <param name="note">An optional, arbitrary string to write to the check status</param>
        public Task PassTTL(string checkID, string note)
        {
            return LegacyUpdateTTL(checkID, note, TTLStatus.Pass);
        }

        /// <summary>
        /// WarnTTL is used to set a TTL check to the warning state
        /// </summary>
        /// <param name="checkID">The check ID</param>
        /// <param name="note">An optional, arbitrary string to write to the check status</param>
        public Task WarnTTL(string checkID, string note)
        {
            return LegacyUpdateTTL(checkID, note, TTLStatus.Warn);
        }

        /// <summary>
        /// FailTTL is used to set a TTL check to the failing state
        /// </summary>
        /// <param name="checkID">The check ID</param>
        /// <param name="note">An optional, arbitrary string to write to the check status</param>
        public Task FailTTL(string checkID, string note)
        {
            return LegacyUpdateTTL(checkID, note, TTLStatus.Critical);
        }

        /// <summary>
        /// UpdateTTL is used to update the TTL of a check
        /// </summary>
        /// <param name="checkID">The check ID</param>
        /// <param name="output">An optional, arbitrary string to write to the check status</param>
        /// <param name="status">The state to set the check to</param>
        /// <returns>An empty write result</returns>
        public Task<WriteResult> UpdateTTL(string checkID, string output, TTLStatus status)
        {
            var u = new CheckUpdate
            {
                Status = status.Status,
                Output = output
            };
            return _client.Put(string.Format("/v1/agent/check/update/{0}", checkID), u).Execute();
        }

        private Task<WriteResult> LegacyUpdateTTL(string checkID, string note, TTLStatus status)
        {
            var request = _client.Put(string.Format("/v1/agent/check/{0}/{1}", status.LegacyStatus, checkID));
            if (!string.IsNullOrEmpty(note))
            {
                request.Params.Add("note", note);
            }
            return request.Execute();
        }

        /// <summary>
        /// CheckRegister is used to register a new check with the local agent
        /// </summary>
        /// <param name="check">A check registration object</param>
        /// <returns>An empty write result</returns>
        public Task<WriteResult> CheckRegister(AgentCheckRegistration check)
        {
            return _client.Put("/v1/agent/check/register", check).Execute();
        }

        /// <summary>
        /// CheckDeregister is used to deregister a check with the local agent
        /// </summary>
        /// <param name="checkID">The check ID to deregister</param>
        /// <returns>An empty write result</returns>
        public Task<WriteResult> CheckDeregister(string checkID)
        {
            return _client.Put(string.Format("/v1/agent/check/deregister/{0}", checkID)).Execute();
        }

        /// <summary>
        /// Join is used to instruct the agent to attempt a join to another cluster member
        /// </summary>
        /// <param name="addr">The address to join to</param>
        /// <param name="wan">Join the WAN pool</param>
        /// <returns>An empty write result</returns>
        public Task<WriteResult> Join(string addr, bool wan)
        {
            var req = _client.Put(string.Format("/v1/agent/join/{0}", addr));
            if (wan)
            {
                req.Params["wan"] = "1";
            }
            return req.Execute();
        }

        /// <summary>
        /// ForceLeave is used to have the agent eject a failed node
        /// </summary>
        /// <param name="node">The node name to remove. An attempt to eject a node that doesn't exist will still be successful</param>
        /// <returns>An empty write result</returns>
        public Task<WriteResult> ForceLeave(string node)
        {
            return _client.Put(string.Format("/v1/agent/force-leave/{0}", node)).Execute();
        }

        /// <summary>
        /// EnableServiceMaintenance toggles service maintenance mode on for the given service ID
        /// </summary>
        /// <param name="serviceID">The service ID</param>
        /// <param name="reason">An optional reason</param>
        /// <returns>An empty write result</returns>
        public Task<WriteResult> EnableServiceMaintenance(string serviceID, string reason)
        {
            var req = _client.Put(string.Format("/v1/agent/service/maintenance/{0}", serviceID));
            req.Params["enable"] = "true";
            req.Params["reason"] = reason;
            return req.Execute();
        }

        /// <summary>
        /// DisableServiceMaintenance toggles service maintenance mode off for the given service ID
        /// </summary>
        /// <param name="serviceID">The service ID</param>
        /// <returns>An empty write result</returns>
        public Task<WriteResult> DisableServiceMaintenance(string serviceID)
        {
            var req = _client.Put(string.Format("/v1/agent/service/maintenance/{0}", serviceID));
            req.Params["enable"] = "false";
            return req.Execute();
        }

        /// <summary>
        /// EnableNodeMaintenance toggles node maintenance mode on for the agent we are connected to
        /// </summary>
        /// <param name="reason">An optional reason</param>
        /// <returns>An empty write result</returns>
        public Task<WriteResult> EnableNodeMaintenance(string reason)
        {
            var req = _client.Put("/v1/agent/maintenance");
            req.Params["enable"] = "true";
            req.Params["reason"] = reason;
            return req.Execute();
        }

        /// <summary>
        /// DisableNodeMaintenance toggles node maintenance mode off for the agent we are connected to
        /// </summary>
        /// <returns>An empty write result</returns>
        public Task<WriteResult> DisableNodeMaintenance()
        {
            var req = _client.Put("/v1/agent/maintenance");
            req.Params["enable"] = "false";
            return req.Execute();
        }
    }

    public partial class ConsulClient : IConsulClient
    {
        private Agent _agent;

        /// <summary>
        /// Agent returns a handle to the agent endpoints
        /// </summary>
        public IAgentEndpoint Agent
        {
            get
            {
                if (_agent == null)
                {
                    lock (_lock)
                    {
                        if (_agent == null)
                        {
                            _agent = new Agent(this);
                        }
                    }
                }
                return _agent;
            }
        }
    }
}
