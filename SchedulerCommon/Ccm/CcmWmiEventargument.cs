using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerCommon.Ccm
{
    public class CcmWmiEventargument : EventArgs
    {
        public string Id { get; set; }

        public string Revision { get; set; }

        public bool IsMachineTarget { get; set; }
    }
}
