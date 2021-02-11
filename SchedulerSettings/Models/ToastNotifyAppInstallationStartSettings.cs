using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class ToastNotifyAppInstallationStartSettings
    {
        public string Title { get; set; } = "Now installing";

        public ToastScenarioIntern ToastScenario { get; set; } = ToastScenarioIntern.Default;

        public int Duration { get; set; } = 1;
    }
}
