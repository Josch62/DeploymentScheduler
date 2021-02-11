using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class WindowsRestartMessage
    {
        public string Line1 { get; set; } = "The computer will restart in 60 seconds, close all open files.";

        public string Line2 { get; set; } = "Thank you for your cooperation.";
    }
}
