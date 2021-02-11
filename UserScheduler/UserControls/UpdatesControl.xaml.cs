using System;
using System.Collections.Generic;
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
using SchedulerCommon.Ccm;
using SchedulerCommon.Sql;
using UserScheduler.Enums;

namespace UserScheduler.UserControls
{
    /// <summary>
    /// Interaction logic for SupBundle.xaml.
    /// </summary>
    public partial class UpdatesControl : UserControl
    {
        private readonly DateTime _deadline;
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private readonly List<Update> _updates;
        private readonly DateTime _enforcementTime;
        private readonly DateTime? _nextServiceTime = null;
        private bool _isReschedule = false;
        private long _scheduleId;

        public UpdatesControl(List<Update> updates, DateTime? nextServiceTime)
        {
            InitializeComponent();
            _updates = updates;
            _nextServiceTime = nextServiceTime;
            _deadline = _updates.OrderBy(x => x.Deadline).Select(y => y.Deadline).ToList().First();
            TpPicker.SelectedDate = RoundUp(DateTime.Now);
            TpPicker.MaximumDate = _deadline;
            TpPicker.MinimumDate = RoundUp(DateTime.Now);

            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => { Load(); }));

            if (_nextServiceTime != null)
            {
                if (_nextServiceTime < _deadline)
                {
                    if (_nextServiceTime <= RoundUp(DateTime.Now).AddMinutes(5))
                    {
                        TpPicker.MinimumDate = ((DateTime)_nextServiceTime).AddMinutes(-10);
                        TimeGrid.IsEnabled = false;
                        ButtonGrid.IsEnabled = false;
                        _timer.Stop();
                    }

                    TpPicker.MaximumDate = (DateTime)_nextServiceTime;
                    TpPicker.SelectedDate = (DateTime)_nextServiceTime;

                    StatusGreen.Visibility = Visibility.Visible;
                    StatusText.Text = Globals.Settings.UpdatesSettings.CoveredByServiceCycleText.Replace("%TIME%", _nextServiceTime.ToString());

                    return;
                }
            }

            SetStatus(ScheduleStatus.Orange);
        }

        public UpdatesControl(List<Update> updates, DateTime enforcementTime, long id, DateTime? nextServiceTime)
        {
            InitializeComponent();
            _nextServiceTime = nextServiceTime;
            _isReschedule = true;
            _updates = updates;
            _enforcementTime = enforcementTime;
            TpPicker.SelectedDate = _enforcementTime;
            _deadline = _updates.OrderBy(x => x.Deadline).Select(y => y.Deadline).ToList().First();
            TpPicker.MaximumDate = _deadline;
            TpPicker.MinimumDate = RoundUp(DateTime.Now);
            _scheduleId = id;

            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => { Load(); }));

            if (_nextServiceTime != null)
            {
                if (enforcementTime < _deadline)
                {
                    if (_nextServiceTime <= RoundUp(DateTime.Now).AddMinutes(5))
                    {
                        TpPicker.MinimumDate = ((DateTime)_nextServiceTime).AddMinutes(-5);
                        TimeGrid.IsEnabled = false;
                        ButtonGrid.IsEnabled = false;
                        _timer.Stop();
                    }

                    TpPicker.MaximumDate = (DateTime)_nextServiceTime;
                    TpPicker.SelectedDate = (DateTime)_nextServiceTime;

                    StatusGreen.Visibility = Visibility.Visible;
                    StatusText.Text = Globals.Settings.UpdatesSettings.CoveredByServiceCycleText.Replace("%TIME%", _nextServiceTime.ToString());
                    return;
                }
            }

            SetStatus(ScheduleStatus.Green);
        }

        private void Load()
        {
            if (_updates.Count() > 0)
            {
                UpdateGrid.ItemsSource = _updates.OrderBy(x => x.Deadline).ToList();
                DetailsExpander.IsEnabled = true;
            }

            LbDeadline.Content = _deadline.ToString();

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

        private void BtInstall_Click(object sender, RoutedEventArgs e)
        {
            BtInstall.IsEnabled = false;
            BtSchedule.IsEnabled = false;
            TpPicker.IsEnabled = false;

            SqlCe.UpdateSupData("STD", string.Empty);

            if (_isReschedule)
            {
                SqlCe.SetEnforcedFlag(_scheduleId);
            }

            SqlCe.SetUpdatesInstallStatusFlag(true);
            CcmUtils.ExecuteInstallUpdates(true);
            SetStatus(ScheduleStatus.Green);
            StatusText.Text = "Installation has been initiated.";
        }

        private void BtSchedule_Click(object sender, RoutedEventArgs e)
        {
            BtInstall.IsEnabled = false;
            BtSchedule.IsEnabled = false;

            if (_isReschedule)
            {
                SqlCe.UpdateAppSchedule(_scheduleId, TpPicker.SelectedDate);
                Globals.Log.Information($"Updated sup schedule with id '{_scheduleId}' to execute at '{TpPicker.SelectedDate}'");
            }
            else
            {
                _isReschedule = true;
                _scheduleId = SqlCe.CreateSupSchedule(TpPicker.SelectedDate);
                Globals.Log.Information($"Created new sup schedule with id '{_scheduleId}' to execute at '{TpPicker.SelectedDate}'");
            }

            SetStatus(ScheduleStatus.Green);
        }

        private void SetStatus(ScheduleStatus status)
        {
            StatusRed.Visibility = Visibility.Hidden;
            StatusGreen.Visibility = Visibility.Hidden;
            StatusOrange.Visibility = Visibility.Hidden;

            switch (status)
            {
                case ScheduleStatus.Red:
                    StatusRed.Visibility = Visibility.Visible;
                    break;
                case ScheduleStatus.Green:
                    StatusGreen.Visibility = Visibility.Visible;
                    StatusText.Text = Globals.Settings.UpdatesSettings.UpdatesHaveBeenScheduledText;
                    break;
                case ScheduleStatus.Orange:
                    StatusOrange.Visibility = Visibility.Visible;
                    StatusText.Text = Globals.Settings.UpdatesSettings.UpdatesNeedsAttentionText;
                    break;
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
