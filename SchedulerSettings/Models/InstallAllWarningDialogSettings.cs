using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class InstallAllWarningDialogSettings
    {
        public string Text { get; set; } = "You have chosen to install all required applications and updates. This may cause the computer to restart several times on short notice, consider to instead schedule this action?\n\nDo you want to proceed?";

        public string ButtonNoText { get; set; } = "No";

        public string ButtonYesText { get; set; } = "Yes";
    }
}
