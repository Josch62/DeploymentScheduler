using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class ServiceConfig
    {
        public int DetectionInterval { get; set; } = 30;

        public RestartDeadlineAfterInstall RestartDeadlineAfterInstall { get; set; } = new RestartDeadlineAfterInstall();

        public int NumberOfRestartToastsPerDay { get; set; } = 48;

        public bool HardSuppressSCNotifications { get; set; } = true;

        public string ServiceCycleTabText { get; set; } = "A Service cycle is running, try again later";
    }
}
