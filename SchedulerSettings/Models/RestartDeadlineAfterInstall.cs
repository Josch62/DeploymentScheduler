using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class RestartDeadlineAfterInstall
    {
        public int InDays { get; set; } = 1;

        public int AtHour { get; set; } = 17;
    }
}
