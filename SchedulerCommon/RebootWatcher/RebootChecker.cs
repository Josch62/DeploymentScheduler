using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using SchedulerCommon.Ccm;
using SchedulerCommon.Logging;
using SchedulerSettings.Models;

namespace SchedulerCommon.RebootWatcher
{
    public static class RebootChecker
    {
        private static readonly ServiceEventSource _log = ServiceEventSource.Log;
        private static List<string> _pendingFileNameExclusions;

        /// <summary>
        /// Initializes a new instance of the <see cref="RebootChecker"/> class.
        /// </summary>
        public static RebootReason RebootRequired(RestartChecks rebootChecks)
        {
            _pendingFileNameExclusions = rebootChecks.PendingFileNameExclusions;
            var rr = new RebootReason();
            var logtext = new StringBuilder();

            if (rebootChecks.ConfigMgrClient)
            {
                if (CcmUtils.IsRebootPending().RebootPending)
                {
                    rr.ConfigMgrClient = true;
                    logtext.AppendLine("RebootWatcher detected SCCM Client RebootPending.");
                }
            }

            try
            {
                var rebootPendingKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending", false);
                if (rebootPendingKey != null && rebootChecks.ComponentBasedServicing)
                {
                    rr.ComponentBasedServicing = true;
                    logtext.AppendLine("RebootWatcher detected Component Based Servicing RebootPending.");
                }

                if (RegistryKeyExists(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update", "RebootRequired") && rebootChecks.WindowsUpdate)
                {
                    rr.WindowsUpdate = true;
                    logtext.AppendLine("RebootWatcher detected Windows Update RebootPending.");
                }

                if (!IsRegKeyEmpty(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager", "PendingFileRenameOperations") && rebootChecks.PendingFileOperations)
                {
                    rr.PendingFileOperations = true;
                    logtext.AppendLine("RebootWatcher detected Pending File Name operations RebootPending.");
                }
            }
            catch (Exception ex)
            {
                _log.Error("RebootRequired", ex.Message);
            }

            if (!rr.Any)
            {
                logtext.AppendLine("RebootWatcher didn't detect any Pending Reboot operations.");
            }

            _log.Information(logtext.ToString());

            return rr;
        }

        private static bool RegistryKeyExists(RegistryHive hive, string path, string valueName)
        {
            RegistryKey root = null;

            switch (hive)
            {
                case RegistryHive.LocalMachine:
                    root = Registry.LocalMachine.OpenSubKey(path, false);
                    break;
                case RegistryHive.CurrentUser:
                    root = Registry.CurrentUser.OpenSubKey(path, false);
                    break;
            }

            if (root.GetValue(valueName) == null)
                return false;
            else
                return true;
        }

        private static bool IsRegKeyEmpty(RegistryHive hive, string path, string valueName)
        {
            var logDelete = new StringBuilder();
            var logRename = new StringBuilder();

            RegistryKey root = null;
            object regvalue = null;

            switch (hive)
            {
                case RegistryHive.LocalMachine:
                    root = Registry.LocalMachine.OpenSubKey(path, false);
                    break;
                case RegistryHive.CurrentUser:
                    root = Registry.CurrentUser.OpenSubKey(path, false);
                    break;
            }

            if ((regvalue = root.GetValue(valueName)) == null)
            {
                return true;
            }
            else if (string.IsNullOrEmpty(regvalue.ToString()))
            {
                return true;
            }
            else if (root.GetValueKind(valueName) == RegistryValueKind.MultiString)
            {
                try
                {
                    var orgValues = (string[])root.GetValue(valueName);

                    if (orgValues == null)
                    {
                        return true;
                    }

                    var refinedList = new List<string>();

                    if (orgValues.Count() < 2)
                    {
                        return true;
                    }

                    var opsCount = orgValues.Count() / 2;

                    _log.Information($"Found {opsCount} pending file operations.");

                    for (var ind = 0; ind < orgValues.Count(); ind = ind + 2)
                    {
                        if (!string.IsNullOrEmpty(orgValues[ind + 1]))
                        {
                            refinedList.Add($"{orgValues[ind]}{orgValues[ind + 1]}");

                            var renameActionText = IsInWildExclusions($"{orgValues[ind]}{orgValues[ind + 1]}") ? "Excluded from RENAME" : "RENAME";
                            logRename.AppendLine($"{renameActionText}: {orgValues[ind]} -> {orgValues[ind + 1]}");
                        }
                        else if (!string.IsNullOrEmpty(orgValues[ind]))
                        {
                            logDelete.AppendLine($"Excluded from DELETE: {orgValues[ind]}");
                        }
                    }

                    if (logDelete.Length != 0 || logRename.Length != 0)
                    {
                        _log.Information($"Pending file operations Summary:\n{logRename}\n{logDelete}");
                    }
                    else
                    {
                        _log.Information($"Pending file operations Summary: None");
                    }

                    var remaingCount = refinedList.Where(s => !IsInWildExclusions(s)).Distinct().Count();

                    _log.Information($"Pending file operations remaining after filtertering: {remaingCount}");

                    if (remaingCount == 0)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message);
                }
            }

            return false;
        }

        private static bool IsInWildExclusions(string value)
        {
            foreach (var excl in _pendingFileNameExclusions)
            {
                if (string.IsNullOrEmpty(excl.Trim()))
                {
                    continue;
                }

                if (value.Contains(excl))
                {
                    _log.Information($"File operation '{value}' contains exclusion '{excl}'");
                    return true;
                }
                else
                {
                    _log.Information($"File operation '{value}' does NOT contain exclusion '{excl}'");
                }
            }

            return false;
        }
    }
}
