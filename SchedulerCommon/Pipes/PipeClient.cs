using System;
using System.Text;
using System.IO.Pipes;

namespace SchedulerCommon.Pipes
{
    public class PipeClient
    {
        public void Send(string sendStr, string pipeName, int timeOut = 1000)
        {
            try
            {
                var pipeStream = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
                pipeStream.Connect(timeOut);

                var buffer = Encoding.UTF8.GetBytes(sendStr);
                pipeStream.BeginWrite(buffer, 0, buffer.Length, AsyncSend, pipeStream);
            }
            catch { }
        }

        private void AsyncSend(IAsyncResult iar)
        {
            try
            {
                // Get the pipe
                var pipeStream = (NamedPipeClientStream)iar.AsyncState;

                // End the write
                pipeStream.EndWrite(iar);
                pipeStream.Flush();
                pipeStream.Close();
            }
            catch { }
        }
    }
}
