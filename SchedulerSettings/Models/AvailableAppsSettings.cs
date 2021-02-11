using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class AvailableAppsSettings
    {
        public string TopTitle { get; set; } = "Pick your preferred time to install";

        public string TopTitleNotAvailable { get; set; } = "No applications are available";

        public string InstallationHasBeenScheduledStatusText { get; set; } = "This installation has been scheduled.";

        public string AppIsBeingEnforcedStatusText { get; set; } = "This application is being enforced.";

        public string AppIsInstalledStatusText { get; set; } = "This application is installed.";

        public string AppCanBeInstalledStatusText { get; set; } = "This application can be installed.";

        public string AppIsInErrorStateStatusText { get; set; } = "This application is in an error state.";
    }
}
