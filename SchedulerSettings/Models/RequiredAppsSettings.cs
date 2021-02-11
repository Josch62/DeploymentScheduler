using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class RequiredAppsSettings
    {
        public string TopTitle { get; set; } = "Pick your preferred time to install";

        public string TopTitleNotAvailable { get; set; } = "No applications are required";

        public string AppIsBeingEnforcedStatusText { get; set; } = "This application is being enforced.";

        public string InstallationHasBeenScheduledStatusText { get; set; } = "This installation has been scheduled.";

        public string AppIsInstalledStatusText { get; set; } = "This application is installed.";

        public string AppNeedsAttentionStatusText { get; set; } = "This application needs attention.";

        public string AppIsInErrorStateStatusText { get; set; } = "This application is in an error state.";

        public string CoveredByServiceCycleText { get; set; } = "Covered by a service cycle at %TIME%";
    }
}
