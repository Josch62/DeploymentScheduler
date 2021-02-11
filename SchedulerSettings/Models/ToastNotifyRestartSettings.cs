using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class ToastNotifyRestartSettings
    {
        public string Title { get; set; } = "Restart required";

        public string SubText { get; set; } = "The computer needs to be restarted to complete installing software or updates.";

        public string BtSchedule { get; set; } = "Schedule";

        public string BtPostpone { get; set; } = "Postpone";

        public ToastScenarioIntern ToastScenario { get; set; } = ToastScenarioIntern.Reminder;

        public int Duration { get; set; } = 10;
    }
}
