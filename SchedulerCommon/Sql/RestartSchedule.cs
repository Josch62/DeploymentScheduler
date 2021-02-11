using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerCommon.Sql
{
    public class RestartSchedule
    {
        public DateTime RestartTime { get; set; }

        public DateTime DeadLine { get; set; }

        public bool IsAcknowledged { get; set; }

        public bool IsExpress { get; set; }
    }
}
