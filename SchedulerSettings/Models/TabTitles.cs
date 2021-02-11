using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class TabTitles
    {
        public string WindowsUpgrade { get; set; } = "Windows upgrade";

        public string RequiredApps { get; set; } = "Required Apps";

        public string AvailableApps { get; set; } = "Available Apps";

        public string Updates { get; set; } = "Updates";

        public string RestartCenter { get; set; } = "Restart center";

        public string Planner { get; set; } = "Wizards";

        public string Feedback { get; set; } = "Feedback";
    }
}
