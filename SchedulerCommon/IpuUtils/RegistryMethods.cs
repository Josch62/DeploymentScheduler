using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using SchedulerSettings;

namespace SchedulerCommon.IpuUtils
{
    public static class RegistryMethods
    {
        public static void SetProgressStatus(int status)
        {
            if (!SettingsUtils.Settings.IpuApplication.ShowProgress)
            {
                return;
            }

            try
            {
                var reg = Registry.LocalMachine;
                var pKey = reg.CreateSubKey("SOFTWARE\\Onevinn\\DeploymentScheduler");
                pKey.SetValue("IPUPhase", status.ToString(), RegistryValueKind.String);
            }
            catch { }
        }

        public static void SetIpuIsRunning()
        {
            try
            {
                var reg = Registry.LocalMachine;
                var pKey = reg.CreateSubKey("SOFTWARE\\Onevinn\\DeploymentScheduler");
                pKey.SetValue("IpuIsRunning", "True", RegistryValueKind.String);
                pKey.SetValue("UseWimDrivers", SettingsUtils.Settings.IpuApplication.UseWimDrivers ? "True" : "False", RegistryValueKind.String);
                pKey.SetValue("ShowProgress", SettingsUtils.Settings.IpuApplication.ShowProgress ? "True" : "False", RegistryValueKind.String);
                pKey.SetValue("CustomDriversFolder", SettingsUtils.Settings.IpuApplication.CustomDriversFolder ?? string.Empty, RegistryValueKind.String);
                pKey.SetValue("ExcludedModels", SettingsUtils.Settings.IpuApplication.ExcludedModels ?? string.Empty, RegistryValueKind.String);
                pKey.SetValue("UseVersionForLenovo", SettingsUtils.Settings.IpuApplication.UseVersionForLenovo ? "True" : "False", RegistryValueKind.String);

            }
            catch { }
        }

        public static void RemoveIpuIsRunning()
        {
            try
            {
                var reg = Registry.LocalMachine;
                var pKey = reg.OpenSubKey("SOFTWARE\\Onevinn\\DeploymentScheduler", true);
                pKey.DeleteValue("IpuIsRunning");
                pKey.DeleteValue("UseWimDrivers");
                pKey.DeleteValue("ShowProgress");
                pKey.DeleteValue("CustomDriversFolder");
                pKey.DeleteValue("ExcludedModels");
                pKey.DeleteValue("UseVersionForLenovo");
            }
            catch { }
        }

        public static bool GetIpuIsRunning()
        {
            try
            {
                var reg = Registry.LocalMachine;
                var pKey = reg.OpenSubKey("SOFTWARE\\Onevinn\\DeploymentScheduler");
                var oValue = pKey.GetValue("IpuIsRunning");

                if (oValue != null)
                {
                    return Convert.ToBoolean(oValue.ToString());
                }
            }
            catch { }

            return false;
        }

        public static void DeleteProgressStatus()
        {
            if (!SettingsUtils.Settings.IpuApplication.ShowProgress)
            {
                return;
            }

            try
            {
                var reg = Registry.LocalMachine;
                var pKey = reg.OpenSubKey("SOFTWARE\\Onevinn\\DeploymentScheduler", true);
                pKey.DeleteValue("IPUPhase");
            }
            catch (Exception ex)
            {
                Globals.Log.Error($"Method='DeleteProgressStatus' Exception='{ex.Message}'");
            }
        }

        public static void MoveSetupDiag(int resultCode)
        {
            try
            {
                var reg = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

                using (var pKey = reg.OpenSubKey("SYSTEM\\Setup\\setupdiag\\results"))
                {
                    using (var outKey = reg.CreateSubKey("SOFTWARE\\Onevinn\\IpuResult", true))
                    {
                        var names = pKey.GetValueNames();

                        foreach (var name in names)
                        {
                            var val = pKey.GetValue(name).ToString();
                            outKey.SetValue(name, val, RegistryValueKind.String);
                        }

                        outKey.SetValue("ResultCode", "0x" + resultCode.ToString("X8"));
                        var lastStatus = resultCode == 0 ? "PendingReboot" : "Failure";
                        outKey.SetValue("LastStatus", lastStatus, RegistryValueKind.String);
                    }
                }
            }
            catch (Exception ex)
            {
                Globals.Log.Error($"Method='MoveSetupDiag' Exception='{ex.Message}'");
            }
        }
    }
}
