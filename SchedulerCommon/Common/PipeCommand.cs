using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerCommon.Common
{
    public class PipeCommand
    {
        public string Action { get; set; }

        public string AppId { get; set; }

        public string AppRevision { get; set; }
    }
}
