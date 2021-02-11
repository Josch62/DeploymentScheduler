using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerSettings.Models
{
    [Serializable]
    public class LegalNotice
    {
        public bool UseLegalNotice { get; set; } = true;

        public string LegalNoticeCaption { get; set; } = "Computer maintenance is running";

        public string LegalNoticeText { get; set; } = "Updates and applications are being installed.\nThe computer may restart several times on short notice.";
    }
}
