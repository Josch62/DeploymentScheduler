using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class RestartChecks
    {
        public bool ConfigMgrClient { get; set; } = true;

        public bool WindowsUpdate { get; set; } = true;

        public bool ComponentBasedServicing { get; set; } = true;

        public bool PendingFileOperations { get; set; } = false;

        public List<string> PendingFileNameExclusions { get; set; } = new List<string> { string.Empty, string.Empty };
    }
}
