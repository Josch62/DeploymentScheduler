using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class ConfirmWindowSettings
    {
        public string InfoText { get; set; } = "Installations are scheduled at: %TIME%\nPlease confirm or view.";
    }
}
