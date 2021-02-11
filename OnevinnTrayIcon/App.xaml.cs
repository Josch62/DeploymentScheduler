using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using SchedulerCommon.Pipes;
using SchedulerCommon.Sql;

namespace OnevinnTrayIcon
{
    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    public partial class App : Application
    {
        private PipeServer _pipeServer;
        private static Mutex _mutex;

        public App()
        {
            _mutex = new Mutex(true, "83C71D2D-99B9-425A-9C66-AC0FDD670973", out var isnew);

            if (!isnew)
            {
                Environment.Exit(0);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
            Globals.NotifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            Globals.NotifyIcon.TrayPopup = new TextBlock
            {
                Text = "No status available",
                Foreground = Brushes.LightSalmon,
                Background = Brushes.Black,
                FontSize = 14,
            };

            if (SqlCe.GetAutoEnforceFlag())
            {
                Dispatcher.Invoke(() =>
                {
                    Globals.NotifyIcon.IconSource = new BitmapImage(new Uri("pack://application:,,,/Icons/red.ico"));
                });
            }

            _pipeServer = new PipeServer();
            _pipeServer.Listen("01DB94E3-90F1-43F4-8DDA-8AEAF6C08A8E");
            _pipeServer.PipeMessage += PipeServer_PipeMessage;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Globals.NotifyIcon.Dispose(); //the icon would clean up automatically, but this is cleaner
            base.OnExit(e);
        }

        private void PipeServer_PipeMessage(object sender, PipeEventArg e)
        {
            switch (e.Message.TrimEnd('\0').Trim())
            {
                case "SetBlue":
                    Dispatcher.Invoke(() =>
                    {
                        Globals.NotifyIcon.IconSource = new BitmapImage(new Uri("pack://application:,,,/Icons/product.ico"));
                    });
                    break;

                case "SetRed":
                    Dispatcher.Invoke(() =>
                    {
                        Globals.NotifyIcon.IconSource = new BitmapImage(new Uri("pack://application:,,,/Icons/red.ico"));
                    });
                    break;

                case "Close":
                    Environment.Exit(0);
                    break;
            }
        }
    }
}
