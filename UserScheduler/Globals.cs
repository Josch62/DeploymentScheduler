using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeploymentScheduler.Helpers;
using SchedulerCommon.Ccm;
using SchedulerCommon.Logging;
using SchedulerSettings;

namespace UserScheduler
{
    public static class Globals
    {
        public static Settings Settings { get; set; }

        public static DesktopEventSource Log { get; set; } = DesktopEventSource.Log;

        public static MainWindow MainWnd { get; set; }

        public static Arguments Args { get; set; } = new Arguments(Environment.GetCommandLineArgs());

        public static CcmWmiEventListener CcmWmiEventListener { get; set; }
    }
}
