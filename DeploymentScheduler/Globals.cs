using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerCommon.Logging;

namespace DeploymentScheduler
{
    public static class Globals
    {
        public static ServiceEventSource Log { get; set; } = ServiceEventSource.Log;
    }
}
