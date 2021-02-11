using System.Windows;
using System.Windows.Controls;
using Microsoft.ConfigurationManagement.AdminConsole.Views.Common;

namespace ConfigurationEditor
{
    public class ConfigurationViewControl : OverviewControllerBase
    {
        public ConfigurationViewControl()
            : base()
        {
        }

        public override void EndInit()
        {
            base.EndInit();
            Content = (FrameworkElement)new ConfigurationControl((OverviewControllerBase)this);
        }
    }
}
