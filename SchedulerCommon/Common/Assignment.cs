using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerCommon.Enums;

namespace SchedulerCommon.Common
{
    public class Assignment
    {
        public string ScopeId { get; set; }

        public string Revision { get; set; }

        public AssignmentPurpose Purpose { get; set; }
    }
}
