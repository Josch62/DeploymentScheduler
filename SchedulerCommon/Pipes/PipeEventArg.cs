using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerCommon.Pipes
{
    public class PipeEventArg : EventArgs
    {
        public string Message { get; set; }
    }
}
