using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class ToastNotifyNewApplicationSettings
    {
        public string Title { get; set; } = "You have a new application that needs attention.";

        public string SubText { get; set; } = "Please, schedule the installation.";

        public string BtSchedule { get; set; } = "Schedule";

        public string BtPostpone { get; set; } = "Postpone";

        public ToastScenarioIntern ToastScenario { get; set; } = ToastScenarioIntern.Reminder;

        public int Duration { get; set; } = 5;

        public bool OpenWizardTabOnSchedule { get; set; } = true;
    }
}
