using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Diagnostics.Tracing;
using SchedulerCommon.Extensions;

namespace SchedulerCommon.Logging
{
    [EventSource(Name = "Onevinn-DeploymentScheduler-SqlCE")]
    public sealed class SqlCEEventSource : EventSource
    {
        public static readonly SqlCEEventSource Log = new SqlCEEventSource();

        private SqlCEEventSource()
            : base()
        {
        }

        [Event(1, Message = "Information='{0}'", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void Information(string message)
        {
            WriteEvent(1, message.TruncateStringForLogging());
        }

        [Event(2, Message = "Warning='{0}'", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void Warning(string message)
        {
            WriteEvent(2, message.TruncateStringForLogging());
        }

        [Event(3, Message = "File='{0}' Method='{1}' LineNumber='{2}' Exception='{3}'", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void Error(string message, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string method = null)
        {
            var fName = Path.GetFileName(file);

            WriteEvent(3, fName, method, lineNumber, message.TruncateStringForLogging());
        }
    }
}
