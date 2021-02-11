using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerCommon.Ccm
{
    public class TrayStatus
    {
        public string Name { get; set; }

        public string EvaluationStateText { get; set; }

        public string ToolTipText { get; set; }

        public int PercentComplete { get; set; }
    }
}
