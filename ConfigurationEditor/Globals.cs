using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ConfigurationManagement.AdminConsole.Views.Common;
using Microsoft.ConfigurationManagement.ManagementProvider;
using SchedulerSettings;

namespace ConfigurationEditor
{
    public static class Globals
    {
        public static OverviewControllerBase AttachedController { get; set; }

        public static Settings Settings { get; set; }

        public static ConnectionManagerBase Sccm
        {
            get
            {
                return AttachedController.ConnectionManager;
            }
        }
    }
}
