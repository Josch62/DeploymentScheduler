using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class ToastNotifySupInstallationStartSettings
    {
        public string Title { get; set; } = "Now installing";

        public string SubText { get; set; } = "Windows updates";

        public ToastScenarioIntern ToastScenario { get; set; } = ToastScenarioIntern.Default;

        public int Duration { get; set; } = 1;
    }
}
