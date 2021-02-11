using System;
using SchedulerSettings.Models;

namespace SchedulerSettings
{
    [Serializable]
    public class Settings
    {
        public bool IsDefault { get; set; } = true;

        public ActiveTabs ActiveTabs { get; set; } = new ActiveTabs();

        public TabTitles TabTitles { get; set; } = new TabTitles();

        public ServiceConfig ServiceConfig { get; set; } = new ServiceConfig();

        public AvailableAppsSettings AvailableAppsSettings { get; set; } = new AvailableAppsSettings();

        public RequiredAppsSettings RequiredAppsSettings { get; set; } = new RequiredAppsSettings();

        public UpdatesSettings UpdatesSettings { get; set; } = new UpdatesSettings();

        public RestartSettings RestartSettings { get; set; } = new RestartSettings();

        public RestartConfig RestartConfig { get; set; } = new RestartConfig();

        public RestartChecks RestartChecks { get; set; } = new RestartChecks();

        public ToastNotifyRestartSettings ToastNotifyRestartSettings { get; set; } = new ToastNotifyRestartSettings();

        public ToastNotifyNewApplicationSettings ToastNotifyNewApplicationSettings { get; set; } = new ToastNotifyNewApplicationSettings();

        public ToastNotifyNewIpuApplicationSettings ToastNotifyNewIpuApplicationSettings { get; set; } = new ToastNotifyNewIpuApplicationSettings();

        public ToastNotifyNewSupSettings ToastNotifyNewSupSettings { get; set; } = new ToastNotifyNewSupSettings();

        public ToastNotifyAppInstallationStartSettings ToastNotifyAppInstallationStartSettings { get; set; } = new ToastNotifyAppInstallationStartSettings();

        public ToastNotifySupInstallationStartSettings ToastNotifySupInstallationStartSettings { get; set; } = new ToastNotifySupInstallationStartSettings();

        public ToastNotifyServiceRestart ToastNotifyServiceRestart { get; set; } = new ToastNotifyServiceRestart();

        public ToastNotifyServiceInit ToastNotifyServiceInit { get; set; } = new ToastNotifyServiceInit();

        public ToastNotifyServiceRunning ToastNotifyServiceRunning { get; set; } = new ToastNotifyServiceRunning();

        public ToastNotifyServiceEnd ToastNotifyServiceEnd { get; set; } = new ToastNotifyServiceEnd();

        public FeedbackConfig FeedbackConfig { get; set; } = new FeedbackConfig();

        public PlannerSettings PlannerSettings { get; set; } = new PlannerSettings();

        public CountdownWindowSettings CountdownWindowSettings { get; set; } = new CountdownWindowSettings();

        public ConfirmWindowSettings ConfirmWindowSettings { get; set; } = new ConfirmWindowSettings();

        public InstallAllWarningDialogSettings InstallAllWarningDialogSettings { get; set; } = new InstallAllWarningDialogSettings();

        public LegalNotice LegalNotice { get; set; } = new LegalNotice();

        public IpuApplication IpuApplication { get; set; } = new IpuApplication();
    }
}
