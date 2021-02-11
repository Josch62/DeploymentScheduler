using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SchedulerCommon.Sql;

namespace OnevinnTrayIcon
{
    public partial class TrayIconResources
    {
        private void TaskbarIcon_TrayPopupOpen(object sender, RoutedEventArgs e)
        {
            var running = SqlCe.GetAutoEnforceFlag();

            Globals.NotifyIcon.TrayPopup = new TextBlock
            {
                Text = running ? "Maintenance is currently running" : "Maintenance is currently not running",
                Foreground = running ? Brushes.LightGreen : Brushes.LightGray,
                Background = Brushes.Black,
                FontSize = 14,
                Margin = new Thickness(5, 5, 5, 40),
                Padding = new Thickness(5),
            };
        }
    }
}