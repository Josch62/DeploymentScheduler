using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerCommon.Ccm
{
    public static class ClientAction
    {
        public static string MachinePolicyRequest { get; } = "{00000000-0000-0000-0000-000000000021}";

        public static string MachinePolicyEvaluation { get; } = "{00000000-0000-0000-0000-000000000022}";

        public static string UserPolicyRequest { get; } = "{00000000-0000-0000-0000-000000000026}";

        public static string UserPolicyEvaluation { get; } = "{00000000-0000-0000-0000-000000000027}";

        public static string HardwareInventoryCycle { get; } = "{00000000-0000-0000-0000-000000000001}";

        public static string AppDeploymentEvaluation { get; } = "{00000000-0000-0000-0000-000000000121}";

        public static string GlobalAppDeploymentEvaluation { get; } = "{00000000-0000-0000-0000-000000000123}";
    }
}
