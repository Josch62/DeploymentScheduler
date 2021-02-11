using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class CountdownWindowSettings
    {
        public int TotalTime { get; set; } = 900;

        public int RestoreInterval { get; set; } = 300;

        public int KeepOnTop { get; set; } = 120;

        public string InfoText { get; set; } = "The computer will be restarted automatically at the end of the countdown. You can manually restart the computer at any time.";
    }
}
