using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class ToastNotifyServiceEnd
    {
        public string Title { get; set; } = "Service cycle ended";

        public string SubText { get; set; } = "All required updates and applications have be installed.";

        public ToastScenarioIntern ToastScenario { get; set; } = ToastScenarioIntern.Default;

        public int Duration { get; set; } = 8;
    }
}
