using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigurationEditor.Logging
{
    public class LogObject
    {
        public string Message { get; set; }

        public LogType LogLevel { get; set; }

        public string Date { get; set; } = DateTime.Now.ToString("MM-dd-yyyy");

        public string Time { get; set; } = DateTime.Now.ToString("HH:mm:ss.ffffff");
    }
}
