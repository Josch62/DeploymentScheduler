using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerCommon.Ccm
{
    public class CMApplication
    {
        public string Id { get; set; }

        public bool IsMachineTarget { get; set; }

        public bool IsIpuApplication { get; set; }

        public string ContentLocation { get; set; }

        public string Revision { get; set; }

        public string DeploymentTypeId { get; set; }

        public string DeploymentTypeRevision { get; set; }

        public string[] AllowedActions { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Icon { get; set; }

        public DateTime Deadline { get; set; }

        public string SoftwareVersion { get; set; }

        public string InstallState { get; set; }

        public DateTime InstallTime { get; set; }

        public string[] InProgressActions { get; set; }

        public int EstimatedInstallTime { get; set; }

        public int PercentComplete { get; set; }

        public int? EvaluationState { get; set; }

        public string EvaluationStateText
        {
            get
            {
                var evaluationState = EvaluationState;
                if (evaluationState.HasValue)
                {
                    switch (evaluationState.GetValueOrDefault())
                    {
                        case 0:
                            return "No state information is available.";
                        case 1:
                            return "Application is enforced to desired/resolved state.";
                        case 2:
                            return "Application is not required on the client.";
                        case 3:
                            return "Application is available for enforcement (install or uninstall based on resolved state). Content may/may not have been downloaded.";
                        case 4:
                            return "Application last failed to enforce (install/uninstall).";
                        case 5:
                            return "Application is currently waiting for content download to complete.";
                        case 6:
                            return "Application is currently waiting for content download to complete.";
                        case 7:
                            return "Application is currently waiting for its dependencies to download.";
                        case 8:
                            return "Application is currently waiting for a service (maintenance) window.";
                        case 9:
                            return "Application is currently waiting for a previously pending reboot.";
                        case 10:
                            return "Application is currently waiting for serialized enforcement.";
                        case 11:
                            return "Application is currently enforcing dependencies.";
                        case 12:
                            return "Application is currently enforcing.";
                        case 13:
                            return "Application install/uninstall enforced and soft reboot is pending.";
                        case 14:
                            return "Application installed/uninstalled and hard reboot is pending.";
                        case 15:
                            return "Update is available but pending installation.";
                        case 16:
                            return "Application failed to evaluate.";
                        case 17:
                            return "Application is currently waiting for an active user session to enforce.";
                        case 18:
                            return "Application is currently waiting for all users to logoff.";
                        case 19:
                            return "Application is currently waiting for a user logon.";
                        case 20:
                            return "Application in progress, waiting for retry.";
                        case 21:
                            return "Application is waiting for presentation mode to be switched off.";
                        case 22:
                            return "Application is pre-downloading content (downloading outside of install job).";
                        case 23:
                            return "Application is pre-downloading dependent content (downloading outside of install job).";
                        case 24:
                            return "Application download failed (downloading during install job).";
                        case 25:
                            return "Application pre-downloading failed (downloading outside of install job).";
                        case 26:
                            return "Download success (downloading during install job).";
                        case 27:
                            return "Post-enforce evaluation.";
                        case 28:
                            return "Waiting for network connectivity.";
                    }
                }

                return "Unknown state information.";
            }
        }
    }
}
