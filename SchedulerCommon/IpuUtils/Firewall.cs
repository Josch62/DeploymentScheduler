using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NetFwTypeLib;

namespace SchedulerCommon.IpuUtils
{
    public static class Firewall
    {
        private static readonly string _systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
        private static readonly string _setupHost = $"{_systemDrive}$WINDOWS.~BT\\Sources\\SetupHost.exe";

        public static void BlockSetupHost()
        {
            try
            {
                var fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
                var rule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));

                rule.Name = "FURule";
                rule.Profiles = (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL;
                rule.Enabled = true;
                rule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                rule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY;
                rule.ApplicationName = _setupHost;

                fwPolicy2.Rules.Add(rule);

                if (Marshal.IsComObject(rule) == true)
                    Marshal.ReleaseComObject(rule);

                if (Marshal.IsComObject(fwPolicy2) == true)
                    Marshal.ReleaseComObject(fwPolicy2);
            }
            catch (Exception ex)
            {
                Globals.Log.Error(ex.Message);
            }
        }

        public static void RemoveBlockSetupHost()
        {
            try
            {
                var fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
                fwPolicy2.Rules.Remove("FURule");

                if (Marshal.IsComObject(fwPolicy2) == true)
                {
                    Marshal.ReleaseComObject(fwPolicy2);
                }
            }
            catch (Exception ex)
            {
                Globals.Log.Error(ex.Message);
            }
        }
    }
}
