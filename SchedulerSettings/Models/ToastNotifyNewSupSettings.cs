using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class ToastNotifyNewSupSettings
    {
        public string Title { get; set; } = "Please install or schedule Windows updates";

        public string SubText { get; set; } = "This generally includes features and security updates. It is important to keep Windows up to date at all times.";

        public string BtInstall { get; set; } = "Install";

        public string BtSchedule { get; set; } = "Schedule";

        public string BtPostpone { get; set; } = "Postpone";

        public ToastScenarioIntern ToastScenario { get; set; } = ToastScenarioIntern.Reminder;

        public int Duration { get; set; } = 5;

        public bool OpenWizardTabOnSchedule { get; set; } = true;
    }
}
