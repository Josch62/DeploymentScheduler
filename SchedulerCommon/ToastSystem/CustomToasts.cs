using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.QueryStringDotNET;
using Microsoft.Toolkit.Uwp.Notifications;
using SchedulerCommon.Ccm;
using SchedulerCommon.Common;
using SchedulerCommon.Sql;
using SchedulerSettings;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace SchedulerCommon.ToastSystem
{
    public static class CustomToasts
    {
        private static readonly Settings _settings = SettingsUtils.Settings;

        public static void NotifyRestart()
        {
            // Construct the visuals of the toast
            var toastContent = new ToastContent()
            {
                // Arguments when the user taps body of toast
                Launch = new QueryString()
                {
                    { "action", "ok" },
                    { "appidrevision", $"reboot" },
                }.ToString(),

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = _settings.ToastNotifyRestartSettings.Title,
                            },

                            new AdaptiveText()
                            {
                                Text = _settings.ToastNotifyRestartSettings.SubText,
                            },
                        },
                    },
                },

                Actions = new ToastActionsCustom()
                {
                    Buttons =
                    {
                        // Note that there's no reason to specify background activation, since our COM
                        // activator decides whether to process in background or launch foreground window
                        /*
                        new ToastButton("Restart now", new QueryString()
                        {
                            { "action", "restart" },
                            { "appidrevision", $"reboot" }
                        }.ToString()),
                        */

                        new ToastButton(_settings.ToastNotifyRestartSettings.BtSchedule, new QueryString()
                        {
                            { "action", "schedulerestart" },
                            { "appidrevision", $"reboot" },
                        }.ToString()),

                        new ToastButton(_settings.ToastNotifyRestartSettings.BtPostpone, new QueryString()
                        {
                            { "action", "remind" },
                            { "appidrevision", $"reboot" },
                        }.ToString()),
                    },
                },

                Scenario = (ToastScenario)_settings.ToastNotifyRestartSettings.ToastScenario,
            };

            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());

            // And create the toast notification
            var toast = new ToastNotification(doc) { ExpirationTime = DateTimeOffset.Now.AddSeconds(_settings.ToastNotifyRestartSettings.Duration) };
            // And then show it
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }

        public static void NotifyNewApplication()
        {
            // Construct the visuals of the toast
            var toastContent = new ToastContent()
            {
                // Arguments when the user taps body of toast
                Launch = new QueryString()
                {
                    { "action", "ok" },
                    { "appidrevision", $"newapp" },
                }.ToString(),

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = _settings.ToastNotifyNewApplicationSettings.Title,
                            },

                            new AdaptiveText()
                            {
                                Text = _settings.ToastNotifyNewApplicationSettings.SubText,
                            },
                        },
                    },
                },

                Actions = new ToastActionsCustom()
                {
                    Buttons =
                    {
                        // Note that there's no reason to specify background activation, since our COM
                        // activator decides whether to process in background or launch foreground window
                        new ToastButton(_settings.ToastNotifyNewApplicationSettings.BtSchedule, new QueryString()
                        {
                            { "action", "schedule" },
                            { "appidrevision", $"newapp" },
                        }.ToString()),

                        new ToastButton(_settings.ToastNotifyNewApplicationSettings.BtPostpone, new QueryString()
                        {
                            { "action", "postpone" },
                            { "appidrevision", $"newapp" },
                        }.ToString()),
                    },
                },

                Scenario = (ToastScenario)_settings.ToastNotifyNewApplicationSettings.ToastScenario,
            };

            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());

            // And create the toast notification
            var toast = new ToastNotification(doc) { ExpirationTime = DateTimeOffset.Now.AddSeconds(_settings.ToastNotifyNewApplicationSettings.Duration) };
            // And then show it
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }

        public static void NotifyNewIpuApplication()
        {
            // Construct the visuals of the toast
            var toastContent = new ToastContent()
            {
                // Arguments when the user taps body of toast
                Launch = new QueryString()
                {
                    { "action", "ok" },
                    { "appidrevision", $"newapp" },
                }.ToString(),

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = _settings.ToastNotifyNewIpuApplicationSettings.Title,
                            },

                            new AdaptiveText()
                            {
                                Text = _settings.ToastNotifyNewIpuApplicationSettings.SubText,
                            },
                        },
                    },
                },

                Actions = new ToastActionsCustom()
                {
                    Buttons =
                    {
                        // Note that there's no reason to specify background activation, since our COM
                        // activator decides whether to process in background or launch foreground window
                        new ToastButton(_settings.ToastNotifyNewIpuApplicationSettings.BtSchedule, new QueryString()
                        {
                            { "action", "scheduleipu" },
                            { "appidrevision", $"newapp" },
                        }.ToString()),

                        new ToastButton(_settings.ToastNotifyNewIpuApplicationSettings.BtPostpone, new QueryString()
                        {
                            { "action", "postpone" },
                            { "appidrevision", $"newapp" },
                        }.ToString()),
                    },
                },

                Scenario = (ToastScenario)_settings.ToastNotifyNewIpuApplicationSettings.ToastScenario,
            };

            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());

            // And create the toast notification
            var toast = new ToastNotification(doc) { ExpirationTime = DateTimeOffset.Now.AddSeconds(_settings.ToastNotifyNewIpuApplicationSettings.Duration) };
            // And then show it
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }

        public static void NotifyNewSup()
        {
            // Construct the visuals of the toast
            var toastContent = new ToastContent()
            {
                // Arguments when the user taps body of toast
                Launch = new QueryString()
                {
                    { "action", "ok" },
                    { "appidrevision", $"sup" },
                }.ToString(),

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = _settings.ToastNotifyNewSupSettings.Title,
                            },

                            new AdaptiveText()
                            {
                                Text = _settings.ToastNotifyNewSupSettings.SubText,
                            },
                        },
                    },
                },

                Actions = new ToastActionsCustom()
                {
                    Buttons =
                    {
                        // Note that there's no reason to specify background activation, since our COM
                        // activator decides whether to process in background or launch foreground window
                        /*
                        new ToastButton(_settings.ToastNotifyNewSupSettings.BtInstall, new QueryString()
                        {
                            { "action", "install" },
                            { "appidrevision", $"sup" }

                        }.ToString()),
                        */

                        new ToastButton(_settings.ToastNotifyNewSupSettings.BtSchedule, new QueryString()
                        {
                            { "action", "schedule" },
                            { "appidrevision", $"sup" },
                        }.ToString()),

                        new ToastButton(_settings.ToastNotifyNewSupSettings.BtPostpone, new QueryString()
                        {
                            { "action", "postpone" },
                            { "appidrevision", $"sup" },
                        }.ToString()),
                    },
                },

                Scenario = (ToastScenario)_settings.ToastNotifyNewSupSettings.ToastScenario,
            };

            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());

            // And create the toast notification
            var toast = new ToastNotification(doc) { ExpirationTime = DateTimeOffset.Now.AddSeconds(_settings.ToastNotifyNewSupSettings.Duration) };
            // And then show it
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }

        public static void NotifyServiceRestart()
        {
            // Construct the visuals of the toast
            var toastContent = new ToastContent()
            {
                // Arguments when the user taps body of toast
                Launch = new QueryString()
                {
                    { "action", "ok" },
                    { "appidrevision", $"close" },
                }.ToString(),

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = _settings.ToastNotifyServiceRestart.Title,
                            },

                            new AdaptiveText()
                            {
                                Text = _settings.ToastNotifyServiceRestart.SubText.Replace("%COUNTDOWN%", _settings.RestartConfig.CountDown),
                            },
                        },
                    },
                },

                Scenario = (ToastScenario)_settings.ToastNotifyServiceRestart.ToastScenario,
            };

            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());

            // And create the toast notification
            var toast = new ToastNotification(doc) { ExpirationTime = DateTimeOffset.Now.AddSeconds(_settings.ToastNotifyServiceRestart.Duration) };
            // And then show it
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }

        public static void NotifyServiceInit()
        {
            // Construct the visuals of the toast
            var toastContent = new ToastContent()
            {
                // Arguments when the user taps body of toast
                Launch = new QueryString()
                {
                    { "action", "ok" },
                    { "appidrevision", $"close" },
                }.ToString(),

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = _settings.ToastNotifyServiceInit.Title,
                            },

                            new AdaptiveText()
                            {
                                Text = _settings.ToastNotifyServiceInit.SubText,
                            },
                        },
                    },
                },

                Scenario = (ToastScenario)_settings.ToastNotifyServiceInit.ToastScenario,
            };

            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());

            // And create the toast notification
            var toast = new ToastNotification(doc) { ExpirationTime = DateTimeOffset.Now.AddSeconds(_settings.ToastNotifyServiceInit.Duration) };
            // And then show it
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }

        public static void NotifyServiceRunning()
        {
            // Construct the visuals of the toast
            var toastContent = new ToastContent()
            {
                // Arguments when the user taps body of toast
                Launch = new QueryString()
                {
                    { "action", "ok" },
                    { "appidrevision", $"close" },
                }.ToString(),

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = _settings.ToastNotifyServiceRunning.Title,
                            },

                            new AdaptiveText()
                            {
                                Text = _settings.ToastNotifyServiceRunning.SubText,
                            },
                        },
                    },
                },

                Scenario = (ToastScenario)_settings.ToastNotifyServiceRunning.ToastScenario,
            };

            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());

            // And create the toast notification
            var toast = new ToastNotification(doc) { ExpirationTime = DateTimeOffset.Now.AddSeconds(_settings.ToastNotifyServiceRunning.Duration) };
            // And then show it
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }

        public static void NotifyServiceEnd()
        {
            // Construct the visuals of the toast
            var toastContent = new ToastContent()
            {
                // Arguments when the user taps body of toast
                Launch = new QueryString()
                {
                    { "action", "ok" },
                    { "appidrevision", $"close" },
                }.ToString(),

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = _settings.ToastNotifyServiceEnd.Title,
                            },

                            new AdaptiveText()
                            {
                                Text = _settings.ToastNotifyServiceEnd.SubText,
                            },
                        },
                    },
                },

                Scenario = (ToastScenario)_settings.ToastNotifyServiceEnd.ToastScenario,
            };

            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());

            // And create the toast notification
            var toast = new ToastNotification(doc) { ExpirationTime = DateTimeOffset.Now.AddSeconds(_settings.ToastNotifyServiceEnd.Duration) };
            // And then show it
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }

        public static void NotifyAppInstallationStart(CMApplication app)
        {
            // Construct the visuals of the toast
            var toastContent = new ToastContent()
            {
                // Arguments when the user taps body of toast
                Launch = new QueryString()
                {
                    { "action", "ok" },
                    { "appidrevision", $"close" },
                }.ToString(),

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = _settings.ToastNotifyAppInstallationStartSettings.Title,
                            },

                            new AdaptiveText()
                            {
                                Text = app.Name,
                            },
                        },
                    },
                },

                Scenario = (ToastScenario)_settings.ToastNotifyAppInstallationStartSettings.ToastScenario,
            };

            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());

            // And create the toast notification
            var toast = new ToastNotification(doc) { ExpirationTime = DateTimeOffset.Now.AddSeconds(_settings.ToastNotifyAppInstallationStartSettings.Duration) };
            // And then show it
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }

        public static void NotifySupInstallationStart()
        {
            // Construct the visuals of the toast
            var toastContent = new ToastContent()
            {
                // Arguments when the user taps body of toast
                Launch = new QueryString()
                {
                    { "action", "ok" },
                    { "appidrevision", $"close" },
                }.ToString(),

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = _settings.ToastNotifySupInstallationStartSettings.Title,
                            },

                            new AdaptiveText()
                            {
                                Text = _settings.ToastNotifySupInstallationStartSettings.SubText,
                            },
                        },
                    },
                },

                Scenario = (ToastScenario)_settings.ToastNotifySupInstallationStartSettings.ToastScenario,
            };

            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());

            // And create the toast notification
            var toast = new ToastNotification(doc) { ExpirationTime = DateTimeOffset.Now.AddSeconds(_settings.ToastNotifySupInstallationStartSettings.Duration) };

            // And then show it
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }

        public static void NotifyComingInstallation()
        {
            var enforcemets = SqlCe.GetPendingSchedules();
            var validToToast = enforcemets.Where(x => x.EnforcementTime < DateTime.Today.AddDays(2)).First();

            // Construct the visuals of the toast
            var toastContent = new ToastContent()
            {
                // Arguments when the user taps body of toast
                Launch = new QueryString()
                {
                    { "action", "ok" },
                    { "appidrevision", $"close" },
                }.ToString(),

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = "Reminder",
                            },

                            new AdaptiveText()
                            {
                                Text = $"You have an upcomming scheduled installation at '{validToToast.EnforcementTime}'",
                            },
                        },
                    },
                },
            };

            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());

            // And create the toast notification
            var toast = new ToastNotification(doc) { ExpirationTime = DateTimeOffset.Now.AddSeconds(15) };
            // And then show it
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }
    }
}
