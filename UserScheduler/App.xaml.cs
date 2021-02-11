using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using SchedulerCommon.ToastSystem;
using SchedulerSettings;
using UserScheduler.Natives;
using UserScheduler.ToastActivator;
using UserScheduler.Windows;

namespace UserScheduler
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification ="Lazy")]
    public partial class App : Application
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Lazy")]
        private static Mutex _mutex;

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;

            // Register AUMID, COM server, and activator
            DesktopNotificationManagerCompat.RegisterAumidAndComServer<UserSchedulerNotificationActivator>("Onevinn.UserScheduler");
            DesktopNotificationManagerCompat.RegisterActivator<UserSchedulerNotificationActivator>();

            if (Globals.Args.Exist("ToastApp"))
            {
                SendAppToast();
                Globals.Log.Information("Closing after sending application toast.");
                Environment.Exit(0);
            }

            if (Globals.Args.Exist("ToastIpu"))
            {
                SendIpuToast();
                Globals.Log.Information("Closing after sending IPUApplication toast.");
                Environment.Exit(0);
            }

            if (Globals.Args.Exist("ToastSup"))
            {
                SendSupToast();
                Globals.Log.Information("Closing after sending sup toast.");
                Environment.Exit(0);
            }

            if (Globals.Args.Exist("ToastReboot"))
            {
                SendRebootToast();
                Globals.Log.Information("Closing after sending restart toast.");
                Environment.Exit(0);
            }

            if (Globals.Args.Exist("ToastNotifyComingInstallation"))
            {
                SendNotifyComingInstallation();
                Globals.Log.Information("Closing after sending reminder toast.");
                Environment.Exit(0);
            }

            if (Globals.Args.Exist("ToastServiceRestartNotification"))
            {
                SendServiceRestartNotification();
                Globals.Log.Information("Closing after sending service restart toast.");
                Environment.Exit(0);
            }

            if (Globals.Args.Exist("ToastServiceInitNotification"))
            {
                SendServiceInitNotification();
                Globals.Log.Information("Closing after sending service init toast.");
                Environment.Exit(0);
            }

            if (Globals.Args.Exist("ToastServiceRunningNotification"))
            {
                SendServiceRunningNotification();
                Globals.Log.Information("Closing after sending service init toast.");
                Environment.Exit(0);
            }

            if (Globals.Args.Exist("ToastServiceEndNotification"))
            {
                SendServiceEndNotification();
                Globals.Log.Information("Closing after sending service end toast.");
                Environment.Exit(0);
            }

            _mutex = new Mutex(true, "ThereCanOnlyBeOneUserScheduler", out var isnew);
            var otherWindow = Globals.Args.Exist("ShowConfirmWindow") || Globals.Args.Exist("ShowRestartWindow") || Globals.Args.Exist("ShowIpuDialog1") || Globals.Args.Exist("ShowIpuDialog2");

            if (!isnew && !otherWindow)
            {
                NativeMethods.PostMessage(
                    (IntPtr)NativeMethods.HWND_BROADCAST,
                    NativeMethods.WM_SHOWMENOW,
                    IntPtr.Zero,
                    IntPtr.Zero);

                Environment.Exit(0);
            }

            Globals.Settings = SettingsUtils.Settings;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // If launched from a toast
            if (e.Args.Contains(DesktopNotificationManagerCompat.TOAST_ACTIVATED_LAUNCH_ARG))
            {
                // Our NotificationActivator code will run after this completes,
                // and will show a window if necessary.
            }
            else
            {
                // Show the window
                // In App.xaml, be sure to remove the StartupUri so that a window doesn't
                // get created by default, since we're creating windows ourselves (and sometimes we
                // don't want to create a window if handling a background activation).
                if (Globals.Args.Exist("ShowConfirmWindow"))
                {
                    new ConfirmWindow().Show();
                }
                else if (Globals.Args.Exist("ShowRestartWindow"))
                {
                    new RestartWindow().Show();
                }
                else if (Globals.Args.Exist("ShowIpuDialog1"))
                {
                    new IpuDialog().Show();
                }
                else if (Globals.Args.Exist("ShowIpuDialog2"))
                {
                    new IpuDialog(true).Show();
                }
                else
                {
                    new MainWindow().Show();
                }
            }

            base.OnStartup(e);
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Globals.Log.Error("Dispatcher_UnhandledException", e.Exception.Message + "   InnerException: " + e.Exception.InnerException.Message);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            Globals.Log.Error("CurrentDomain_UnhandledException", ex.Message);
        }

        private void SendAppToast()
        {
            CustomToasts.NotifyNewApplication();
        }

        private void SendIpuToast()
        {
            CustomToasts.NotifyNewIpuApplication();
        }

        private void SendSupToast()
        {
            CustomToasts.NotifyNewSup();
        }

        private void SendRebootToast()
        {
            CustomToasts.NotifyRestart();
        }

        private void SendNotifyComingInstallation()
        {
            CustomToasts.NotifyComingInstallation();
        }

        private void SendServiceRestartNotification()
        {
            CustomToasts.NotifyServiceRestart();
        }

        private void SendServiceInitNotification()
        {
            CustomToasts.NotifyServiceInit();
        }

        private void SendServiceRunningNotification()
        {
            CustomToasts.NotifyServiceRunning();
        }

        private void SendServiceEndNotification()
        {
            CustomToasts.NotifyServiceEnd();
        }
    }
}
