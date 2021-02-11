using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class ToastNotifyServiceInit
    {
        public string Title { get; set; } = "A service cycle was initiated";

        public string SubText { get; set; } = "All required updates and applications will be installed. The computer might restart several times on short notice.";

        public ToastScenarioIntern ToastScenario { get; set; } = ToastScenarioIntern.Default;

        public int Duration { get; set; } = 8;
    }
}
