using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class PlannerSettings
    {
        public string InfoText { get; set; } = "A minimum of one weekly automatic install time is recommended. All required updates and applications will be installed and if needed the computer restarted. This will suppress all individual notifications except for the auto install itself.";
    }
}
