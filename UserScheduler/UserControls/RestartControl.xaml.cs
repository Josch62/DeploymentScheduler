using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using OneControls;
using SchedulerCommon.Sql;

namespace UserScheduler.UserControls
{
    /// <summary>
    /// Interaction logic for RestartControl.xaml
    /// </summary>
    public partial class RestartControl : UserControl
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public string TopTitle
        {
            set
            {
                TopTitleBox.Text = value;
            }
        }

        private RestartSchedule _rs;

        public RestartControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _rs = SqlCe.GetRestartSchedule();
            LbDeadline.Content = _rs.DeadLine.ToString();
            TpPicker.SelectedDate = _rs.RestartTime;
            TpPicker.MaximumDate = _rs.DeadLine;
            TpPicker.MinimumDate = RoundUp(DateTime.Now);
            SetStatus();
            SetupTimer();
        }

        private void SetupTimer()
        {
            _timer.Tick += Timer_Tick;
            _timer.Interval = new TimeSpan(0, 0, 0, 0, AutoInterval());
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();

            if (TpPicker.MinimumDate <= DateTime.Now)
            {
                TpPicker.MinimumDate = RoundUp(DateTime.Now);
            }

            _timer.Interval = new TimeSpan(0, 0, 0, 0, AutoInterval());
            _timer.Start();
        }

        private void BtSchedule_Click(object sender, RoutedEventArgs e)
        {
            _rs.RestartTime = TpPicker.SelectedDate;
            _rs.IsAcknowledged = true;
            SqlCe.SetRestartSchedule(_rs);
            SetStatus();
        }

        private void BtRestart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var conf = Globals.Settings.RestartConfig;

                Process.Start(new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = $"/r /f /t 0",
                    WindowStyle = ProcessWindowStyle.Hidden,
                });
            }
            catch (Exception ex)
            {
                Globals.Log.Error(ex.Message);
            }
        }

        private void SetStatus()
        {
            if (_rs.IsAcknowledged)
            {
                StatusGreen.Visibility = Visibility.Visible;
                StatusOrange.Visibility = Visibility.Hidden;
                StatusText.Text = Globals.Settings.RestartSettings.RestartAcknowledgedStatusText.Replace("%RESTARTTIME%", _rs.RestartTime.ToString());
            }
            else
            {
                StatusGreen.Visibility = Visibility.Hidden;
                StatusOrange.Visibility = Visibility.Visible;
                StatusText.Text = Globals.Settings.RestartSettings.RestartRequiredStatusText;
            }
        }

        private void TpPicker_DateChanged(object sender, RoutedEventArgs e)
        {
            if (e.Source is DateTimePicker dp)
            {
                if (dp.SelectedDate < RoundUp(DateTime.Now))
                {
                    TpPicker.MinimumDate = RoundUp(DateTime.Now);
                }
            }
        }

        private DateTime RoundUp(DateTime date)
        {
            return new DateTime(date.Ticks - (date.Ticks % (TimeSpan.TicksPerMinute * 5))).AddMinutes(5);
        }

        private int AutoInterval()
        {
            var min = 5;
            var dt = DateTime.Now;
            var interval = min * 60000;
            double ms = dt.Millisecond;
            double mn = dt.Minute;
            double ss = dt.Second;
            var now = (mn * 60000) + (ss * 1000) + ms;
            var mod = (int)(now / interval);
            var next = interval * ++mod;
            var togo = next - now;
            var t = (now < interval) ? interval - now : togo;

            return (int)t + 50;
        }
    }
}
