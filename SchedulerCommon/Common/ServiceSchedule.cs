using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerCommon.Common
{
    public class ServiceSchedule
    {
        public DateTime ExecuteTime { get; set; }

        public bool IsAcknowledged { get; set; }
    }
}
