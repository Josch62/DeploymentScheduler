using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerCommon.Common
{
    public class ScheduledObject
    {
        public long Id { get; set; }

        public string EnforcementType { get; set; }

        public DateTime EnforcementTime { get; set; }

        public string ObjectId { get; set; }

        public string Revision { get; set; }

        public DateTime? LastToastTime { get; set; }

        public string Action { get; set; } = "I";

        public bool HasRaisedConfirm { get; set; } = false;

        public bool IsAutoSchedule { get; set; } = false;
    }
}
