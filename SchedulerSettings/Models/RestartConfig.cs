using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class RestartConfig
    {
        public WindowsRestartMessage WindowsRestartMessage { get; set; } = new WindowsRestartMessage();

        public string CountDown { get; set; } = "60";
    }
}