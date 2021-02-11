using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ConfigurationEditor.Logging
{
    public static class Logger
    {
        private static readonly BlockingCollection<LogObject> _logQueue = new BlockingCollection<LogObject>(new ConcurrentQueue<LogObject>());
        private static readonly string _logFileName = "Onevinn.ConfigurationEditor.log";
        private static readonly string _fileName = "ConfigurationEditor.dll";
        private static readonly string _component = "ConfigurationEditor";
        private static readonly string _programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        private static readonly string _logfolder = Path.Combine(_programData, "Onevinn", "ConfigurationEditor", "log");
        private static readonly string _logfile = Path.Combine(_logfolder, _logFileName);
        private static readonly string _pid = Process.GetCurrentProcess().Id.ToString();

        public static void Log(string message, LogType type)
        {
            _logQueue.Add(new LogObject
            {
                Message = message,
                LogLevel = type,
            });
        }

        public static void Start()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var l = _logQueue.Take();
                        WriteLog(l);
                    }
                    catch { }
                }
            });
        }

        private static void WriteLog(LogObject log)
        {
            CheckSizeAndCreateOldLog();
            var logTextParsed = $"<![LOG[{log.Message}]LOG]!><time=\"{log.Time}\" date=\"{log.Date}\" component=\"{_component}\" context=\"\" type=\"{(int)log.LogLevel}\" thread=\"{_pid}\" file=\"{_fileName}\">";
            File.AppendAllText(_logfile, logTextParsed + Environment.NewLine);
        }

        private static string MakeOldLogName()
        {
            var fn = _logfile.Replace(".log", string.Empty);
            var dt = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            fn = $"{fn}-{dt}.log";

            return Path.Combine(_logfolder, fn);
        }

        private static void CheckSizeAndCreateOldLog()
        {
            if (!Directory.Exists(_logfolder))
            {
                Directory.CreateDirectory(_logfolder);
            }

            if (!File.Exists(_logfile))
            {
                return;
            }

            var fi = new FileInfo(_logfile);

            if (fi.Length > 20000)
            {
                File.Copy(_logfile, MakeOldLogName());
                File.Delete(_logfile);
            }
        }
    }
}
