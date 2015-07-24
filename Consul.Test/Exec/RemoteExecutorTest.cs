using System;
using Consul.Exec;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Consul.Test.Exec
{
    [TestClass]
    public class ExecTest
    {
        [TestMethod]
        public void ExecCommandRun()
        {

            var rexec = new ExecCommand(new RemoteExecutionConfiguration() { Cmd = "echo hi"}, CancellationToken.None);

            rexec.Run();
            Task.Delay(1000).Wait();
        }
    }
}
