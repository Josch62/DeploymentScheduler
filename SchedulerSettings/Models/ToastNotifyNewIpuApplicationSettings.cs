using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class ToastNotifyNewIpuApplicationSettings
    {
        public string Title { get; set; } = "A Windows upgrade is available.";

        public string SubText { get; set; } = "Please, schedule the upgrade.";

        public string BtSchedule { get; set; } = "Schedule";

        public string BtPostpone { get; set; } = "Postpone";

        public ToastScenarioIntern ToastScenario { get; set; } = ToastScenarioIntern.Reminder;

        public int Duration { get; set; } = 5;
    }
}
