using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class IpuApplication
    {
        public string UiTitle { get; set; } = "A Windows update is available";

        public string UiInfo { get; set; } = "This is a mandatory update that will be forcefully applied at deadline. Uptil then you can freely schedule it at a time of your convenience.\nThe upgrade is divided into a long running (30-60 minutes) phase where you can continue to use the computer normally as long as you do not restart it.\n\nWhen all preparations are done you will be prompted to restart the computer, this restart will take 10-15 minutes and must not be interupted.";

        public bool ShowDialog1 { get; set; } = true;

        public string Dialog1 { get; set; } = "Windows is being upgraded.\n\nThe upgrade is divided into a long running (30-60 minutes) phase where you can continue to use the computer normally as long as you do not restart it.\nWhen all preparations are done you will be prompted to restart the computer, this restart will take 10-15 minutes and must not be interupted.";

        public int Dialog1Time { get; set; } = 300;

        public string Dialog1AbortButtonText { get; set; } = "Abort";

        public string Dialog1StartButtonText { get; set; } = "Install";

        public string Dialog2 { get; set; } = "Windows is being upgraded.\n\nA restart is required to finalize the upgrade, this restart will take 10-15 minutes and must not be interupted.\n\nClose all open files and applications before restarting!";

        public int Dialog2Time { get; set; } = 300;

        public string Dialog2StartButtonText { get; set; } = "Restart";

        public string ApiUrl { get; set; } = "Under development";

        public string ExcludedModels { get; set; } = string.Empty;

        public bool UseVersionForLenovo { get; set; } = true;

        public bool UseWimDrivers { get; set; } = false;

        public string CustomDriversFolder { get; set; } = string.Empty;

        public bool ShowProgress { get; set; } = true;

        public string Phase1Text { get; set; } = "Upgrade, Phase 1/4";

        public string Phase2Text { get; set; } = "Upgrade, Phase 2/4";

        public string Phase3Text { get; set; } = "Patching, Phase 3/4";

        public string Phase4Text { get; set; } = "Upgrade, Phase 4/4";

        public string FullMediaStatusText { get; set; } = "Upgrade progress";

        public string ProgressTooltip { get; set; } = "Windows is being upgraded.\n\nDo not turn off the computer.";
    }
}
