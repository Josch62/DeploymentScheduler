using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class UpdatesSettings
    {
        public string UpdatesAvailableTitleText { get; set; } = "Security updates are available. Please schedule the installation or install now.";

        public string TopTitleNotAvailable { get; set; } = "No updates available";

        public string UpdatesAreScheduledTitleText { get; set; } = "Security updates are scheduled. Reschedule or install them now.";

        public string UpdatesNeedsAttentionText { get; set; } = "These updates need attention.";

        public string UpdatesHaveBeenScheduledText { get; set; } = "These updates have been scheduled.";

        public string CoveredByServiceCycleText { get; set; } = "Covered by a service cycle at %TIME%";
    }
}
