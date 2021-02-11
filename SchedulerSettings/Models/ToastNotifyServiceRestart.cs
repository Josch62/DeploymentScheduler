using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class ToastNotifyServiceRestart
    {
        public string Title { get; set; } = "Maintenance";

        public string SubText { get; set; } = "The computer is performing a service restart wihin %COUNTDOWN% seconds";

        public ToastScenarioIntern ToastScenario { get; set; } = ToastScenarioIntern.Default;

        public int Duration { get; set; } = 8;
    }
}
