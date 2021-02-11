using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;
using SchedulerCommon.Common;
using SchedulerCommon.Enums;
using SchedulerCommon.Logging;
using SchedulerCommon.Sql;
using SchedulerCommon.Wmi;
using SchedulerSettings;

namespace SchedulerCommon.Ccm
{
    public static class CcmUtils
    {
        private static readonly WmiEventSource _log = WmiEventSource.Log;
        private static readonly InstallEventSource _installLog = InstallEventSource.Log;
        private static readonly ObservableCollection<CMApplication> _requiredApps = new ObservableCollection<CMApplication>();
        private static readonly ObservableCollection<CMApplication> _availableApps = new ObservableCollection<CMApplication>();
        private static readonly ObservableCollection<Update> _updates = new ObservableCollection<Update>();
        private static readonly string _namespace_CCM = "ROOT\\ccm";
        private static readonly string _namespace_ClientSDK = "ROOT\\ccm\\ClientSDK";
        private static readonly string _namespace_DeploymentAgent = "ROOT\\ccm\\SoftwareUpdates\\DeploymentAgent";
        private static readonly string _namespace_Policy = "ROOT\\ccm\\Policy\\Machine\\ActualConfig";
        private static readonly string _namespace_CIModels = "ROOT\\ccm\\CIModels";
        private static readonly string _namespace_SoftMgmtAgent = "ROOT\\ccm\\SoftMgmtAgent";

        public static ObservableCollection<CMApplication> RequiredApps
        {
            get
            {
                BuildRequiredApps();
                return _requiredApps;
            }
        }

        public static ObservableCollection<CMApplication> AvailableApps
        {
            get
            {
                BuildAvailableApps();
                return _availableApps;
            }
        }

        public static ObservableCollection<Update> Updates
        {
            get
            {
                BuildUpdates();
                return _updates;
            }
        }

        public static List<SettingsVariable> GetSettingsVariables()
        {
            var retList = new List<SettingsVariable>();

            try
            {
                var result = ExecuteQuery(_namespace_Policy, "SELECT * FROM CCM_CollectionVariable WHERE Name Like 'DPLSCH%'");

                foreach (var instance in result)
                {
                    retList.Add(new SettingsVariable
                    {
                        Name = instance["Name"].StringValue,
                        Value = instance["Value"].StringValue,
                    });
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return retList;
        }

        public static List<Assignment> GetAssignments()
        {
            var retList = new List<Assignment>();

            try
            {
                var result = ExecuteQuery(_namespace_Policy, "SELECT * FROM CCM_ApplicationCIAssignment");

                foreach (var instance in result)
                {
                    var action = instance["AssignmentAction"].IntegralValue;
                    var assignedCIs = instance["AssignedCIs"].StringArrayValue;

                    if (assignedCIs.Length == 0)
                    {
                        continue;
                    }

                    foreach (var assignedCI in assignedCIs)
                    {
                        var assignment = GetScopeAndRevision(assignedCI);
                        assignment.Purpose = action == 0 ? AssignmentPurpose.Required : AssignmentPurpose.Available;
                        retList.Add(assignment);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return retList;
        }

        private static Assignment GetScopeAndRevision(string str)
        {
            var xml = XElement.Parse(str);
            var scopeId = xml.Elements("ID").First().Value;
            scopeId = scopeId.Substring(0, scopeId.LastIndexOf('/')).Replace("Required", string.Empty);
            var revision = xml.Elements("CIVersion").First().Value;

            return new Assignment
            {
                ScopeId = scopeId,
                Revision = revision,
            };
        }

        public static ObservableCollection<Update> GetUpdatesStatus()
        {
            var retList = new ObservableCollection<Update>();

            try
            {
                var result = ExecuteQuery(_namespace_ClientSDK, "SELECT * FROM CCM_SoftwareUpdate");

                foreach (var instance in result)
                {
                    retList.Add(new Update
                    {
                        UpdateId = instance["UpdateID"].StringValue,
                        StartTime = instance["StartTime"].DataTimeValue,
                        Name = instance["Name"].StringValue,
                        IsO365Update = instance["IsO365Update"].BooleanValue,
                        HasBeenEnforced = false,
                        EvaluationState = instance["EvaluationState"].IntegralValue,
                        PercentComplete = instance["PercentComplete"].IntegralValue,
                        ExclusiveUpdate = instance["ExclusiveUpdate"].BooleanValue,
                    });
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return retList;
        }

        public static bool IsUpdateInErrorState()
        {
            try
            {
                var result = ExecuteQuery(_namespace_ClientSDK, "SELECT * FROM CCM_SoftwareUpdate");

                foreach (var instance in result)
                {
                    if (instance["EvaluationState"].IntegralValue == 13)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return false;
        }

        public static bool IsUpdatesEnforcing()
        {
            try
            {
                var result = ExecuteQuery(_namespace_ClientSDK, "SELECT * FROM CCM_SoftwareUpdate");

                foreach (var instance in result)
                {
                    if (instance["EvaluationState"].IntegralValue >= 4 && instance["EvaluationState"].IntegralValue <= 7)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return false;
        }

        public static bool IsAppsEnforcing()
        {
            try
            {
                var result = ExecuteQuery(_namespace_ClientSDK, "SELECT * FROM CCM_Application");

                foreach (var instance in result)
                {
                    if (instance["EvaluationState"].IntegralValue == 11 || (instance["EvaluationState"].IntegralValue == 12 && !IsRebootPending().HardRebootPending))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return false;
        }

        /// <summary>
        /// Returns a list of pending updates.
        /// </summary>
        private static void BuildUpdates()
        {
            _updates.Clear();

            try
            {
                var result = ExecuteQuery(_namespace_ClientSDK, "SELECT * FROM CCM_SoftwareUpdate");

                foreach (var instance in result)
                {
                    if (instance["StartTime"].DataTimeValue > DateTime.Now)
                    {
                        continue;
                    }

                    if (instance["EvaluationState"].IntegralValue > 1)
                    {
                        continue;
                    }

                    var desc = instance["Description"].StringValue;

                    if (desc.ToLower().Contains("excludefromscheduler"))
                    {
                        continue;
                    }

                    var uid = instance["UpdateID"].StringValue;

                    var deadline = GetEnforcementTime(uid);

                    if (deadline == DateTime.MaxValue)
                    {
                        continue;
                    }

                    if (deadline <= instance["StartTime"].DataTimeValue)
                    {
                        continue;
                    }

                    _updates.Add(new Update
                    {
                        UpdateId = instance["UpdateID"].StringValue,
                        Deadline = deadline,
                        StartTime = instance["StartTime"].DataTimeValue,
                        Name = instance["Name"].StringValue,
                        Description = desc.Trim(),
                        IsO365Update = instance["IsO365Update"].BooleanValue,
                        HasBeenEnforced = false,
                        EvaluationState = instance["EvaluationState"].IntegralValue,
                        PercentComplete = instance["PercentComplete"].IntegralValue,
                        ExclusiveUpdate = instance["ExclusiveUpdate"].BooleanValue,
                    });
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        private static DateTime GetEnforcementTime(string updateId)
        {
            var retDt = DateTime.MaxValue;

            try
            {
                var sql = $"SELECT * FROM CCM_TargetedUpdateEx1 WHERE UpdateId = '{updateId.Trim()}'";

                var result = ExecuteQuery(_namespace_DeploymentAgent, sql);

                foreach (var instance in result)
                {
                    retDt = instance["Deadline"].DataTimeValue;
                    break;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return retDt;
        }

        public static bool IsApplicationEvaluationRequired()
        {
            var retVal = false;

            try
            {
                var sql = "SELECT * FROM CCM_Application";

                var result = ExecuteQuery(_namespace_ClientSDK, sql);

                foreach (var instance in result)
                {
                    var evalState = instance["EvaluationState"].IntegralValue;

                    if (evalState == 0)
                    {
                        if (!HasAppBeenEvaluated(instance["Id"].StringValue, instance["Revision"].StringValue))
                        {
                            _log.Warning($"'{instance["Name"].StringValue}' is in EvaluationState '{instance["EvaluationState"].IntegralValue}' - running evaluation");
                            retVal = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return retVal;
        }

        private static bool HasAppBeenEvaluated(string id, string revision)
        {
            if (SqlCe.IsApplicationEvaluated(new EvaluatedApplication { Id = id, Revision = revision }))
            {
                return true;
            }

            SqlCe.StoreEvaluatedAppIdRevision(new EvaluatedApplication { Id = id, Revision = revision });
            return false;
        }

        private static void BuildRequiredApps()
        {
            _requiredApps.Clear();
            var evalrequired = false;

            try
            {
                var sql = "SELECT * FROM CCM_Application";

                var result = ExecuteQuery(_namespace_ClientSDK, sql);

                foreach (var instance in result)
                {
                    if (!instance["ApplicabilityState"].StringValue.Equals("Applicable"))
                    {
                        continue;
                    }

                    var isIpuApplication = false;
                    var evalState = instance["EvaluationState"].IntegralValue;

                    if (evalState == 0)
                    {
                        evalrequired = true;
                        continue;
                    }

                    if (SqlCe.GetAssignment(instance["Id"].StringValue, instance["Revision"].StringValue) != AssignmentPurpose.Required)
                    {
                        continue;
                    }

                    if (instance["AllowedActions"].StringArrayValue.Count() == 0)
                    {
                        continue;
                    }

                    if (!instance["IsMachineTarget"].BooleanValue)
                    {
                        continue;
                    }

                    if (instance["StartTime"].DataTimeValue > DateTime.Now)
                    {
                        continue;
                    }

                    try
                    {
                        var cats = instance["Categories"].StringArrayValue;

                        if (cats.Contains("ExcludeFromScheduler"))
                        {
                            _log.Information($"Excluded '{instance["Name"].StringValue}' from Scheduler");
                            continue;
                        }

                        isIpuApplication = cats.Contains("IPUApplication");
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex.Message);
                    }

                    var deadline = GetDeadline(instance["Name"].StringValue, instance["Id"].StringValue, instance["Revision"].StringValue, instance["Deadline"].DataTimeValue);

                    if (deadline <= instance["StartTime"].DataTimeValue)
                    {
                        continue;
                    }

                    if (isIpuApplication && deadline.AddMinutes(-60) < DateTime.Now)
                    {
                        if (!SqlCe.IsAppScheduled(instance["Id"].StringValue, instance["Revision"].StringValue, out var dummy))
                        {
                            SqlCe.CreateAppSchedule(DateTime.Now.AddMinutes(15), instance["Id"].StringValue, instance["Revision"].StringValue);
                        }
                    }

                    string[] allowedActions = null;
                    var deploymentType = string.Empty;
                    var estimatedInstallTime = 0;

                    var instancePath = $"{_namespace_ClientSDK}:CCM_Application.Id=\"{instance["Id"].StringValue}\",IsMachineTarget={instance["IsMachineTarget"].StringValue},Revision=\"{instance["Revision"].StringValue}\"";
                    var isContentDownloaded = true;
                    var contentLocation = string.Empty;

                    using (var managementObject = new ManagementObject(new ManagementScope(_namespace_ClientSDK), new ManagementPath(instancePath), new ObjectGetOptions()))
                    {
                        managementObject.Get();
                        var resultObject = (IResultObject)new WmiResultObject(managementObject);
                        var dts = resultObject["AppDTs"].ObjectArrayValue;

                        foreach (var dt in dts)
                        {
                            if (!dt["ApplicabilityState"].StringValue.Equals("Applicable"))
                            {
                                continue;
                            }

                            if (isIpuApplication)
                            {
                                if (isContentDownloaded = IsContentDownloaded(dt["Id"].StringValue, dt["Revision"].StringValue, out var location))
                                {
                                    contentLocation = location;
                                    _log.Information($"Detected IPUApplication available in '{contentLocation}'");
                                }
                            }

                            allowedActions = dt["AllowedActions"].StringArrayValue;
                            deploymentType = dt["Id"].StringValue;
                            estimatedInstallTime = dt["EstimatedInstallTime"].IntegralValue;
                            break;
                        }
                    }

                    if (!isContentDownloaded)
                    {
                        continue;
                    }

                    var isInList = _requiredApps.Any(x => x.Id.Equals(instance["Id"].StringValue));

                    if (isInList)
                    {
                        var existing = _requiredApps.First(x => x.Id.Equals(instance["Id"].StringValue));

                        if (Convert.ToInt32(existing.Revision) < instance["Revision"].IntegralValue)
                        {
                            _requiredApps.Remove(existing);
                        }
                        else
                        {
                            continue;
                        }
                    }

                    _requiredApps.Add(new CMApplication
                    {
                        Id = instance["Id"].StringValue,
                        IsMachineTarget = instance["IsMachineTarget"].BooleanValue,
                        IsIpuApplication = isIpuApplication,
                        ContentLocation = contentLocation,
                        Revision = instance["Revision"].StringValue,
                        AllowedActions = allowedActions,
                        DeploymentTypeId = deploymentType,
                        Name = instance["Name"].StringValue,
                        Description = instance["Description"].StringValue,
                        Icon = instance["Icon"].StringValue,
                        Deadline = deadline,
                        SoftwareVersion = instance["SoftwareVersion"].StringValue,
                        EstimatedInstallTime = estimatedInstallTime,
                        EvaluationState = instance["EvaluationState"].IntegralValue,
                        InProgressActions = instance["InProgressActions"].StringArrayValue,
                        InstallState = instance["InstallState"].StringValue,
                        InstallTime = instance["LastInstallTime"].DataTimeValue,
                        PercentComplete = instance["PercentComplete"].IntegralValue,
                    });
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            if (evalrequired && IsApplicationEvaluationRequired())
            {
                _log.Information("At least one application in need of evaluation");
                EvaluateAllPolicies();
            }
        }

        private static bool IsContentDownloaded(string id, string revision, out string location)
        {
            var isContentComplete = false;
            location = string.Empty;

            try
            {
                if (Environment.UserInteractive)
                {
                    var status = SqlCe.GetContentStatus(id, revision);

                    if (status == null)
                    {
                        return false;
                    }

                    location = status.Location;
                    return status.IsDownloaded;
                }

                var arg = new Dictionary<string, object>
                {
                    { "AppDeliveryTypeId", id },
                    { "Revision", revision },
                    { "ActionType", "Install" },
                };

                var contentInfo = ExecuteMethod(_namespace_CIModels, "CCM_AppDeliveryType", "GetContentInfo", arg);

                var contentId = contentInfo["ContentId"].StringValue;

                var result = ExecuteQuery(_namespace_SoftMgmtAgent, $"SELECT * FROM CacheInfoEx WHERE ContentId='{contentId}'");

                foreach (var obj in result)
                {
                    isContentComplete = obj["ContentComplete"].BooleanValue;
                    location = obj["Location"].StringValue;
                    break;
                }

                var ipuSettings = SettingsUtils.Settings.IpuApplication;

                if (isContentComplete && ipuSettings.UseWimDrivers)
                {
                    var cmpModel = Cimv2.GetComputerMakeModel();

                    var exclModels = ipuSettings.ExcludedModels.Split(';');

                    if (!exclModels.Contains(cmpModel.Model))
                    {
                        var windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                        var ccmcache = Path.Combine(windowsPath, "ccmcache");

                        if (!Directory.GetFiles(ccmcache, "IPUDrivers*.wim", SearchOption.AllDirectories).Any())
                        {
                            isContentComplete = false;
                            _log.Information("IPU: Content is downloaded but drivers are still missing.");
                        }
                    }
                }

                SqlCe.SetContentStatus(id, revision, isContentComplete, location);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return isContentComplete;
        }

        private static DateTime GetDeadline(string appName, string scopeId, string revision, DateTime deadline)
        {
            if (SqlCe.GetDeadline(scopeId, revision, out var dt))
            {
                if (dt != deadline && deadline != DateTime.MinValue)
                {
                    SqlCe.UpdateDeadline(scopeId, revision, deadline);
                    return deadline;
                }
                else if (dt != deadline)
                {
                    _log.Information($"Using stored 'Deadline' [{dt}] for application '{appName}'");
                    return dt;
                }
            }
            else if (deadline != DateTime.MinValue)
            {
                SqlCe.SaveDeadline(scopeId, revision, deadline);
            }

            return deadline;
        }

        public static CMApplication GetSpecificApp(ScheduledObject scheduledapp, bool includeDT = true)
        {
            try
            {
                var sql = $"SELECT * FROM CCM_Application WHERE Id='{scheduledapp.ObjectId.Trim()}'";

                var result = ExecuteQuery(_namespace_ClientSDK, sql);

                foreach (var instance in result)
                {
                    if (!instance["Revision"].StringValue.Trim().Equals(scheduledapp.Revision.Trim()))
                    {
                        continue;
                    }

                    string[] allowedActions = null;
                    var deploymentTypeId = string.Empty;
                    var deploymentTyperevision = string.Empty;
                    var deploymentReport = string.Empty;
                    var estimatedInstallTime = 0;

                    if (includeDT)
                    {
                        var instancePath = $"{_namespace_ClientSDK}:CCM_Application.Id=\"{instance["Id"].StringValue}\",IsMachineTarget={instance["IsMachineTarget"].StringValue},Revision=\"{instance["Revision"].StringValue}\"";

                        using (var managementObject = new ManagementObject(new ManagementScope(_namespace_ClientSDK), new ManagementPath(instancePath), new ObjectGetOptions()))
                        {
                            managementObject.Get();
                            var resultObject = (IResultObject)new WmiResultObject(managementObject);
                            var dts = resultObject["AppDTs"].ObjectArrayValue;

                            foreach (var dt in dts)
                            {
                                if (!dt["ApplicabilityState"].StringValue.Equals("Applicable"))
                                {
                                    continue;
                                }

                                allowedActions = dt["AllowedActions"].StringArrayValue;
                                deploymentTypeId = dt["Id"].StringValue;
                                deploymentTyperevision = dt["Revision"].StringValue;
                                deploymentReport = dt["DeploymentReport"].StringValue;
                                estimatedInstallTime = dt["EstimatedInstallTime"].IntegralValue;
                                break;
                            }
                        }
                    }

                    var isIpuApplication = false;

                    try
                    {
                        var cats = instance["Categories"].StringArrayValue;
                        isIpuApplication = cats.Contains("IPUApplication");
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex.Message);
                    }

                    return new CMApplication
                    {
                        Id = instance["Id"].StringValue,
                        IsMachineTarget = instance["IsMachineTarget"].BooleanValue,
                        IsIpuApplication = isIpuApplication,
                        Revision = instance["Revision"].StringValue,
                        AllowedActions = allowedActions,
                        DeploymentTypeId = deploymentTypeId,
                        DeploymentTypeRevision = deploymentTyperevision,
                        Name = instance["Name"].StringValue,
                        Description = instance["Description"].StringValue,
                        Icon = instance["Icon"].StringValue,
                        Deadline = instance["Deadline"].DataTimeValue,
                        SoftwareVersion = instance["SoftwareVersion"].StringValue,
                        EstimatedInstallTime = estimatedInstallTime,
                        EvaluationState = instance["EvaluationState"].IntegralValue,
                        InProgressActions = instance["InProgressActions"].StringArrayValue,
                        InstallState = instance["InstallState"].StringValue,
                        InstallTime = instance["LastInstallTime"].DataTimeValue,
                    };
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return null;
        }

        private static void BuildAvailableApps()
        {
            _availableApps.Clear();

            try
            {
                var sql = "SELECT * FROM CCM_Application";

                var result = ExecuteQuery(_namespace_ClientSDK, sql);

                foreach (var instance in result)
                {
                    if (SqlCe.GetAssignment(instance["Id"].StringValue, instance["Revision"].StringValue) != AssignmentPurpose.Available)
                    {
                        continue;
                    }

                    if (!instance["UserUIExperience"].BooleanValue)
                    {
                        continue;
                    }

                    if (!instance["IsMachineTarget"].BooleanValue)
                    {
                        continue;
                    }

                    if (instance["AllowedActions"].StringArrayValue.Count() == 0)
                    {
                        continue;
                    }

                    if (instance["StartTime"].DataTimeValue > DateTime.Now)
                    {
                        continue;
                    }

                    try
                    {
                        var cats = instance["Categories"].StringArrayValue;

                        if (cats.Contains("ExcludeFromScheduler"))
                        {
                            _log.Information($"Excluded '{instance["Name"].StringValue.Trim()}' from Scheduler");
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex.Message);
                    }

                    string[] allowedActions = null;
                    var deploymentType = string.Empty;
                    var estimatedInstallTime = 0;

                    var instancePath = $"{_namespace_ClientSDK}:CCM_Application.Id=\"{instance["Id"].StringValue}\",IsMachineTarget={instance["IsMachineTarget"].StringValue},Revision=\"{instance["Revision"].StringValue}\"";

                    using (var managementObject = new ManagementObject(new ManagementScope(_namespace_ClientSDK), new ManagementPath(instancePath), new ObjectGetOptions()))
                    {
                        managementObject.Get();
                        var resultObject = (IResultObject)new WmiResultObject(managementObject);
                        var dts = resultObject["AppDTs"].ObjectArrayValue;

                        foreach (var dt in dts)
                        {
                            allowedActions = dt["AllowedActions"].StringArrayValue;
                            deploymentType = dt["Id"].StringValue;
                            estimatedInstallTime = dt["EstimatedInstallTime"].IntegralValue;
                            break;
                        }
                    }

                    var isInList = _availableApps.Any(x => x.Id.Equals(instance["Id"].StringValue));

                    if (isInList)
                    {
                        var existing = _availableApps.First(x => x.Id.Equals(instance["Id"].StringValue));

                        if (Convert.ToInt32(existing.Revision) < instance["Revision"].IntegralValue)
                        {
                            _availableApps.Remove(existing);
                        }
                        else
                        {
                            continue;
                        }
                    }

                    _availableApps.Add(new CMApplication
                    {
                        Id = instance["Id"].StringValue,
                        IsMachineTarget = instance["IsMachineTarget"].BooleanValue,
                        Revision = instance["Revision"].StringValue,
                        AllowedActions = allowedActions,
                        DeploymentTypeId = deploymentType,
                        Name = instance["Name"].StringValue,
                        Description = instance["Description"].StringValue,
                        Icon = instance["Icon"].StringValue,
                        Deadline = instance["Deadline"].DataTimeValue,
                        SoftwareVersion = instance["SoftwareVersion"].StringValue,
                        EstimatedInstallTime = estimatedInstallTime,
                        EvaluationState = instance["EvaluationState"].IntegralValue,
                        InProgressActions = instance["InProgressActions"].StringArrayValue,
                        InstallState = instance["InstallState"].StringValue,
                        InstallTime = instance["LastInstallTime"].DataTimeValue,
                    });
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static void InstallAllAppsAndUpdates()
        {
            if (ExecuteInstallUpdates())
            {
                var apps = RequiredApps.Where(x => !x.InstallState.Equals("Installed") && x.EvaluationState == 3 && !x.IsIpuApplication).ToList();

                foreach (var app in apps)
                {
                    SqlCe.SetAppEnforcedFlag(app.Id, app.Revision);
                    InstallApplication(app);
                }
            }
        }

        public static string InstallApplication(CMApplication app)
        {
            try
            {
                var tempApp = GetSpecificApp(new ScheduledObject { ObjectId = app.Id, Revision = app.Revision }, false);
                _installLog.Information($"Attempting install of '{tempApp.Name}'");

                var allScheds = SqlCe.GetPendingSchedules();

                if (allScheds.Where(x => x.ObjectId.Equals(app.Id) && x.Revision.Equals(app.Revision)).Any())
                {
                    var sched = allScheds.Where(x => x.ObjectId.Equals(app.Id) && x.Revision.Equals(app.Revision)).First();
                    SqlCe.SetEnforcedFlag(sched.Id);
                }

                var inParams = new Dictionary<string, object>
                {
                    { "Id", app.Id },
                    { "Revision", app.Revision },
                    { "IsMachineTarget", app.IsMachineTarget },
                    { "EnforcePreference", 0U },
                    { "Priority", "Normal" },
                    { "IsRebootIfNeeded", false },
                };

                var result = ExecuteMethod(_namespace_ClientSDK, "CCM_Application", "Install", inParams);

                if (result["ReturnValue"].IntegralValue == 0)
                {
                    _installLog.Information($"Install attempt of '{tempApp.Name}' result: 'Success'");
                    return result["JobId"].StringValue;
                }
                else
                {
                    _installLog.Warning($"Install attempt of '{tempApp.Name}' result: 'Failure'");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return string.Empty;
        }

        public static string RepairApplication(CMApplication app)
        {
            try
            {
                var tempApp = GetSpecificApp(new ScheduledObject { ObjectId = app.Id, Revision = app.Revision }, false);
                _installLog.Information($"Attempting install of '{tempApp.Name}'");

                var allScheds = SqlCe.GetPendingSchedules();

                if (allScheds.Where(x => x.ObjectId.Equals(app.Id) && x.Revision.Equals(app.Revision)).Any())
                {
                    var sched = allScheds.Where(x => x.ObjectId.Equals(app.Id) && x.Revision.Equals(app.Revision)).First();
                    SqlCe.SetEnforcedFlag(sched.Id);
                }

                var inParams = new Dictionary<string, object>
                {
                    { "Id", app.Id },
                    { "Revision", app.Revision },
                    { "IsMachineTarget", app.IsMachineTarget },
                    { "EnforcePreference", 0U },
                    { "Priority", "Normal" },
                    { "IsRebootIfNeeded", false },
                };

                var result = ExecuteMethod(_namespace_ClientSDK, "CCM_Application", "Repair", inParams);

                if (result["ReturnValue"].IntegralValue == 0)
                {
                    _installLog.Information($"Install attempt of '{tempApp.Name}' result: 'Success'");
                    return result["JobId"].StringValue;
                }
                else
                {
                    _installLog.Warning($"Install attempt of '{tempApp.Name}' result: 'Failure'");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return string.Empty;
        }

        public static string UninstallApplication(CMApplication app)
        {
            try
            {
                var tempApp = GetSpecificApp(new ScheduledObject { ObjectId = app.Id, Revision = app.Revision }, false);
                _installLog.Information($"Attempting uninstall of '{tempApp.Name}'");

                var allScheds = SqlCe.GetPendingSchedules();

                if (allScheds.Where(x => x.ObjectId.Equals(app.Id) && x.Revision.Equals(app.Revision)).Any())
                {
                    var sched = allScheds.Where(x => x.ObjectId.Equals(app.Id) && x.Revision.Equals(app.Revision)).First();
                    SqlCe.SetEnforcedFlag(sched.Id);
                }

                var inParams = new Dictionary<string, object>
                {
                    { "Id", app.Id },
                    { "Revision", app.Revision },
                    { "IsMachineTarget", app.IsMachineTarget },
                    { "EnforcePreference", 0U },
                    { "Priority", "Normal" },
                    { "IsRebootIfNeeded", false },
                };

                var result = ExecuteMethod(_namespace_ClientSDK, "CCM_Application", "Uninstall", inParams);

                if (result["ReturnValue"].IntegralValue == 0)
                {
                    _installLog.Information($"Uninstall attempt of '{tempApp.Name}' result: 'Success'");
                    return result["JobId"].StringValue;
                }
                else
                {
                    _installLog.Warning($"Uninstall attempt of '{tempApp.Name}' result: 'Failure'");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return string.Empty;
        }

        public static bool IsEnforcementJobRunning(string jobId)
        {
            try
            {
                var sql = $"SELECT * FROM CCM_CIEvaluationJob WHERE Id='{jobId}'";

                var result = ExecuteQuery(_namespace_ClientSDK, sql);

                foreach (var instance in result)
                {
                    if (!string.IsNullOrEmpty(instance["JobState"].StringValue))
                    {
                        return !instance["JobState"].StringValue.ToLower().Equals("success");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return true;
        }

        public static InprogressState GetInProgressStatus(string id, string revision)
        {
            try
            {
                var sql = $"SELECT * FROM CCM_Application WHERE Id='{id.Trim()}'";

                var result = ExecuteQuery(_namespace_ClientSDK, sql);

                foreach (var instance in result)
                {
                    if (!instance["Revision"].StringValue.Trim().Equals(revision.Trim()))
                    {
                        continue;
                    }

                    var state = instance["InProgressActions"].StringArrayValue;

                    if (state != null && state.Length > 0)
                    {
                        return state.Contains("Uninstall") ? InprogressState.Uninstalling : InprogressState.Installing;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return InprogressState.Unknown;
        }

        public static bool ExecuteInstallUpdates(bool installAll = false)
        {
            var refinedUpdates = new List<IResultObject>();
            var exclusiveUpdates = new List<IResultObject>();

            try
            {
                var result = ExecuteQuery(_namespace_ClientSDK, "SELECT * FROM CCM_SoftwareUpdate");

                foreach (var update in result)
                {
                    if (update["StartTime"].DataTimeValue <= DateTime.Now && update["EvaluationState"].IntegralValue < 2)
                    {
                        if (update["ExclusiveUpdate"].BooleanValue)
                        {
                            exclusiveUpdates.Add(update);
                            break;
                        }
                        else
                        {
                            refinedUpdates.Add(update);
                        }
                    }
                }

                if (exclusiveUpdates.Any())
                {
                    InstallUpdate(exclusiveUpdates.ToArray());

                    if (installAll && refinedUpdates.Any())
                    {
                        System.Threading.Thread.Sleep(2000);
                        InstallUpdate(refinedUpdates.ToArray());
                    }

                    return false;
                }

                if (refinedUpdates.Any())
                {
                    InstallUpdate(refinedUpdates.ToArray());
                    return false;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return true;
        }

        private static void InstallUpdate(IResultObject[] updates)
        {
            _log.Information($"Attempting to enforce {updates.Count()} updates");

            var listWmiObjects = new List<ManagementBaseObject>();

            try
            {
                foreach (var update in updates)
                {
                    var obj = GetSingleUpdate(update["UpdateID"].StringValue);

                    if (obj != null)
                    {
                        listWmiObjects.Add(obj);
                    }
                }

                var inParams = new Dictionary<string, object>
                {
                    { "CCMUpdates", listWmiObjects.ToArray() },
                };

                ExecuteMethod(_namespace_ClientSDK, "CCM_SoftwareUpdatesManager", "InstallUpdates", inParams);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static bool EvaluateAllPolicies()
        {
            try
            {
                var inParams = new Dictionary<string, object>
                {
                    { "IsEnforceAction", false },
                };

                ExecuteMethod(_namespace_ClientSDK, "CCM_ApplicationPolicy", "EvaluateAllPolicies", inParams);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return false;
            }

            return true;
        }

        public static bool EvaluateApplicationPolicy(string appId, string revision, bool isMachineTarget)
        {
            try
            {
                appId = appId.Replace("Application", "RequiredApplication");

                var inParams = new Dictionary<string, object>
                {
                    { "PolicyId", appId },
                    { "PolicyRevision", revision },
                    { "IsMachineTarget", isMachineTarget },
                    { "Priority", "Normal" },
                    { "IsEnforceAction", false },
                };

                ExecuteMethod(_namespace_ClientSDK, "CCM_ApplicationPolicy", "EvaluateAppPolicy", inParams);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return false;
            }

            return true;
        }

        public static void TriggerClientAction(string schId)
        {
            try
            {
                var inParams = new Dictionary<string, object>
                {
                    { "sScheduleID", schId },
                };

                var outSiteParams = ExecuteMethod(_namespace_CCM, "sms_client", "TriggerSchedule", inParams);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        public static bool IsRebootPending1()
        {
            var ispending = false;

            try
            {
                var outSiteParams = ExecuteMethod(_namespace_ClientSDK, "CCM_ClientUtilities", "DetermineIfRebootPending", new Dictionary<string, object>());

                if (outSiteParams["RebootPending"].BooleanValue)
                {
                    ispending = true;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return ispending;
        }

        public static bool IsCmClientOperational()
        {
            var isOperational = false;

            try
            {
                var outSiteParams = ExecuteMethod(_namespace_CCM, "SMS_Client", "GetAssignedSite", new Dictionary<string, object>());

                if (outSiteParams["sSiteCode"].StringValue.Trim().Length == 3)
                {
                    isOperational = true;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return isOperational;
        }

        public static UIBranding GetBranding()
        {
            new UIBranding();
            var xmlstring = string.Empty;

            try
            {
                var outSiteParams = ExecuteMethod(_namespace_ClientSDK, "CCM_SoftwareCenterUI", "GetUiData", null);

                xmlstring = outSiteParams["XmlUiData"].StringValue;

                if (string.IsNullOrEmpty(xmlstring))
                {
                    return null;
                }

                var doc = new XmlDocument();
                doc.LoadXml(xmlstring);
                var orgnameList = doc.GetElementsByTagName("brand-orgname");
                var logoList = doc.GetElementsByTagName("brand-logo");
                var colorList = doc.GetElementsByTagName("brand-color");

                return new UIBranding
                {
                    Orgname = orgnameList[0].InnerXml,
                    Color = colorList[0].InnerXml,
                    Logo = logoList[0].InnerXml,
                };
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return null;
        }

        public static bool AreMultipleUsersLoggedOn()
        {
            return ExecuteMethod(_namespace_ClientSDK, "CCM_ClientInternalUtilities", "AreMultiUsersLoggedOn", null)["MultiUsersLoggedOn"].StringValue.Equals("True");
        }

        public static RebootInformation IsRebootPending()
        {
            var resultObject = ExecuteMethod(_namespace_ClientSDK, "CCM_ClientUtilities", "DetermineIfRebootPending", null);
            var rebootInformation = new RebootInformation(resultObject["RebootPending"].BooleanValue, resultObject["IsHardRebootPending"].BooleanValue, resultObject["InGracePeriod"].BooleanValue, resultObject["DisableHideTime"].DataTimeValue, resultObject["RebootDeadline"].DataTimeValue);
            var num = resultObject["NotifyUI"].BooleanValue ? 1 : 0;
            rebootInformation.NotifyUI = num != 0;
            return rebootInformation;
        }

        public static bool IsRestartSystemAllowed()
        {
            var methodParameters = new Dictionary<string, object>
            {
                ["Feature"] = (object)ClientFeature.Restart,
            };

            return (uint)(ExecuteMethod(_namespace_ClientSDK, "CCM_ClientUtilities", "GetUserCapability", methodParameters)["Value"].IntegralValue & 268435456) > 0U;
        }

        public static bool IsInstallationRestricted()
        {
            var methodParameters = new Dictionary<string, object>
            {
                ["Feature"] = (object)ClientFeature.Application,
            };

            return (ExecuteMethod(_namespace_ClientSDK, "CCM_ClientUtilities", "GetUserCapability", methodParameters)["Value"].IntegralValue & 4) == 0;
        }

        private static ManagementBaseObject GetSingleUpdate(string updateId)
        {
            ManagementObjectSearcher managementObjectSearcher = null;
            ManagementBaseObject singleUpdate = null;

            var wql = $"SELECT * FROM CCM_SoftwareUpdate WHERE UpdateID = '{updateId}'";

            try
            {
                managementObjectSearcher = new ManagementObjectSearcher(wql)
                {
                    Scope = new ManagementScope(_namespace_ClientSDK),
                };

                foreach (var managementObject in managementObjectSearcher.Get())
                {
                    singleUpdate = managementObject;
                    break;
                }
            }
            catch (ManagementException ex)
            {
                _log.Error(ex.Message);
            }
            catch (COMException ex)
            {
                _log.Error(ex.Message);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            finally
            {
                if (managementObjectSearcher != null)
                {
                    managementObjectSearcher.Dispose();
                }
            }

            return singleUpdate;
        }

        private static IResultObject ExecuteMethod(string nameSpace, string methodClass, string methodName, Dictionary<string, object> methodParameters)
        {
            ManagementClass managementClass = null;
            ManagementBaseObject inParameters = null;
            ManagementBaseObject outParameters = null;

            try
            {
                managementClass = new ManagementClass(methodClass)
                {
                    Scope = new ManagementScope(nameSpace),
                };

                var options = new InvokeMethodOptions(null, ManagementOptions.InfiniteTimeout);
                inParameters = WmiUtilityClass.GetWmiMethodParameter(managementClass, methodName, methodParameters);
                outParameters = managementClass.InvokeMethod(methodName, inParameters, options);
            }
            catch (ManagementException ex)
            {
                _log.Error(ex.Message);
            }
            catch (COMException ex)
            {
                _log.Error(ex.Message);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            finally
            {
                if (managementClass != null)
                {
                    managementClass.Dispose();
                }

                if (inParameters != null)
                {
                    inParameters.Dispose();
                }
            }

            return new WmiResultObject(outParameters);
        }

        private static ICollection<IResultObject> ExecuteQuery(string nameSpace, string query)
        {
            ManagementObjectSearcher managementObjectSearcher = null;
            var resultObjectList = new List<IResultObject>();

            try
            {
                managementObjectSearcher = new ManagementObjectSearcher(query)
                {
                    Scope = new ManagementScope(nameSpace),
                };

                foreach (var managementObject in managementObjectSearcher.Get())
                {
                    resultObjectList.Add(new WmiResultObject(managementObject));
                }
            }
            catch (ManagementException ex)
            {
                _log.Error(ex.Message);
            }
            catch (COMException ex)
            {
                _log.Error(ex.Message);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            finally
            {
                if (managementObjectSearcher != null)
                {
                    managementObjectSearcher.Dispose();
                }
            }

            return resultObjectList;
        }
    }
}
