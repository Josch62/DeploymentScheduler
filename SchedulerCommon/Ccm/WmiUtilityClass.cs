using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerCommon.Ccm
{
    public static class WmiUtilityClass
    {
        public static ManagementBaseObject GetWmiMethodParameter(ManagementObject methodObject, string methodName, Dictionary<string, object> methodParameters)
        {
            ManagementBaseObject managementBaseObject = null;

            if (methodParameters != null && methodParameters.Count > 0)
            {
                managementBaseObject = methodObject.GetMethodParameters(methodName);

                foreach (var methodParameter in methodParameters)
                {
                    managementBaseObject[methodParameter.Key] = methodParameter.Value;
                }
            }

            return managementBaseObject;
        }
    }
}
