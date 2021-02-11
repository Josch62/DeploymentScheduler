using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using OneControls;
using SchedulerCommon.Ccm;
using SchedulerCommon.Pipes;
using SchedulerCommon.Sql;
using UserScheduler.Enums;
using UserScheduler.Windows;

namespace UserScheduler.UserControls
{
    /// <summary>
    /// Interaction logic for ScheduleAllControl.xaml
    /// </summary>
    public partial class ScheduleAllControl : UserControl
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private readonly List<CMApplication> _applications;
        private readonly List<Update> _updates;
        private DateTime _firstDeadline;
        private bool _canSchedule = false;

        public ScheduleAllControl(IEnumerable<CMApplication> applications, IEnumerable<Update> updates, DateTime? nextServiceTime)
        {
            InitializeComponent();

            if (applications != null)
            {
                _applications = applications.Where(x => !x.InstallState.Equals("Installed")).OrderBy(x => x.Deadline).ToList();
            }
            else
            {
                _applications = new List<CMApplication>();
            }

            if (updates != null)
            {
                _updates = updates.OrderBy(x => x.Deadline).ToList();
            }
            else
            {
                _updates = new List<Update>();
            }

            DtPicker.MinimumDate = RoundUp(DateTime.Now);
            LoadAppsAndUpdate();
            EvalStatus();
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

            if (DtPicker.MinimumDate <= DateTime.Now)
            {
                DtPicker.MinimumDate = RoundUp(DateTime.Now);
            }

            _timer.Interval = new TimeSpan(0, 0, 0, 0, AutoInterval());
            _timer.Start();
        }

        private void LoadAppsAndUpdate()
        {
            var count = _updates.Count() + _applications.Count();

            if (count == 0)
            {
                TbInfoText.Text = "No updates or applications required at this time.";
                return;
            }
            else
            {
                TbInfoText.Text = $"Pick a time to run a one time Updates and Applications service cycle.";
                AppGrid.ItemsSource = _applications;
                UpdateGrid.ItemsSource = _updates;
                _canSchedule = true;
            }

            var deadlineList = _updates.Select(x => x.Deadline).ToList();
            deadlineList.AddRange(_applications.Select(x => x.Deadline).ToList());
            _firstDeadline = deadlineList.OrderBy(x => x).ToList().FirstOrDefault().AddHours(-1);

            if (_firstDeadline < DateTime.Now.AddMinutes(10))
            {
                TbInfoText.Text = $"Updates or Applications are required but cannot be scheduled due to early deadline (minimum 2h) '{_firstDeadline}'";
                return;
            }

            DtPicker.MaximumDate = _firstDeadline;
            ScheduleGrid.IsEnabled = true;
            BtInstall.IsEnabled = true;
            DetailsExpander.IsEnabled = true;
        }

        private void EvalStatus()
        {
            var existingSchedule = SqlCe.GetServiceSchedule();

            if (existingSchedule != null)
            {
                if (existingSchedule.ExecuteTime < _firstDeadline)
                {
                    SetStatus(ScheduleStatus.Green);
                    DtPicker.SelectedDate = existingSchedule.ExecuteTime;
                }
                else
                {
                    SqlCe.DeleteServiceSchedule();
                    SetStatus(ScheduleStatus.Blue);
                }
            }
            else if (_canSchedule)
            {
                SetStatus(ScheduleStatus.Blue);
            }
            else
            {
                SetStatus(ScheduleStatus.Orange);
            }
        }

        private void BtSchedule_Click(object sender, RoutedEventArgs e)
        {
            SqlCe.SetServiceSchedule(DtPicker.SelectedDate);
            EvalStatus();
        }

        private void DtPicker_DateChanged(object sender, RoutedEventArgs e)
        {
            if (e.Source is DateTimePicker dp)
            {
                if (dp.SelectedDate < RoundUp(DateTime.Now))
                {
                    DtPicker.MinimumDate = RoundUp(DateTime.Now);
                }
            }
        }

        private DateTime RoundUp(DateTime date)
        {
            return new DateTime(date.Ticks - (date.Ticks % (TimeSpan.TicksPerMinute * 5))).AddMinutes(5);
        }

        private PipeClient _pipeClient = null;

        private void BtInstall_Click(object sender, RoutedEventArgs e)
        {
            var ww = new WarningDialog
            {
                ShowActivated = true,
                Owner = Application.Current.MainWindow,
            };

            ww.ShowDialog();

            if (ww.Abort)
            {
                return;
            }

            ScheduleGrid.IsEnabled = false;
            BtInstall.IsEnabled = false;
            DetailsExpander.IsExpanded = false;
            DetailsExpander.IsEnabled = false;
            SetStatus(ScheduleStatus.Orange);

            SqlCe.SetAutoEnforceFlag(true);
            SqlCe.DeleteServiceSchedule();
            SqlCe.UpdateSupData("STD", string.Empty);

            CcmUtils.InstallAllAppsAndUpdates();

            if (_pipeClient == null)
            {
                _pipeClient = new PipeClient();
            }

            _pipeClient.Send("SetRed", "01DB94E3-90F1-43F4-8DDA-8AEAF6C08A8E");
            Globals.Log.Information("Sent icon switch command to tray.");
        }

        private void SetStatus(ScheduleStatus status)
        {
            StatusRed.Visibility = Visibility.Hidden;
            StatusGreen.Visibility = Visibility.Hidden;
            StatusOrange.Visibility = Visibility.Hidden;
            StatusBlue.Visibility = Visibility.Hidden;
            StatusText.Text = string.Empty;

            switch (status)
            {
                case ScheduleStatus.Orange:
                    StatusOrange.Visibility = Visibility.Visible;
                    StatusText.Text = "No updates or applications can be scheduled";
                    break;

                case ScheduleStatus.Blue:
                    StatusBlue.Visibility = Visibility.Visible;
                    StatusText.Text = "Updates or applications can be scheduled";
                    break;

                case ScheduleStatus.Green:
                    StatusGreen.Visibility = Visibility.Visible;
                    StatusText.Text = "Updates or applications are scheduled";
                    break;
            }
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
