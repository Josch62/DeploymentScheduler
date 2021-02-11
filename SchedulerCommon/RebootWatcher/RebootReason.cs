using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerCommon.RebootWatcher
{
    public class RebootReason
    {
        public bool ConfigMgrClient { get; set; }

        public bool WindowsUpdate { get; set; }

        public bool ComponentBasedServicing { get; set; }

        public bool PendingFileOperations { get; set; }

        public bool Any
        {
            get
            {
                return ConfigMgrClient | WindowsUpdate | ComponentBasedServicing | PendingFileOperations;
            }
        }
    }
}
