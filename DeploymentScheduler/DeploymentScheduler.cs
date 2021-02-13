using System;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using SchedulerCommon.Ccm;
using SchedulerCommon.Common;
using SchedulerCommon.Extensions;
using SchedulerCommon.IpuUtils;
using SchedulerCommon.NativeMethods;
using SchedulerCommon.Pipes;
using SchedulerCommon.RebootWatcher;
using SchedulerCommon.RegistryMethods;
using SchedulerCommon.Sql;
using SchedulerSettings;
using SchedulerSettings.Models;
using SchedulerSettings.Utils;

namespace DeploymentScheduler
{
    public partial class DeploymentScheduler : ServiceBase
    {
        private const int _fiveMinuteIntervalsPerDay = 288;
        private readonly System.Timers.Timer _enforcementTimer = new System.Timers.Timer();
        private readonly BlockingCollection<ScheduledObject> _objectsToEnforce = new BlockingCollection<ScheduledObject>(new ConcurrentQueue<ScheduledObject>());
        private readonly PipeClient _pipeClient = new PipeClient();
        private readonly string _userApp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserScheduler.exe");
        private readonly string _trayApp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OnevinnTrayIcon.exe");
        private readonly string _progressApp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IpuProgress.exe");
        private double _iEnforcementInterval = 5;
        private int _rebootToastDelay = 0;
        private bool _rebootServicingCheck = true;
        private bool _allowFastReboot = false;
        private Settings _settings;
        private ServiceConfig _serviceSettings;
        private CcmWmiEventListener _ccmWmiEventListener;
        private PipeServer _pipeServer;
        private DateTime? _lastFoundSups = null;

        public DeploymentScheduler()
        {
            InitializeComponent();
            CanHandleSessionChangeEvent = true;
        }

        protected override void OnStart(string[] args)
        {
            Globals.Log.Information("Service is starting");
            WaitForCcmExec();
            WaitForCmClientOperational();
            SqlCe.MaintainDatabase();
            _pipeServer = new PipeServer();
            _pipeServer.Listen("3A2CD127-D069-4CD5-994D-C481F4760748");
            _pipeServer.PipeMessage += PipeServer_PipeMessage;

            _settings = SettingsUtils.Settings;

            if (_settings.IsDefault)
            {
                SettingsUtils.WriteSettingsToFile();
                Globals.Log.Warning("Failed to load settings from file, created new 'Settings.xml'. If this is the first start after installation, it's expected.");
            }

            _serviceSettings = _settings.ServiceConfig;

            if (SqlCe.GetAutoEnforceFlag())
            {
                Globals.Log.Information("Auto Enforce Flag is true, enforcing all.");

                _iEnforcementInterval = 1;
                CcmUtils.InstallAllAppsAndUpdates();

                if (_settings.LegalNotice.UseLegalNotice)
                {
                    Reg.SetLegalNotice(_settings.LegalNotice);
                }
            }

            SqlCe.SetUpdatesInstallStatusFlag(false);
            SaveAssignmentsToDB();

            _enforcementTimer.AutoReset = false;
            _enforcementTimer.Elapsed += EnforcementTimer_Elapsed;
            _enforcementTimer.Interval = AutoInterval();
            _enforcementTimer.Start();

            _ccmWmiEventListener = new CcmWmiEventListener();
            _ccmWmiEventListener.OnNewApplication += CcmWmiEventListener_OnNewApplication;
            _ccmWmiEventListener.OnNewUpdate += CcmWmiEventListener_OnNewUpdate;

            FeedEnforcementsPump();
            EnforcePump();
            ConfirmPump();

            if (_serviceSettings.HardSuppressSCNotifications)
            {
                RenameSCNotification();
            }

            if (UnsafeNativeMethods.IsUserLoggedOn())
            {
                UnsafeNativeMethods.Run(_trayApp, null, false);
            }

            Globals.Log.Information("Service sucessfully started.");
        }

        protected override void OnStop()
        {
            if (_serviceSettings.HardSuppressSCNotifications)
            {
                RestoreSCNotification();
            }

            _pipeClient.Send("Close", "01DB94E3-90F1-43F4-8DDA-8AEAF6C08A8E");

            if (SqlCe.GetAutoEnforceFlag())
            {
                var reqApps = CcmUtils.RequiredApps.Where(x => !x.InstallState.Equals("Installed")).ToList();
                var pendingUpdates = CcmUtils.Updates.Where(x => x.EvaluationState == 1).ToList();

                if (reqApps.Count() == 0 && pendingUpdates.Count() == 0 && !RebootChecker.RebootRequired(new RestartChecks() { PendingFileOperations = true }).Any)
                {
                    Globals.Log.Information("Resetting Auto Enforce Flag, nothing left to enforce.");
                    SqlCe.SetAutoEnforceFlag(false);
                    _pipeClient.Send("SetBlue", "01DB94E3-90F1-43F4-8DDA-8AEAF6C08A8E");
                    _iEnforcementInterval = 5;

                    if (UnsafeNativeMethods.IsUserLoggedOn())
                    {
                        UnsafeNativeMethods.Run(_userApp, $"/ToastServiceEndNotification", false);
                    }

                    if (_settings.LegalNotice.UseLegalNotice)
                    {
                        Reg.RemoveLegalNotice();
                    }
                }
            }

            Globals.Log.Information("Service is stopping");
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            if (changeDescription.Reason == SessionChangeReason.SessionLogon || changeDescription.Reason == SessionChangeReason.SessionUnlock)
            {
                var enforcemets = SqlCe.GetPendingSchedules();

                if (enforcemets.Where(x => x.EnforcementTime < DateTime.Today.AddDays(2)).Any())
                {
                    Task.Run(() =>
                    {
                        System.Threading.Thread.Sleep(20000);
                        UnsafeNativeMethods.Run(_userApp, $"/ToastNotifyComingInstallation", false);
                    });
                }

                if (SqlCe.GetAutoEnforceFlag())
                {
                    UnsafeNativeMethods.Run(_userApp, $"/ToastServiceRunningNotification", false);
                }

                _rebootToastDelay = 0;

                var rs = SqlCe.GetRestartSchedule();

                if (rs != null)
                {
                    if (rs.IsExpress)
                    {
                        SqlCe.DeleteRestartSchedule();
                    }

                    RestartRequired();
                }
            }

            if (changeDescription.Reason == SessionChangeReason.SessionLogon)
            {
                UnsafeNativeMethods.Run(_trayApp, null, false);

                if (_settings.IpuApplication.ShowProgress && _ipuRunning)
                {
                    UnsafeNativeMethods.Run(_progressApp, string.Empty, false);
                }
            }
        }

        private void PipeServer_PipeMessage(object sender, PipeEventArg e)
        {
            try
            {
                if (e.Message.Contains("IPUSUCCESS"))
                {
                    IpuSuccess();
                    return;
                }

                if (e.Message.Contains("IPUERROR"))
                {
                    IpuError();
                    return;
                }

                if (e.Message.Contains("STARTPROGRESS"))
                {
                    UnsafeNativeMethods.Run(_progressApp, string.Empty, false);
                    return;
                }

                var cmd = JsonConvert.DeserializeObject<PipeCommand>(e.Message);

                switch (cmd.Action)
                {
                    case "ResetAutoEnforceFlag":
                        ResetAutoEnforceFlag();
                        break;

                    case "InstallIpuApplication":
                        InstallIpuApplication(new CMApplication { Id = cmd.AppId, Revision = cmd.AppRevision });
                        break;
                }
            }
            catch (Exception ex)
            {
                Globals.Log.Error(ex.Message);
            }
        }

        private void ResetAutoEnforceFlag()
        {
            Task.Run(() =>
            {
                Globals.Log.Information("Resetting Auto Enforce Flag on user request - tray command.");

                SqlCe.SetAutoEnforceFlag(false);
                _iEnforcementInterval = 5;

                if (UnsafeNativeMethods.IsUserLoggedOn())
                {
                    UnsafeNativeMethods.Run(_userApp, $"/ToastServiceEndNotification", false);
                }

                if (_settings.LegalNotice.UseLegalNotice)
                {
                    Reg.RemoveLegalNotice();
                }
            });
        }

        private void WaitForCcmExec()
        {
            while (!Process.GetProcessesByName("CcmExec").Any())
            {
                Globals.Log.Information("Waiting 15 s for 'CcmExec'.");
                System.Threading.Thread.Sleep(15000);
            }

            Globals.Log.Information("Detected 'CcmExec' is running continuing start.");
        }

        private void WaitForCmClientOperational()
        {
            while (!CcmUtils.IsCmClientOperational())
            {
                Globals.Log.Information("Waiting 15 s for 'SMS_Client' to get operational.");
                System.Threading.Thread.Sleep(15000);
            }

            Globals.Log.Information("Detected 'SMS_Client' is operational continuing start.");
        }

        private bool _onNewUpdateRunning = false;

        private void CcmWmiEventListener_OnNewUpdate(object sender, NewUpdateEventArg e)
        {
            if (_onNewUpdateRunning)
            {
                return;
            }

            _onNewUpdateRunning = true;

            Task.Run(() =>
            {
                System.Threading.Thread.Sleep(60000);

                try
                {
                    CheckForSups();
                }
                catch (Exception ex)
                {
                    Globals.Log.Error(ex.Message);
                }

                _onNewUpdateRunning = false;
            }).Wait();
        }

        private void IpuSuccess()
        {
            Task.Run(() =>
            {
                RegistryMethods.DeleteProgressStatus();
                SqlCe.SetIpuInstallTime(_runningIpuApp.DeploymentTypeId, _runningIpuApp.DeploymentTypeRevision, DateTime.Now);

                if (UnsafeNativeMethods.IsUserLoggedOn())
                {
                    UnsafeNativeMethods.Run(_userApp, "/ShowIpuDialog2", true);
                }

                _runningIpuApp = null;

                RestartComputer(true);
            });
        }

        private void IpuError()
        {
            Task.Run(() =>
            {
                RegistryMethods.DeleteProgressStatus();
                _ipuRunning = false;
                _runningIpuApp = null;
                RegistryMethods.RemoveIpuIsRunning();
            });
        }

        private bool _ipuRunning = false;

        private CMApplication _runningIpuApp;

        private void InstallIpuApplication(CMApplication cmApp)
        {
            if (_ipuRunning)
            {
                return;
            }

            _ipuRunning = true;

            Task.Run(() =>
            {
                _runningIpuApp = CcmUtils.GetSpecificApp(new ScheduledObject { ObjectId = cmApp.Id, Revision = cmApp.Revision });

                if (_runningIpuApp == null)
                {
                    Globals.Log.Error($"Failed to get specific application, Id '{cmApp.Id}' Revision: '{cmApp.Revision}'");
                    _ipuRunning = false;
                    RegistryMethods.RemoveIpuIsRunning();
                    return;
                }

                var contentInfo = SqlCe.GetContentStatus(_runningIpuApp.DeploymentTypeId, _runningIpuApp.DeploymentTypeRevision);

                if (contentInfo != null)
                {
                    if (contentInfo.IsDownloaded && contentInfo.InstallTime < DateTime.Now.AddMinutes(-30))
                    {
                        Globals.Log.Information($"Detected request for upgrade, IpuApplication at '{contentInfo.Location}' - attempting upgrade.");
                        RegistryMethods.SetIpuIsRunning();
                        CcmUtils.InstallApplication(_runningIpuApp);
                    }
                    else
                    {
                        Globals.Log.Information($"Detected request for upgrade, IpuApplication at '{contentInfo.Location}' - content not yet downloaded or already installed, skipping.");
                        _ipuRunning = false;
                        RegistryMethods.RemoveIpuIsRunning();
                    }
                }
                else
                {
                    Globals.Log.Information($"Detected request for upgrade, IpuApplication DT Id '{_runningIpuApp.DeploymentTypeId}' Revision '{_runningIpuApp.DeploymentTypeRevision}' - content status returned Null, skipping.");
                    _ipuRunning = false;
                    RegistryMethods.RemoveIpuIsRunning();
                }
            });
        }

        private void KillIpuProgress()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("ipuprogress"))
                {
                    p.Kill();
                }
            }
            catch { }
        }

        private bool _onNewApplicationRunning = false;

        private void CcmWmiEventListener_OnNewApplication(object sender, NewApplicationEventArg e)
        {
            if (_onNewApplicationRunning)
            {
                return;
            }

            _onNewApplicationRunning = true;

            Task.Run(() =>
            {
                try
                {
                    CheckForApps();
                }
                catch (Exception ex)
                {
                    Globals.Log.Error(ex.Message);
                }

                _onNewApplicationRunning = false;
            }).Wait();
        }

        private void EnforcementTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _enforcementTimer.Stop();
            _enforcementTimer.Interval = AutoInterval();
            _enforcementTimer.Start();

            SaveAssignmentsToDB();

            if (SqlCe.GetAutoEnforceFlag())
            {
                Globals.Log.Information("Checking Service cycle status.");

                var reqApps = CcmUtils.RequiredApps.Where(x => !x.InstallState.Equals("Installed") && x.EvaluationState == 3 && !x.IsIpuApplication).ToList();
                var pendingUpdates = CcmUtils.Updates.ToList();

                if (reqApps.Count() == 0 && pendingUpdates.Count() == 0 && !(CcmUtils.IsUpdatesEnforcing() || CcmUtils.IsAppsEnforcing()) && !RebootChecker.RebootRequired(new RestartChecks() { PendingFileOperations = true }).Any)
                {
                    Globals.Log.Information("Service cycle - Resetting Auto Enforce Flag, nothing left to enforce and no reboot pending.");
                    SqlCe.SetAutoEnforceFlag(false);
                    _pipeClient.Send("SetBlue", "01DB94E3-90F1-43F4-8DDA-8AEAF6C08A8E");
                    _iEnforcementInterval = 5;

                    if (UnsafeNativeMethods.IsUserLoggedOn())
                    {
                        UnsafeNativeMethods.Run(_userApp, $"/ToastServiceEndNotification", false);
                    }

                    if (_settings.LegalNotice.UseLegalNotice)
                    {
                        Reg.RemoveLegalNotice();
                    }

                    return;
                }
                else if (CcmUtils.IsRebootPending().HardRebootPending && !(CcmUtils.IsUpdatesEnforcing() || CcmUtils.IsAppsEnforcing()))
                {
                    Globals.Log.Information("Service cycle - Pending hard reboot detected - restarting computer.");

                    if (UnsafeNativeMethods.IsUserLoggedOn())
                    {
                        UnsafeNativeMethods.Run(_userApp, $"/ToastServiceRestartNotification", false);
                        RestartComputer();
                    }
                    else
                    {
                        RestartComputer(true);
                    }

                    return;
                }
                else if (!(CcmUtils.IsUpdatesEnforcing() || CcmUtils.IsAppsEnforcing()) && (reqApps.Count() != 0 || pendingUpdates.Count() != 0))
                {
                    Globals.Log.Information("Service cycle - Cycle running but required installations not enforcing - pushing enforcement.");
                    CcmUtils.InstallAllAppsAndUpdates();
                    return;
                }
                else if (reqApps.Count() == 0 && pendingUpdates.Count() == 0 && !(CcmUtils.IsUpdatesEnforcing() || CcmUtils.IsAppsEnforcing()) && RebootChecker.RebootRequired(new RestartChecks() { PendingFileOperations = true }).Any)
                {
                    Globals.Log.Information("Service cycle - nothing left to enforce but a pending reboot (Any) was detected - restarting computer.");

                    if (UnsafeNativeMethods.IsUserLoggedOn())
                    {
                        UnsafeNativeMethods.Run(_userApp, $"/ToastServiceRestartNotification", false);
                        RestartComputer();
                    }
                    else
                    {
                        RestartComputer(true);
                    }

                    return;
                }

                Globals.Log.Information("Service cycle not yet finished.");

                return;
            }

            if (CcmUtils.IsUpdatesEnforcing())
            {
                return;
            }

            var rs = SqlCe.GetRestartSchedule();

            if (rs != null)
            {
                if (rs.RestartTime <= DateTime.Now)
                {
                    SqlCe.DeleteRestartSchedule();

                    if (UnsafeNativeMethods.IsUserLoggedOn())
                    {
                        UnsafeNativeMethods.Run(_userApp, $"/ShowRestartWindow", false);
                        Globals.Log.Information($"Launched Restart Window.");
                    }
                    else
                    {
                        RestartComputer(true);
                    }

                    return;
                }
            }

            RestartRequired();

            if (CcmUtils.IsRebootPending().HardRebootPending)
            {
                Globals.Log.Warning($"Detected pending hard reboot - postponing further enforcement(s)");
                return;
            }

            try
            {
                var serviceSchedule = CommonUtils.GetNextServiceCycleAsDateTime();

                if (serviceSchedule != null)
                {
                    if (serviceSchedule <= DateTime.Now)
                    {
                        SqlCe.SetAutoEnforceFlag(true);
                        _pipeClient.Send("SetRed", "01DB94E3-90F1-43F4-8DDA-8AEAF6C08A8E");
                        CcmUtils.InstallAllAppsAndUpdates();
                        _iEnforcementInterval = 1;
                        _enforcementTimer.Stop();
                        _enforcementTimer.Interval = AutoInterval();
                        _enforcementTimer.Start();

                        if (_settings.LegalNotice.UseLegalNotice)
                        {
                            Reg.SetLegalNotice(_settings.LegalNotice);
                        }

                        SqlCe.DeleteServiceSchedule();
                        SqlCe.UpdateSupData("STD", string.Empty);

                        if (UnsafeNativeMethods.IsUserLoggedOn())
                        {
                            UnsafeNativeMethods.Run(_userApp, $"/ToastServiceInitNotification", false);
                        }

                        Globals.Log.Information($"Found and triggered a Service Schedule (Install All)");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Globals.Log.Error(ex.Message);
            }

            try
            {
                var sch = SqlCe.GetDueSchedules();

                if (sch.Count() > 0)
                {
                    Globals.Log.Information($"Found {sch.Count()} objects due for enforcement.");
                    sch.ForEach(x => _objectsToEnforce.Add(x));
                }
                else
                {
                    Globals.Log.Information($"No scheduled enforcements found at this time");
                }
            }
            catch (Exception ex)
            {
                Globals.Log.Error(ex.Message);
            }

            try
            {
                if (CcmUtils.IsApplicationEvaluationRequired())
                {
                    CcmUtils.EvaluateAllPolicies();
                }
            }
            catch (Exception ex)
            {
                Globals.Log.Error(ex.Message);
            }
        }

        private double AutoInterval()
        {
            var min = _iEnforcementInterval;
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

            return t + 50;
        }

        private void ConfirmPump()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        if (!UnsafeNativeMethods.IsUserLoggedOn())
                        {
                            System.Threading.Thread.Sleep(60 * 60000);
                            continue;
                        }

                        var needConfirm = false;
                        var arg = "/ShowConfirmWindow";

                        var allPendingSchedules = SqlCe.GetPendingSchedules();
                        var fourHourLimitSchedules = allPendingSchedules.Where(x => x.EnforcementTime <= DateTime.Now.AddHours(4) && !x.HasRaisedConfirm);

                        foreach (var schedule in fourHourLimitSchedules)
                        {
                            SqlCe.SetHasRaisedConfirm(schedule.Id);
                            needConfirm = true;
                        }

                        var dtServiceTime = CommonUtils.GetNextServiceCycleAsDateTime();

                        if (dtServiceTime != null)
                        {
                            var tempServiceAck = SqlCe.GetLastServiceAck();
                            var lastServiceAck = tempServiceAck == DateTime.MaxValue ? DateTime.MinValue : tempServiceAck.AddHours(4);

                            if (dtServiceTime <= DateTime.Now.AddHours(4) && lastServiceAck <= DateTime.Now)
                            {
                                arg += " /ServiceSchedule";
                                SqlCe.SetLastServiceAck();
                                needConfirm = true;
                            }
                        }

                        if (needConfirm)
                        {
                            Globals.Log.Information("Confirmation required, launching confirm dialog.");
                            UnsafeNativeMethods.Run(_userApp, arg, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Globals.Log.Error(ex.Message);
                    }

                    System.Threading.Thread.Sleep(60 * 60000);
                }
            });
        }

        private string GetCDataFromString(string xml)
        {
            var xTemp = XElement.Parse(xml);
            var cData = from element in xTemp.DescendantNodes()
                                where element.NodeType == XmlNodeType.CDATA
                                select element.Parent.Value.Trim();

            return cData.Any() ? cData.FirstOrDefault() : null;
        }

        private string DecryptVariableValue(string value)
        {
            var byteData = new byte[(value.Length / 2) - 4];

            for (var i = 0; i < ((value.Length / 2) - 4); i++)
            {
                byteData[i] = Convert.ToByte(value.Substring((i + 4) * 2, 2), 16);
            }

            var decryptedbytes = ProtectedData.Unprotect(byteData, null, DataProtectionScope.CurrentUser);

            return Encoding.Unicode.GetString(decryptedbytes);
        }

        private string _oldSettings = string.Empty;

        private void LookForNewSettings()
        {
            var settingFile = string.Empty;

            var collVars = CcmUtils.GetSettingsVariables();

            if (collVars.Count() < 2)
            {
                return;
            }

            collVars = collVars.OrderBy(x => x.Name).ToList();

            foreach (var collVar in collVars)
            {
                var tmp = GetCDataFromString(collVar.Value);

                if (tmp == null)
                {
                    return;
                }

                settingFile += DecryptVariableValue(tmp).Trim().TrimEnd('\0');
            }

            if (!string.IsNullOrEmpty(settingFile))
            {
                settingFile = StringCompressor.DecompressString(settingFile);
            }

            if (settingFile.StartsWith("<?xml") && settingFile.EndsWith("</Settings>") && !settingFile.Equals(_oldSettings))
            {
                SettingsUtils.WriteStringToSettingsFile(settingFile);
                Globals.Log.Information("Wrote new settings to 'Settings.xml'");
                _oldSettings = settingFile;
            }
            else
            {
                Globals.Log.Information("No new settings found in Policy");
            }
        }

        private void FeedEnforcementsPump()
        {
            Task.Run(() =>
            {
                var firstCheck = true;

                while (true)
                {
                    try
                    {
                        LookForNewSettings();
                    }
                    catch (Exception ex)
                    {
                        Globals.Log.Error(ex.Message);
                    }

                    try
                    {
                        _settings = SettingsUtils.Settings;
                    }
                    catch (Exception ex)
                    {
                        Globals.Log.Error(ex.Message);
                    }

                    try
                    {
                        if (!_onNewUpdateRunning)
                        {
                            CheckForSups();
                        }
                    }
                    catch (Exception ex)
                    {
                        Globals.Log.Error(ex.Message);
                    }

                    try
                    {
                        if (!_onNewApplicationRunning)
                        {
                            CheckForApps();
                        }
                    }
                    catch (Exception ex)
                    {
                        Globals.Log.Error(ex.Message);
                    }

                    if (firstCheck)
                    {
                        firstCheck = false;
                        System.Threading.Thread.Sleep(6 * 60000);
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(_serviceSettings.DetectionInterval * 60000);
                    }
                }
            });
        }

        private bool RestartRequired()
        {
            if (CcmUtils.IsUpdatesEnforcing() || CcmUtils.IsAppsEnforcing())
            {
                Globals.Log.Information("Detected ongoing installation, suppressing reboot checks.");
                return false;
            }

            var checks = _settings.RestartChecks;

            var updatesStatus = _rebootServicingCheck && !SqlCe.GetUpdatesInstallStatusFlag();

            if (!updatesStatus)
            {
                checks.ComponentBasedServicing = updatesStatus;
                checks.PendingFileOperations = updatesStatus;
                checks.WindowsUpdate = updatesStatus;
            }

            var result = RebootChecker.RebootRequired(checks);
            var rs = SqlCe.GetRestartSchedule();

            if (!result.Any)
            {
                SqlCe.DeleteRestartSchedule();
            }
            else if (rs != null)
            {
                if (rs.IsAcknowledged)
                {
                    return false;
                }
            }

            if (result.ConfigMgrClient && (_allowFastReboot || !UnsafeNativeMethods.IsUserLoggedOn()))
            {
                SqlCe.DeleteRestartSchedule();

                SqlCe.SetRestartSchedule(new RestartSchedule
                {
                    DeadLine = DateTime.Now.MinutePrecission().AddMinutes(5),
                    RestartTime = DateTime.Now.MinutePrecission().AddMinutes(5),
                    IsAcknowledged = true,
                    IsExpress = true,
                });

                return result.Any;
            }

            if (result.Any && _rebootToastDelay-- <= 1)
            {
                if (rs == null)
                {
                    SqlCe.SetRestartSchedule(new RestartSchedule
                    {
                        DeadLine = DateTime.Today.AddDays(_serviceSettings.RestartDeadlineAfterInstall.InDays).AddHours(_serviceSettings.RestartDeadlineAfterInstall.AtHour),
                        RestartTime = DateTime.Today.AddDays(_serviceSettings.RestartDeadlineAfterInstall.InDays).AddHours(_serviceSettings.RestartDeadlineAfterInstall.AtHour),
                        IsAcknowledged = false,
                        IsExpress = false,
                    });
                }

                if (SqlCe.GetAutoEnforceFlag())
                {
                    Globals.Log.Information($"Detected auto enforce flag 'true', skipping reboot toast.");
                }
                else if (!UnsafeNativeMethods.IsSessionLocked())
                {
                    UnsafeNativeMethods.Run(_userApp, $"/ToastReboot", false);
                }
                else
                {
                    Globals.Log.Information($"Detected a locked or non existing user session, a Toast notification was suppressed.");
                }

                _rebootToastDelay = _fiveMinuteIntervalsPerDay / _serviceSettings.NumberOfRestartToastsPerDay;
            }

            return result.Any;
        }

        private void CheckForSups()
        {
            if (_supCheckBlocked || SqlCe.GetAutoEnforceFlag())
            {
                return;
            }

            try
            {
                var sups = CcmUtils.Updates;

                if (sups.Count() == 0)
                {
                    _lastFoundSups = null;
                    Globals.Log.Information("No updates available at this time");
                    return;
                }

                _lastFoundSups = _lastFoundSups ?? DateTime.Now;

                if (_lastFoundSups > DateTime.Now.AddMinutes(-5))
                {
                    return;
                }

                var jsonSups = JsonConvert.SerializeObject(sups);
                SqlCe.UpdateSupData("STD", jsonSups);

                Globals.Log.Information($"Found {sups.Count()} updates available for scheduling");

                var nextSchedule = SqlCe.GetNextSupSchedule();
                var supdeadline = sups.OrderBy(y => y.Deadline).First().Deadline;
                var nextServiceCycle = SqlCe.GetServiceSchedule();

                if (nextSchedule.Id != 0)
                {
                    Globals.Log.Information($"Next update enforcement time '{nextSchedule.EnforcementTime}' deadline for available updates = '{supdeadline}', 'toasting' user - reschedule necessary");

                    if (nextSchedule.EnforcementTime <= supdeadline)
                    {
                        Globals.Log.Information($"Next update enforcement time '{nextSchedule.EnforcementTime}' deadline for available updates = '{supdeadline}', skipping 'toast'");
                        return;
                    }
                    else
                    {
                        SqlCe.SetEnforcedFlag(nextSchedule.Id);
                    }
                }

                var dtNextServiceCycle = CommonUtils.GetNextServiceCycleAsDateTime();

                if (dtNextServiceCycle != null)
                {
                    if (dtNextServiceCycle <= supdeadline)
                    {
                        Globals.Log.Information($"Detected an upcoming service cycle, a Toast notification was suppressed.");
                        return;
                    }
                }

                if (UnsafeNativeMethods.IsUserLoggedOn() && !RegistryMethods.GetIpuIsRunning())
                {
                    if (!UnsafeNativeMethods.IsSessionLocked())
                    {
                        UnsafeNativeMethods.Run(_userApp, $"/ToastSup", false);
                    }
                    else
                    {
                        Globals.Log.Information($"Detected a locked or non existing user session, a Toast notification was suppressed.");
                    }
                }
            }
            catch (Exception ex)
            {
                Globals.Log.Error(ex.Message);
            }
        }

        private void CheckForApps()
        {
            try
            {
                if (SqlCe.GetAutoEnforceFlag())
                {
                    return;
                }

                var rApps = CcmUtils.RequiredApps;

                var allRequiredAppsApps = rApps.Where(x => !x.InstallState.Equals("Installed") && x.Deadline > DateTime.Now.AddMinutes(15)).ToList();

                if (allRequiredAppsApps.Count < 1)
                {
                    Globals.Log.Information($"Wmi didn't return any required applications at this time.");
                    return;
                }

                var appDeadline = allRequiredAppsApps.OrderBy(x => x.Deadline).First().Deadline;
                var allSchedules = SqlCe.GetAllSchedules();
                var showToast = false;
                var featureUpdateExists = false;
                var dtNextServiceCycle = CommonUtils.GetNextServiceCycleAsDateTime();

                foreach (var app in allRequiredAppsApps)
                {
                    if (allSchedules.Where(x => x.ObjectId.Trim().Equals(app.Id.Trim()) && x.Revision.Trim().Equals(app.Revision.Trim())).Any())
                    {
                        Globals.Log.Information($"Skipping toast for already scheduled application: '{app.Name}'");
                        continue;
                    }

                    if (dtNextServiceCycle < app.Deadline)
                    {
                        Globals.Log.Information($"Skipping toast for application covered by service cycle: '{app.Name}'");
                        continue;
                    }

                    if (app.EvaluationState == 12)
                    {
                        Globals.Log.Information($"Skipping toast for currently installing (possibly long-running) app '{app.Name}'");
                        continue;
                    }

                    if (app.IsIpuApplication)
                    {
                        featureUpdateExists = true;
                    }

                    showToast = true;
                }

                if (dtNextServiceCycle != null)
                {
                    if (dtNextServiceCycle <= appDeadline)
                    {
                        Globals.Log.Information($"Detected an upcoming service cycle, a Toast notification was suppressed.");
                        return;
                    }
                }

                if (showToast && UnsafeNativeMethods.IsUserLoggedOn() && !RegistryMethods.GetIpuIsRunning())
                {
                    if (!UnsafeNativeMethods.IsSessionLocked())
                    {
                        var arg = featureUpdateExists ? "/ToastIpu" : "/ToastApp";
                        UnsafeNativeMethods.Run(_userApp, $"{arg}", false);
                    }
                    else
                    {
                        Globals.Log.Information($"Detected a locked or non existing user session, a Toast notification was suppressed.");
                    }
                }
            }
            catch (Exception ex)
            {
                Globals.Log.Error(ex.Message);
            }
        }

        private void EnforcePump()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var scheduledobject = _objectsToEnforce.Take();
                        _allowFastReboot = scheduledobject.IsAutoSchedule;

                        SqlCe.SetEnforcedFlag(scheduledobject.Id);

                        if (scheduledobject.EnforcementType.Equals("APP"))
                        {
                            var cmapp = CcmUtils.GetSpecificApp(scheduledobject);

                            if (cmapp == null)
                            {
                                Globals.Log.Warning($"A scheduled enforcement '{scheduledobject.ObjectId}/{scheduledobject.Revision}' could not be performed. Deployment has been removed or changed?");
                                continue;
                            }

                            if (scheduledobject.Action.Equals("I"))
                            {
                                if (cmapp.IsIpuApplication && _settings.IpuApplication.ShowDialog1 && UnsafeNativeMethods.IsUserLoggedOn())
                                {
                                    if (UnsafeNativeMethods.Run(_userApp, "/ShowIpuDialog1", true) == 0)
                                    {
                                        //RegistryMethods.SetIpuIsRunning();
                                        //CcmUtils.InstallApplication(cmapp);
                                        InstallIpuApplication(cmapp);
                                        Globals.Log.Information($"User pressed 'Install' feature updated '{cmapp.Name}'");
                                    }
                                    else
                                    {
                                        SqlCe.DeleteAppSchedule(cmapp.Id, cmapp.Revision);
                                        Globals.Log.Warning($"User aborted a scheduled Windows feature upgrade '{cmapp.Name}'");
                                    }
                                }
                                else if (cmapp.IsIpuApplication)
                                {
                                    //RegistryMethods.SetIpuIsRunning();
                                    InstallIpuApplication(cmapp);
                                    //CcmUtils.InstallApplication(cmapp);
                                }
                                else
                                {
                                    CcmUtils.InstallApplication(cmapp);
                                }
                            }
                            else if (scheduledobject.Action.Equals("R"))
                            {
                                CcmUtils.RepairApplication(cmapp);
                            }
                            else
                            {
                                CcmUtils.UninstallApplication(cmapp);
                            }
                        }
                        else if (scheduledobject.EnforcementType.Equals("SUP"))
                        {
                            Globals.Log.Information("Installing updates, disabling component servicing reboot detection until rebooted.");
                            _rebootServicingCheck = false;
                            Set_SupCheckBlocked();
                            SqlCe.UpdateSupData("STD", string.Empty);
                            CcmUtils.ExecuteInstallUpdates(true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Globals.Log.Error(ex.Message);
                    }
                }
            });
        }

        private bool _supCheckBlocked = false;

        private void Set_SupCheckBlocked()
        {
            Task.Run(() =>
            {
                _supCheckBlocked = true;
                System.Threading.Thread.Sleep(120000);
                _supCheckBlocked = false;
            });
        }

        private void SaveAssignmentsToDB()
        {
            var assignments = CcmUtils.GetAssignments();
            SqlCe.SaveAssignmentsToDB(assignments);
        }

        private void RestartComputer(bool fastReboot = false)
        {
            var conf = _settings.RestartConfig;

            try
            {
                Globals.Log.Information("RestartComputer");

                var arg = $"/r /f /t {conf.CountDown} /c \"{conf.WindowsRestartMessage.Line1}\n{conf.WindowsRestartMessage.Line2}\"";

                if (fastReboot)
                {
                    arg = $"/r /f /t 0";
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = arg,
                    WindowStyle = ProcessWindowStyle.Hidden,
                });

                _enforcementTimer.Stop();
            }
            catch (Exception ex)
            {
                Globals.Log.Error(ex.Message);
            }
        }

        private void RenameSCNotification()
        {
            try
            {
                var orgFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "CCM", "SCNotification.exe");
                var renamedFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "CCM", "SCNotification_.exe");

                if (File.Exists(orgFile))
                {
                    var procs = Process.GetProcessesByName("SCNotification");
                    foreach (var proc in procs)
                    {
                        proc.Kill();
                    }

                    File.Move(orgFile, renamedFile);
                    Globals.Log.Information("Renamed SCNotification.exe to SCNotification_.exe to suppress SC toasts");
                }
            }
            catch (Exception ex)
            {
                Globals.Log.Error(ex.Message);
            }
        }

        private void RestoreSCNotification()
        {
            try
            {
                var orgFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "CCM", "SCNotification.exe");
                var renamedFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "CCM", "SCNotification_.exe");

                if (File.Exists(renamedFile) && !File.Exists(orgFile))
                {
                    File.Move(renamedFile, orgFile);
                }

                Globals.Log.Information("Restored SCNotification.exe from SCNotification_.exe to enable SC toasts");
            }
            catch (Exception ex)
            {
                Globals.Log.Error(ex.Message);
            }
        }
    }
}
