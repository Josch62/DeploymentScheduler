using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerCommon.Common
{
    public class IpuContentInformation
    {
        public bool IsDownloaded { get; set; }

        public string Location { get; set; }

        public DateTime? InstallTime { get; set; }
    }
}
