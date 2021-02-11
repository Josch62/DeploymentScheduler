using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.Diagnostics;
using System.Security.Principal;
using System.Security.AccessControl;

namespace SchedulerCommon.Pipes
{
    public class PipeServer
    {
        public event EventHandler<PipeEventArg> PipeMessage;

        private string _pipeName;

        public void Listen(string pipeName)
        {
            try
            {
                // Set to class level var so we can re-use in the async callback method
                _pipeName = pipeName;
                // Create the new async pipe
                var pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.In, 10, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 8192, 256, GetSecurity());

                // Wait for a connection
                pipeServer.BeginWaitForConnection(new AsyncCallback(WaitForConnectionCallBack), pipeServer);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void WaitForConnectionCallBack(IAsyncResult iar)
        {
            try
            {
                // Get the pipe
                var pipeServer = (NamedPipeServerStream)iar.AsyncState;
                // End waiting for the connection
                pipeServer.EndWaitForConnection(iar);

                var buffer = new byte[255];

                // Read the incoming message
                pipeServer.Read(buffer, 0, 255);

                // Convert byte buffer to string
                var stringData = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

                // Pass message back to calling form
                PipeMessage?.Invoke(this, new PipeEventArg { Message = stringData.Trim() });

                // Kill original sever and create new wait server
                pipeServer.Close();
                pipeServer = null;
                // pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.In, 10, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 256, 256, GetSecurity());
                // Recursively wait for the connection again and again....
                pipeServer.BeginWaitForConnection(new AsyncCallback(WaitForConnectionCallBack), pipeServer);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private PipeSecurity GetSecurity()
        {
            var ps = new PipeSecurity();
            var usersSid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            var createrownerSid = new SecurityIdentifier(WellKnownSidType.CreatorOwnerSid, null);
            var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            ps.AddAccessRule(new PipeAccessRule(usersSid, PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow)); // "Users"
            ps.AddAccessRule(new PipeAccessRule(createrownerSid, PipeAccessRights.FullControl, AccessControlType.Allow)); //"CREATOR OWNER"
            ps.AddAccessRule(new PipeAccessRule(systemSid, PipeAccessRights.FullControl, AccessControlType.Allow)); // "SYSTEM"
            return ps;
        }
    }
}
