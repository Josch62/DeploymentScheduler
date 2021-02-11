using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class MailSettings
    {
        public string MailTo { get; set; } = string.Empty;

        public string SmtpServer { get; set; } = string.Empty;

        public string SuccessTextLine1 { get; set; } = "Your feedback has been sent.";

        public string SuccessTextLine2 { get; set; } = "Thank you, your feedback is important to us.";

        public string FailedTextLine1 { get; set; } = "Failed to send feedback.";

        public string FailedTextLine2 { get; set; } = "Try again later.";
    }
}
