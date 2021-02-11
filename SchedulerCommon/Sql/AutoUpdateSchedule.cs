using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerCommon.Sql
{
    public class AutoUpdateSchedule
    {
        public bool IsActive { get; set; }

        public int DayOfWeek { get; set; }

        public string Hour { get; set; }

        public string Minute { get; set; }

        public string AmPm { get; set; }
    }
}
