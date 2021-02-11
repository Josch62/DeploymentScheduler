using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using Newtonsoft.Json;
using OneControls;
using SchedulerCommon.Ccm;
using SchedulerCommon.Common;
using SchedulerCommon.Pipes;
using SchedulerCommon.Sql;
using SchedulerSettings.Models;
using UserScheduler.Common;
using UserScheduler.Extensions;

namespace UserScheduler.UserControls
{
    /// <summary>
    /// Interaction logic for ShowMessage.xaml.
    /// </summary>
    public partial class RequiredAppsControl : UserControl
    {
        private readonly RequiredAppsSettings _requiredAppsSettings;
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private readonly long _scheduleId;
        private readonly DateTime _enforcementTime;
        private readonly DateTime? _nextServiceTime = null;
        private bool _isReschedule = false;
        private CMApplication _app;
        private bool _evalIsRunning = false;

        public RequiredAppsControl(CMApplication app, DateTime? nextServiceTime)
        {
            InitializeComponent();
            _requiredAppsSettings = Globals.Settings.RequiredAppsSettings;
            _nextServiceTime = nextServiceTime;
            _app = app;
            DataContext = _app;
            TpPicker.MaximumDate = _app.InstallState.Equals("Installed") && _app.AllowedActions.Contains("Repair") ? DateTime.Now.AddYears(1) : _app.Deadline.DropSeconds();
            TpPicker.MinimumDate = RoundUp(DateTime.Now);
            TpPicker.SelectedDate = RoundUp(DateTime.Now);
            BtInstall.IsEnabled = !_app.InstallState.Equals("Installed");
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

            if (_nextServiceTime != null && !_app.InstallState.Equals("Installed"))
            {
                if (_nextServiceTime < _app.Deadline)
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
                    StatusText.Text = Globals.Settings.RequiredAppsSettings.CoveredByServiceCycleText.Replace("%TIME%", _nextServiceTime.ToString());
                    return;
                }
            }

            SetStatus(_app);
        }

        public RequiredAppsControl(CMApplication app, DateTime enforcementTime, long id, DateTime? nextServiceTime)
        {
            InitializeComponent();
            _requiredAppsSettings = Globals.Settings.RequiredAppsSettings;
            _nextServiceTime = nextServiceTime;
            _app = app;
            DataContext = _app;
            _isReschedule = true;
            _enforcementTime = enforcementTime;
            _scheduleId = id;

            TpPicker.SelectedDate = _enforcementTime;
            TpPicker.MaximumDate = _nextServiceTime != null ? ((DateTime)_nextServiceTime).DropSeconds() : _app.Deadline.DropSeconds();
            TpPicker.MinimumDate = RoundUp(DateTime.Now);

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

            if (_nextServiceTime != null && !_app.InstallState.Equals("Installed"))
            {
                if (_enforcementTime > _nextServiceTime)
                {
                    if (nextServiceTime <= RoundUp(DateTime.Now).AddMinutes(5))
                    {
                        TpPicker.MinimumDate = ((DateTime)_nextServiceTime).AddMinutes(-10);
                        TimeGrid.IsEnabled = false;
                        ButtonGrid.IsEnabled = false;
                        _timer.Stop();
                    }

                    TpPicker.MaximumDate = (DateTime)_nextServiceTime;
                    TpPicker.SelectedDate = (DateTime)_nextServiceTime;

                    StatusGreen.Visibility = Visibility.Visible;
                    StatusText.Text = Globals.Settings.RequiredAppsSettings.CoveredByServiceCycleText.Replace("%TIME%", _nextServiceTime.ToString());
                    return;
                }
            }

            SetStatus(_app);
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
            //TpPicker.Expanded = false;
            BtInstall.IsEnabled = false;
            BtSchedule.IsEnabled = false;
            BtRepair.IsEnabled = false;

            if (_isReschedule)
            {
                SqlCe.UpdateAppSchedule(_scheduleId, TpPicker.SelectedDate);
                Globals.Log.Information($"Updated schedule: {TpPicker.SelectedDate} Required application: '{_app.Name}'");

                /*
                SqlCe.UpdateAppSchedule(_scheduleId, TpPicker.PickedTime);
                Globals.Log.Information($"Updated schedule: {TpPicker.PickedTime.ToString()} Required application: '{_app.Name}'");
                */
            }
            else if (!_app.InstallState.Equals("Installed"))
            {
                _isReschedule = true;
                SqlCe.CreateAppSchedule(TpPicker.SelectedDate, _app.Id, _app.Revision);
                Globals.Log.Information($"Created schedule: {TpPicker.SelectedDate} Required application: '{_app.Name}'");

                /*
                SqlCe.CreateAppSchedule(TpPicker.PickedTime, _app.Id, _app.Revision);
                Globals.Log.Information($"Created schedule: {TpPicker.PickedTime.ToString()} Required application: '{_app.Name}'");
                */
            }
            else if (_app.InstallState.Equals("Installed"))
            {
                _isReschedule = true;
                SqlCe.CreateAppSchedule(TpPicker.SelectedDate, _app.Id, _app.Revision, false, "R");
                Globals.Log.Information($"Created schedule: {TpPicker.SelectedDate} Repair required application: '{_app.Name}'");
            }

            SetStatus(_app);
            BtInstall.IsEnabled = !_app.InstallState.Equals("Installed");
            BtSchedule.IsEnabled = true;
            BtRepair.IsEnabled = _app.InstallState.Equals("Installed") && _app.AllowedActions.Contains("Repair");
        }

        private void BtInstall_Click(object sender, RoutedEventArgs e)
        {
            BtInstall.IsEnabled = false;
            _isReschedule = false;

            if (_app.IsIpuApplication)
            {
                try
                {
                    var pipeCmd = new PipeCommand { Action = "InstallIpuApplication", AppId = _app.Id, AppRevision = _app.Revision };
                    var jsonCmd = JsonConvert.SerializeObject(pipeCmd);
                    new PipeClient().Send(jsonCmd, "3A2CD127-D069-4CD5-994D-C481F4760748");
                }
                catch (Exception ex)
                {
                    Globals.Log.Error(ex.Message);
                }
            }
            else
            {
                CcmUtils.InstallApplication(_app);
            }
        }

        private void BtRepair_Click(object sender, RoutedEventArgs e)
        {
            BtRepair.IsEnabled = false;
            _isReschedule = false;
            CcmUtils.RepairApplication(_app);
        }

        private async Task EvaluateApplication()
        {
            BtInstall.IsEnabled = false;
            BtRepair.IsEnabled = false;
            BtSchedule.IsEnabled = false;
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
                    Globals.MainWnd.SpApps.Children.Remove(this);
                }
            });

            ProgressbarEnforcement.Visibility = Visibility.Hidden;
            BtInstall.IsEnabled = !tmpApp.InstallState.Equals("Installed");
            BtRepair.IsEnabled = tmpApp.InstallState.Equals("Installed") && _app.AllowedActions.Contains("Repair");
            TpPicker.IsEnabled = !tmpApp.InstallState.Equals("Installed") || (tmpApp.InstallState.Equals("Installed") && _app.AllowedActions.Contains("Repair"));
            //TpPicker.Expanded = !tmpApp.InstallState.Equals("Installed");
            BtSchedule.IsEnabled = !tmpApp.InstallState.Equals("Installed") || (tmpApp.InstallState.Equals("Installed") && _app.AllowedActions.Contains("Repair"));
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
                    StatusText.Text = _requiredAppsSettings.AppIsInstalledStatusText;
                    return;
                }

                switch (app.EvaluationState)
                {
                    case 1:
                    case 3:
                        if (_isReschedule)
                        {
                            StatusGreen.Visibility = Visibility.Visible;
                            StatusText.Text = _requiredAppsSettings.InstallationHasBeenScheduledStatusText;
                        }
                        else
                        {
                            StatusOrange.Visibility = Visibility.Visible;
                            StatusText.Text = _requiredAppsSettings.AppNeedsAttentionStatusText;
                        }

                        break;

                    case 4:
                    case 16:
                    case 24:
                    case 25:
                        StatusRed.Visibility = Visibility.Visible;
                        StatusText.Text = _requiredAppsSettings.AppIsInErrorStateStatusText;
                        break;

                    default:
                        StatusOrange.Visibility = Visibility.Visible;
                        StatusText.Text = app.EvaluationState == 13 ? "Reboot pending." : _requiredAppsSettings.AppIsBeingEnforcedStatusText;
                        BtInstall.IsEnabled = false;
                        BtRepair.IsEnabled = false;
                        BtSchedule.IsEnabled = false;
                        ProgressbarEnforcement.Visibility = Visibility.Visible;

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
            Globals.CcmWmiEventListener.OnStatusChange -= CcmWmiEventListener_OnStatusChange;
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
