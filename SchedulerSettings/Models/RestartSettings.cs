using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class RestartSettings
    {
        public string TopTitle { get; set; } = "Pick a time to restart the computer";

        public string TopTitleNotAvailable { get; set; } = "No pending restart";

        public string RestartRequiredStatusText { get; set; } = "Please confirm (save) the restart time?";

        public string RestartAcknowledgedStatusText { get; set; } = "This restart is scheduled at %RESTARTTIME%";
    }
}
