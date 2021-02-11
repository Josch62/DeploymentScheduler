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
using SchedulerCommon.Ccm;
using SchedulerCommon.Common;
using SchedulerCommon.Sql;
using SchedulerSettings.Models;
using UserScheduler.Common;
using UserScheduler.Enums;

namespace UserScheduler.UserControls
{
    /// <summary>
    /// Interaction logic for AvailableAppsControl.xaml.
    /// </summary>
    public partial class AvailableAppsControl : UserControl
    {
        private readonly AvailableAppsSettings _availableAppsSettings;
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private readonly DateTime _enforcementTime;
        private readonly long _scheduleId = 0;
        private bool _isReschedule = false;
        private CMApplication _app;
        private bool _evalIsRunning = false;

        public AvailableAppsControl(CMApplication app, DateTime? nextServiceTime)
        {
            InitializeComponent();
            _availableAppsSettings = Globals.Settings.AvailableAppsSettings;
            _app = app;
            DataContext = _app;
            SetStatus(_app);
            TpPicker.MaximumDate = DateTime.Now.AddYears(1);
            TpPicker.MinimumDate = RoundUp(DateTime.Now);
            TpPicker.SelectedDate = RoundUp(DateTime.Now);

            BtInstall.IsEnabled = !_app.InstallState.Equals("Installed");
            BtUninstall.IsEnabled = _app.InstallState.Equals("Installed");
            BtRepair.IsEnabled = _app.InstallState.Equals("Installed") && _app.AllowedActions.Contains("Repair");

            if (!string.IsNullOrEmpty(_app.Icon.Trim()))
            {
                Icon.Source = Utils.ToBitmapImage(_app.Icon.Trim());
            }

            if (!string.IsNullOrEmpty(_app.Description))
            {
                LoadMessage(_app.Description);
            }

            Globals.CcmWmiEventListener.OnStatusChange += CcmWmiEventListener_OnStatusChange;

            SetupTimer();
        }

        public AvailableAppsControl(CMApplication app, DateTime enforcementTime, long id, DateTime? nextServiceTime)
        {
            InitializeComponent();
            _availableAppsSettings = Globals.Settings.AvailableAppsSettings;
            _app = app;
            DataContext = _app;
            _isReschedule = true;
            _enforcementTime = enforcementTime;
            TpPicker.SelectedDate = _enforcementTime;
            TpPicker.MaximumDate = DateTime.Now.AddYears(1);
            TpPicker.MinimumDate = RoundUp(DateTime.Now);

            BtInstall.IsEnabled = !_app.InstallState.Equals("Installed");
            BtUninstall.IsEnabled = _app.InstallState.Equals("Installed");
            BtRepair.IsEnabled = _app.InstallState.Equals("Installed") && _app.AllowedActions.Contains("Repair");
            _scheduleId = id;

            SetStatus(_app);

            if (!string.IsNullOrEmpty(_app.Icon))
            {
                var bmp = Utils.ToBitmapImage(_app.Icon);

                if (bmp != null)
                {
                    Icon.Source = Utils.ToBitmapImage(_app.Icon);
                }
            }

            if (!string.IsNullOrEmpty(_app.Description))
            {
                LoadMessage(_app.Description);
            }

            Globals.CcmWmiEventListener.OnStatusChange += CcmWmiEventListener_OnStatusChange;

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

        private async void CcmWmiEventListener_OnStatusChange(object sender, CcmWmiEventargument e)
        {
            Globals.CcmWmiEventListener.OnStatusChange -= CcmWmiEventListener_OnStatusChange;

            try
            {
                if (!_evalIsRunning)
                {
                    if (e.Id.Equals(_app.Id) && e.Revision.Equals(_app.Revision) && e.IsMachineTarget)
                    {
                        _evalIsRunning = true;

                        await Dispatcher.Invoke(async () =>
                        {
                            await EvaluateApplication();
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Globals.Log.Error(ex.Message);
            }
            finally
            {
                _evalIsRunning = false;
            }

            Globals.CcmWmiEventListener.OnStatusChange += CcmWmiEventListener_OnStatusChange;
        }

        private void Hyperlink_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var hyperlink = (Hyperlink)sender;
            Process.Start(hyperlink.NavigateUri.ToString());
        }

        private void LoadMessage(string description)
        {
            try
            {
                var incontent = new TextRange(rtfBox.Document.ContentStart, rtfBox.Document.ContentEnd)
                {
                    Text = description.Trim(),
                };

                rtfBox.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Globals.Log.Error(ex.Message);
            }
        }

        private void BtSchedule_Click(object sender, RoutedEventArgs e)
        {
            BtSchedule.IsEnabled = false;
            //TpPicker.Expanded = false;
            if (_isReschedule)
            {
                SqlCe.UpdateAppSchedule(_scheduleId, TpPicker.SelectedDate);
                Globals.Log.Information($"Updated schedule: {TpPicker.SelectedDate} Available application: '{_app.Name}'");
            }
            else
            {
                _isReschedule = true;
                SqlCe.CreateAppSchedule(TpPicker.SelectedDate, _app.Id, _app.Revision, false, _app.InstallState.Equals("Installed") ? "U" : "I");
                Globals.Log.Information($"Created schedule: {TpPicker.SelectedDate} Available application: '{_app.Name}'");
            }

            SetStatus(_app);
        }

        private void BtInstall_Click(object sender, RoutedEventArgs e)
        {
            BtInstall.IsEnabled = false;
            _isReschedule = false;
            var jobId = CcmUtils.InstallApplication(_app);
            Globals.Log.Information($"JobId Install: '{jobId}' Application name: '{_app.Name}'");
        }

        private void BtUninstall_Click(object sender, RoutedEventArgs e)
        {
            BtUninstall.IsEnabled = false;
            _isReschedule = false;
            var jobId = CcmUtils.UninstallApplication(_app);
            Globals.Log.Information($"JobId Install: '{jobId}' Application name: '{_app.Name}'");
        }

        private async Task EvaluateApplication()
        {
            BtInstall.IsEnabled = false;
            BtUninstall.IsEnabled = false;
            BtRepair.IsEnabled = false;
            BtSchedule.IsEnabled = false;
            //TpPicker.Expanded = false;
            TpPicker.IsEnabled = false;
            ProgressbarEnforcement.Visibility = Visibility.Visible;

            CMApplication tmpApp = null;

            await Task.Run(() =>
            {
                try
                {
                    tmpApp = CcmUtils.GetSpecificApp(new ScheduledObject { ObjectId = _app.Id, Revision = _app.Revision }, false);
                    SetStatus(tmpApp);

                    do
                    {
                        System.Threading.Thread.Sleep(2500);
                        tmpApp = CcmUtils.GetSpecificApp(new ScheduledObject { ObjectId = _app.Id, Revision = _app.Revision }, false);
                        SetStatus(tmpApp);
                    }
                    while (!(tmpApp.EvaluationState == 1 || tmpApp.EvaluationState == 3 || tmpApp.EvaluationState == 4 || tmpApp.EvaluationState == 16 || tmpApp.EvaluationState == 24 || tmpApp.EvaluationState == 25));
                }
                catch
                {
                    Globals.MainWnd.SpAvailableApps.Children.Remove(this);
                }
            });

            ProgressbarEnforcement.Visibility = Visibility.Hidden;
            BtInstall.IsEnabled = !tmpApp.InstallState.Equals("Installed");
            BtUninstall.IsEnabled = tmpApp.InstallState.Equals("Installed");
            BtRepair.IsEnabled = tmpApp.InstallState.Equals("Installed") && _app.AllowedActions.Contains("Repair");
            TpPicker.IsEnabled = true;
            BtSchedule.IsEnabled = true;
            _app = CcmUtils.GetSpecificApp(new ScheduledObject { ObjectId = _app.Id, Revision = _app.Revision });
        }

        private void SetStatus(CMApplication app)
        {
            Dispatcher.Invoke(() =>
            {
                StatusRed.Visibility = Visibility.Hidden;
                StatusGreen.Visibility = Visibility.Hidden;
                StatusOrange.Visibility = Visibility.Hidden;
                StatusBlue.Visibility = Visibility.Hidden;

                StatusRed.ToolTip = app.EvaluationStateText;
                StatusGreen.ToolTip = app.EvaluationStateText;
                StatusOrange.ToolTip = app.EvaluationStateText;
                StatusBlue.ToolTip = app.EvaluationStateText;

                if (app.InstallState.Equals("Installed") && app.EvaluationState == 1 && !_isReschedule)
                {
                    StatusGreen.Visibility = Visibility.Visible;
                    StatusText.Text = _availableAppsSettings.AppIsInstalledStatusText;
                    return;
                }

                switch (app.EvaluationState)
                {
                    case 1:
                    case 3:
                        if (_isReschedule)
                        {
                            StatusGreen.Visibility = Visibility.Visible;
                            StatusText.Text = _availableAppsSettings.InstallationHasBeenScheduledStatusText;
                        }
                        else
                        {
                            StatusBlue.Visibility = Visibility.Visible;
                            StatusText.Text = _availableAppsSettings.AppCanBeInstalledStatusText;
                        }

                        break;

                    case 4:
                    case 16:
                    case 24:
                    case 25:
                        StatusRed.Visibility = Visibility.Visible;
                        StatusText.Text = _availableAppsSettings.AppIsInErrorStateStatusText;
                        break;

                    default:
                        StatusOrange.Visibility = Visibility.Visible;
                        StatusText.Text = app.EvaluationState == 13 ? "Reboot pending." : _availableAppsSettings.AppIsBeingEnforcedStatusText;
                        ProgressbarEnforcement.Visibility = Visibility.Visible;
                        TpPicker.IsEnabled = false;
                        BtInstall.IsEnabled = false;
                        BtRepair.IsEnabled = false;
                        BtSchedule.IsEnabled = false;

                        if (_isReschedule)
                        {
                            if (SqlCe.IsAppScheduled(app.Id, app.Revision, out var id))
                            {
                                SqlCe.SetEnforcedFlag(id);
                            }

                            _isReschedule = false;
                        }

                        break;
                }
            });
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Globals.CcmWmiEventListener.OnStatusChange -= CcmWmiEventListener_OnStatusChange;
            }
            catch { }
        }

        private void BtRepair_Click(object sender, RoutedEventArgs e)
        {
            BtRepair.IsEnabled = false;
            _isReschedule = false;
            var jobId = CcmUtils.RepairApplication(_app);
            Globals.Log.Information($"JobId Repair: '{jobId}' Application name: '{_app.Name}'");
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
