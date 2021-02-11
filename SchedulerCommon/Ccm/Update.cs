using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerCommon.Ccm
{
    public class Update
    {
        public string UpdateId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime Deadline { get; set; }

        public bool HasBeenEnforced { get; set; }

        public bool IsO365Update { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int EvaluationState { get; set; }

        public int PercentComplete { get; set; }

        public bool ExclusiveUpdate { get; set; }

        public string EvaluationStateText
        {
            get
            {
                switch (EvaluationState)
                {
                    case 0:
                        return "None";
                    case 1:
                        return "Available";
                    case 2:
                        return "Submitted";
                    case 3:
                        return "Detecting";
                    case 4:
                        return "PreDownload";
                    case 5:
                        return "Downloading";
                    case 6:
                        return "WaitInstall";
                    case 7:
                        return "Installing";
                    case 8:
                        return "PendingSoftReboot";
                    case 9:
                        return "PendingHardReboot";
                    case 10:
                        return "WaitReboot";
                    case 11:
                        return "Verifying";
                    case 12:
                        return "InstallComplete";
                    case 13:
                        return "Error";
                    case 14:
                        return "WaitServiceWindow";
                    case 15:
                        return "WaitUserLogon";
                    case 16:
                        return "WaitUserLogoff";
                    case 17:
                        return "WaitJobUserLogon";
                    case 18:
                        return "WaitUserReconnect";
                    case 19:
                        return "PendingUserLogoff";
                    case 20:
                        return "PendingUpdate";
                    case 21:
                        return "WaitingRetry";
                    case 22:
                        return "WaitPresModeOff";
                    case 23:
                        return "WaitForOrchestration";

                    default:
                        return string.Empty;
                }
            }
        }
    }
}
