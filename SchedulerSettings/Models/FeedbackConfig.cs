using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class FeedbackConfig
    {
        public FeedbackType FeedBackType { get; set; } = FeedbackType.None;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification ="Saved to xml")]
        public string Url { get; set; } = string.Empty;

        public MailSettings MailSettings { get; set; } = new MailSettings();
    }
}
