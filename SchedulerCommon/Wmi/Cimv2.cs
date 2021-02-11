using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using SchedulerCommon.Common;
using SchedulerSettings;

namespace SchedulerCommon.Wmi
{
    public static class Cimv2
    {
        public static ComputerMakeModel GetComputerMakeModel()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_ComputerSystemProduct");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    var tmpVendor = queryObj["Vendor"].ToString().Trim();
                    var useVersion = SettingsUtils.Settings.IpuApplication.UseVersionForLenovo && tmpVendor.ToUpper().Equals("LENOVO");

                    return new ComputerMakeModel
                    {
                        Manufacturer = queryObj["Vendor"].ToString().Trim(),
                        Model = useVersion ? queryObj["Version"].ToString().Trim() : queryObj["Name"].ToString().Trim(),
                    };
                }
            }
            catch { }

            return null;
        }
    }
}
