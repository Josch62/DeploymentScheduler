using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class ActiveTabs
    {
        public bool RequiredApps { get; set; } = true;

        public bool AvailableApps { get; set; } = true;

        public bool Updates { get; set; } = true;

        public bool RestartCenter { get; set; } = true;

        public bool Planner { get; set; } = true;

        public bool FeedBack { get; set; } = true;
    }
}
