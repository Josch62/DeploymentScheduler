using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using DeploymentScheduler.Helpers;
using FontAwesome.WPF;
using Microsoft.QueryStringDotNET;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using Newtonsoft.Json;
using SchedulerCommon.Ccm;
using SchedulerCommon.Common;
using SchedulerCommon.Sql;
using SchedulerCommon.ToastSystem;
using SchedulerSettings.Models;
using UserScheduler.Common;
using UserScheduler.Natives;
using UserScheduler.UserControls;

namespace UserScheduler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty ScaleValueProperty = DependencyProperty.Register("ScaleValue", typeof(double), typeof(MainWindow), new UIPropertyMetadata(1.0, new PropertyChangedCallback(OnScaleValueChanged), new CoerceValueCallback(OnCoerceScaleValue)));
        private readonly CcmWmiEventListener _ccmWmiEventListener;

        #region ScaleValue Depdencies

        protected virtual double OnCoerceScaleValue(double value)
        {
            if (double.IsNaN(value))
                return 1.0f;

            value = Math.Max(0.1, value);
            return value;
        }

        protected virtual void OnScaleValueChanged(double oldValue, double newValue)
        {
        }

        private static object OnCoerceScaleValue(DependencyObject o, object value)
        {
            if (o is MainWindow mainWindow)
                return mainWindow.OnCoerceScaleValue((double)value);
            else
                return value;
        }

        private static void OnScaleValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is MainWindow mainWindow)
                mainWindow.OnScaleValueChanged((double)e.OldValue, (double)e.NewValue);
        }

        public double ScaleValue
        {
            get
            {
                return (double)GetValue(ScaleValueProperty);
            }

            set
            {
                SetValue(ScaleValueProperty, value);
            }
        }

        private void MainGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CalculateScale();
        }

        private void CalculateScale()
        {
            var yScale = ActualHeight / 650.0d;
            var xScale = ActualWidth / 1000.0d;
            var value = Math.Min(xScale, yScale);
            ScaleValue = (double)OnCoerceScaleValue(MainGrid, value);
        }

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            CheckServicesRunning();
            ApplyBranding();
            Globals.MainWnd = this;
            Globals.Log.Information("The application was started from the start-menu shortcut");
            Globals.CcmWmiEventListener = new CcmWmiEventListener();

            BtApps_MouseLeftButtonDown(BtApps, null);
            BtApps_MouseLeftButtonUp(BtApps, null);

            BuildIpuAppLayout();
            BuildAvailableAppLayout();
            BuildSupLayout();

            _ccmWmiEventListener = new CcmWmiEventListener();
            _ccmWmiEventListener.OnNewApplication += CcmWmiEventListener_OnNewApplication;
            _ccmWmiEventListener.OnNewUpdate += CcmWmiEventListener_OnNewUpdate;
        }

        public MainWindow(bool toastStart)
        {
            InitializeComponent();
            CheckServicesRunning();
            ApplyBranding();
            Globals.MainWnd = this;
            Globals.Log.Information("The application was started from a toast");
            Globals.CcmWmiEventListener = new CcmWmiEventListener();
            _ccmWmiEventListener = new CcmWmiEventListener();
            _ccmWmiEventListener.OnNewApplication += CcmWmiEventListener_OnNewApplication;
            _ccmWmiEventListener.OnNewUpdate += CcmWmiEventListener_OnNewUpdate;
        }

        private void CheckServicesRunning()
        {
            if (Process.GetProcessesByName("ccmexec").Any() && Process.GetProcessesByName("DeploymentScheduler").Any())
            {
                return;
            }

            MessageBox.Show("Services are not yet running, try again in a minute.", "Scheduler - Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(0);
        }

        public void AddAppScheduler()
        {
            Dispatcher.Invoke(() =>
            {
                Topmost = true;
                BtApps_MouseLeftButtonDown(BtApps, null);
                BtApps_MouseLeftButtonUp(BtApps, null);
                BuildAvailableAppLayout();
                BuildIpuAppLayout();
                BuildSupLayout();
            });
        }

        public void AddIpuAppScheduler()
        {
            Dispatcher.Invoke(() =>
            {
                Topmost = true;
                BtFuUpdate_MouseLeftButtonDown(BtFuUpdate, null);
                BtFuUpdate_MouseLeftButtonUp(BtFuUpdate, null);
                BuildAppLayout();
                BuildAvailableAppLayout();
                BuildSupLayout();
            });
        }

        public void AddSupScheduler()
        {
            Dispatcher.Invoke(() =>
            {
                Topmost = true;
                BtUpdates_MouseLeftButtonDown(BtUpdates, null);
                BtUpdates_MouseLeftButtonUp(BtUpdates, null);
                BuildAppLayout();
                BuildAvailableAppLayout();
                BuildIpuAppLayout();
            });
        }

        public void RebootScheduler()
        {
            Dispatcher.Invoke(() =>
            {
                Topmost = true;
                BtRestart_MouseLeftButtonDown(BtRestart, null);
                BtRestart_MouseLeftButtonUp(BtRestart, null);
                BuildAppLayout();
                BuildAvailableAppLayout();
                BuildIpuAppLayout();
                BuildSupLayout();
            });
        }

        public void OpenWizadTab()
        {
            Dispatcher.Invoke(() =>
            {
                Topmost = true;
                BtPlanner_MouseLeftButtonDown(BtPlanner, null);
                BtPlanner_MouseLeftButtonUp(BtPlanner, null);
                BuildAvailableAppLayout();
                BuildIpuAppLayout();
                BuildSupLayout();
            });
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_SHOWMENOW)
            {
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }
            }

            return IntPtr.Zero;
        }

        private void ApplyBranding()
        {
            Title = "Deployment scheduler - " + Assembly.GetExecutingAssembly().GetName().Version.ToString();

            var branding = CcmUtils.GetBranding();

            if (branding == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(branding.Logo))
            {
                Orglogo.Source = Utils.ToBitmapImage(branding.Logo);
                Globals.Log.Information("Loaded SC custom logo");
            }

            if (!string.IsNullOrEmpty(branding.Color))
            {
                MainWnd.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom(branding.Color);
                Globals.Log.Information("Loaded SC custom color");
            }

            if (!string.IsNullOrEmpty(branding.Orgname))
            {
                OrgName.Text = branding.Orgname;
                Globals.Log.Information("Loaded SC custom organization name");
            }
        }

        private bool _onNewUpdateRunning = false;

        private void CcmWmiEventListener_OnNewUpdate(object sender, NewUpdateEventArg e)
        {
            if (_onNewUpdateRunning)
            {
                return;
            }

            _onNewUpdateRunning = true;
            Thread.Sleep(65000);

            Dispatcher.Invoke(() =>
            {
                try
                {
                    BuildSupLayout();
                }
                catch (Exception ex)
                {
                    Globals.Log.Error(ex.Message);
                }

                _onNewUpdateRunning = false;
            });
        }

        private bool _onNewApplicationRunning = false;

        private void CcmWmiEventListener_OnNewApplication(object sender, NewApplicationEventArg e)
        {
            if (_onNewApplicationRunning)
            {
                return;
            }

            _onNewApplicationRunning = true;
            Thread.Sleep(35000);

            Dispatcher.Invoke(() =>
            {
                try
                {
                    BuildAppLayout();
                    BuildAvailableAppLayout();
                    BuildIpuAppLayout();
                }
                catch (Exception ex)
                {
                    Globals.Log.Error(ex.Message);
                }

                _onNewApplicationRunning = false;
            });
        }

        private bool _buildingBuildIpuAppLayout = false;

        private async void BuildIpuAppLayout()
        {
            if (_buildingBuildIpuAppLayout)
            {
                return;
            }

            _buildingBuildIpuAppLayout = true;

            SpFeatureUpdates.Children.Clear();
            SpFeatureUpdates.Visibility = Visibility.Hidden;
            PbContainer.Visibility = Visibility.Visible;

            if (SqlCe.GetAutoEnforceFlag())
            {
                Dispatcher.Invoke(() =>
                {
                    SpFeatureUpdates.Children.Add(new TextBlock
                    {
                        Text = Globals.Settings.ServiceConfig.ServiceCycleTabText,
                        Margin = new Thickness(8),
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        Background = System.Windows.Media.Brushes.Yellow,
                    });
                });

                PbContainer.Visibility = Visibility.Hidden;
                SpFeatureUpdates.Visibility = Visibility.Visible;

                _buildingBuildIpuAppLayout = false;

                return;
            }

            await Task.Run(() =>
            {
                var dtServiceTime = CommonUtils.GetNextServiceCycleAsDateTime();
                var pendingSchedules = SqlCe.GetPendingSchedules();
                var allRequiredApps = CcmUtils.RequiredApps.Where(x => x.IsIpuApplication).GroupBy(x => x.Id).Select(y => y.Last()).ToList();
                var allAppsInstalled = allRequiredApps.Where(x => x.InstallState.Equals("Installed")).ToList();
                var allAppsNotInstalled = allRequiredApps.Where(x => !x.InstallState.Equals("Installed") && x.Deadline > DateTime.Now).ToList();
                var totalAppsCount = allAppsNotInstalled.Count;

                if (allRequiredApps.Count > 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        SpFeatureUpdates.Children.Add(new TextBlock
                        {
                            Text = Globals.Settings.IpuApplication.UiTitle,
                            Margin = new Thickness(8, 8, 30, 8),
                            FontSize = 14,
                            FontWeight = FontWeights.SemiBold,
                        });

                        SpFeatureUpdates.Children.Add(new TextBlock
                        {
                            Text = Globals.Settings.IpuApplication.UiInfo,
                            Margin = new Thickness(8, 8, 30, 8),
                            FontSize = 12,
                            FontWeight = FontWeights.Normal,
                            TextWrapping = TextWrapping.Wrap,
                        });
                    });

                    if (pendingSchedules.Where(x => x.EnforcementType.Equals("APP")).Any())
                    {
                        var pendingapps = pendingSchedules.Where(x => x.EnforcementType.Equals("APP")).OrderBy(x => x.EnforcementTime);

                        foreach (var pendingapp in pendingapps)
                        {
                            var apps = allAppsNotInstalled.Where(x => x.Id.Trim().Equals(pendingapp.ObjectId.Trim()) && x.Revision.Trim().Equals(pendingapp.Revision.Trim()));

                            if (apps.Count() == 1)
                            {
                                try
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        SpFeatureUpdates.Children.Add(new RequiredAppsControl(apps.First(), pendingapp.EnforcementTime, pendingapp.Id, dtServiceTime));
                                        //totalAppsCount--;
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Globals.Log.Error(ex.Message);
                                }
                            }
                        }
                    }

                    foreach (var anyApp in allAppsNotInstalled)
                    {
                        if (pendingSchedules.Where(x => x.ObjectId.Trim().Equals(anyApp.Id.Trim()) && x.Revision.Trim().Equals(anyApp.Revision.Trim())).Any())
                        {
                            continue;
                        }

                        try
                        {
                            Dispatcher.Invoke(() =>
                            {
                                SpFeatureUpdates.Children.Add(new RequiredAppsControl(anyApp, dtServiceTime));
                            });
                        }
                        catch (Exception ex)
                        {
                            Globals.Log.Error(ex.Message);
                        }
                    }

                    Dispatcher.Invoke(() =>
                    {
                        if (totalAppsCount > 0)
                        {
                            BtFuUpdate.Visibility = Visibility.Visible;

                            SpFeatureUpdates.Children.Insert(2, new TextBlock
                            {
                                Text = Globals.Settings.RequiredAppsSettings.TopTitle,
                                Margin = new Thickness(8),
                                FontSize = 14,
                                FontWeight = FontWeights.SemiBold,
                            });
                        }
                        else
                        {
                            BtFuUpdate.Visibility = Visibility.Collapsed;
                        }
                    });
                }
            });

            PbContainer.Visibility = Visibility.Hidden;
            SpFeatureUpdates.Visibility = Visibility.Visible;

            _buildingBuildIpuAppLayout = false;
        }

        private bool _buildingBuildAppLayout = false;

        private async void BuildAppLayout()
        {
            if (!Globals.Settings.ActiveTabs.RequiredApps)
            {
                return;
            }

            if (_buildingBuildAppLayout)
            {
                return;
            }

            _buildingBuildAppLayout = true;

            SpApps.Children.Clear();
            SpApps.Visibility = Visibility.Hidden;
            PbContainer.Visibility = Visibility.Visible;
            BorderCountRequiredApps.Visibility = Visibility.Hidden;

            if (SqlCe.GetAutoEnforceFlag())
            {
                Dispatcher.Invoke(() =>
                {
                    SpApps.Children.Insert(0, new TextBlock
                    {
                        Text = Globals.Settings.ServiceConfig.ServiceCycleTabText,
                        Margin = new Thickness(8),
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        Background = System.Windows.Media.Brushes.Yellow,
                    });
                });

                PbContainer.Visibility = Visibility.Hidden;
                SpApps.Visibility = Visibility.Visible;

                _buildingBuildAppLayout = false;

                return;
            }

            await Task.Run(() =>
            {
                var dtServiceTime = CommonUtils.GetNextServiceCycleAsDateTime();
                var pendingSchedules = SqlCe.GetPendingSchedules();
                var allRequiredApps = CcmUtils.RequiredApps.Where(x => !x.IsIpuApplication).GroupBy(x => x.Id).Select(y => y.Last()).ToList();
                var allAppsInstalled = allRequiredApps.Where(x => x.InstallState.Equals("Installed")).ToList();
                var allAppsNotInstalled = allRequiredApps.Where(x => !x.InstallState.Equals("Installed") && x.Deadline > DateTime.Now).ToList();
                var totalAppsCount = allAppsNotInstalled.Count;
                Globals.Log.Information($"Required Apps = {allRequiredApps.Count()} Installed Apps: {allAppsInstalled.Count()} Apps Not Installed: {allAppsNotInstalled.Count()}");

                if (allRequiredApps.Count < 1)
                {
                    Dispatcher.Invoke(() =>
                    {
                        BorderCountRequiredApps.Visibility = Visibility.Hidden;
                    });
                }
                else
                {
                    if (pendingSchedules.Where(x => x.EnforcementType.Equals("APP")).Any())
                    {
                        var pendingapps = pendingSchedules.Where(x => x.EnforcementType.Equals("APP")).OrderBy(x => x.EnforcementTime);

                        foreach (var pendingapp in pendingapps)
                        {
                            var apps = allAppsNotInstalled.Where(x => x.Id.Trim().Equals(pendingapp.ObjectId.Trim()) && x.Revision.Trim().Equals(pendingapp.Revision.Trim()));

                            if (apps.Count() == 1)
                            {
                                try
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        SpApps.Children.Add(new RequiredAppsControl(apps.First(), pendingapp.EnforcementTime, pendingapp.Id, dtServiceTime));
                                        totalAppsCount--;
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Globals.Log.Error(ex.Message);
                                }
                            }
                        }
                    }

                    foreach (var anyApp in allAppsNotInstalled)
                    {
                        if (pendingSchedules.Where(x => x.ObjectId.Trim().Equals(anyApp.Id.Trim()) && x.Revision.Trim().Equals(anyApp.Revision.Trim())).Any())
                        {
                            continue;
                        }

                        try
                        {
                            Dispatcher.Invoke(() =>
                            {
                                SpApps.Children.Insert(0, new RequiredAppsControl(anyApp, dtServiceTime));
                            });
                        }
                        catch (Exception ex)
                        {
                            Globals.Log.Error(ex.Message);
                        }
                    }

                    foreach (var app in allAppsInstalled.Where(x => x.AllowedActions.Contains("Repair")))
                    {
                        try
                        {
                            var pendingapps = pendingSchedules.Where(x => x.EnforcementType.Equals("APP")).OrderBy(x => x.EnforcementTime);
                            var pendingApp = pendingapps.Where(x => x.ObjectId.Trim().Equals(app.Id.Trim()) && x.Revision.Trim().Equals(app.Revision.Trim()));

                            if (pendingApp.Count() == 1)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    SpApps.Children.Add(new RequiredAppsControl(app, pendingApp.First().EnforcementTime, pendingApp.First().Id, dtServiceTime));
                                    totalAppsCount--;
                                });
                            }
                            else
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    SpApps.Children.Add(new RequiredAppsControl(app, dtServiceTime));
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Globals.Log.Error(ex.Message);
                        }
                    }

                    if (totalAppsCount > 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            BorderCountRequiredApps.Visibility = Visibility.Visible;
                            TextBlockCountRequiredApps.Text = totalAppsCount.ToString();
                        });
                    }
                }
            });

            Dispatcher.Invoke(() =>
            {
                SpApps.Children.Insert(0, new TextBlock
                {
                    Text = SpApps.Children.Count > 0 ? Globals.Settings.RequiredAppsSettings.TopTitle : Globals.Settings.RequiredAppsSettings.TopTitleNotAvailable,
                    Margin = new Thickness(8),
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                });
            });

            PbContainer.Visibility = Visibility.Hidden;
            SpApps.Visibility = Visibility.Visible;

            _buildingBuildAppLayout = false;
        }

        private bool _buildingBuildSupLayout = false;

        private async void BuildSupLayout()
        {
            if (!Globals.Settings.ActiveTabs.Updates)
            {
                return;
            }

            if (_buildingBuildSupLayout)
            {
                return;
            }

            _buildingBuildSupLayout = true;

            SpUpdates.Children.Clear();
            PbContainer.Visibility = Visibility.Visible;
            SpUpdates.Visibility = Visibility.Hidden;
            BorderCountUpdates.Visibility = Visibility.Hidden;

            if (SqlCe.GetAutoEnforceFlag())
            {
                Dispatcher.Invoke(() =>
                {
                    SpUpdates.Children.Insert(0, new TextBlock
                    {
                        Text = Globals.Settings.ServiceConfig.ServiceCycleTabText,
                        Margin = new Thickness(8),
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        Background = System.Windows.Media.Brushes.Yellow,
                    });
                });

                PbContainer.Visibility = Visibility.Hidden;
                SpUpdates.Visibility = Visibility.Visible;

                _buildingBuildSupLayout = false;

                return;
            }

            await Task.Run(() =>
            {
                var dtServiceTime = CommonUtils.GetNextServiceCycleAsDateTime();
                var nowEnforcing = SqlCe.GetEnforcingSchedules();

                if (nowEnforcing.Where(x => x.EnforcementType.Equals("SUP")).Any())
                {
                    Globals.Log.Warning("Software updates were enforcest less than 5 minutes ago, skipping load until state can be evaluated");
                }
                else
                {
                    var pendingSchedules = SqlCe.GetPendingSchedules();

                    if (pendingSchedules.Where(x => x.EnforcementType.Equals("SUP")).Any())
                    {
                        var lastsup = pendingSchedules.Where(x => x.EnforcementType.Equals("SUP")).OrderBy(x => x.EnforcementTime).Last();
                        var jsonSup = SqlCe.GetSupData("STD");

                        if (!string.IsNullOrEmpty(jsonSup))
                        {
                            try
                            {
                                var sups = JsonConvert.DeserializeObject<List<Update>>(jsonSup);

                                Dispatcher.Invoke(() =>
                                {
                                    SpUpdates.Children.Insert(0, new TextBlock { Text = Globals.Settings.UpdatesSettings.UpdatesAreScheduledTitleText, Height = 22, FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(4, 2, 0, 0) });
                                    SpUpdates.Children.Insert(1, new UpdatesControl(sups, lastsup.EnforcementTime, lastsup.Id, dtServiceTime));
                                });
                            }
                            catch (Exception ex)
                            {
                                Globals.Log.Error(ex.Message);
                            }
                        }
                    }
                    else
                    {
                        var jsonSup1 = string.Empty;

                        try
                        {
                            jsonSup1 = SqlCe.GetSupData("STD");
                        }
                        catch { }

                        if (!string.IsNullOrEmpty(jsonSup1))
                        {
                            try
                            {
                                var sups1 = JsonConvert.DeserializeObject<List<Update>>(jsonSup1);

                                if (sups1.Count() > 0)
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        SpUpdates.Children.Insert(0, new TextBlock { Text = Globals.Settings.UpdatesSettings.UpdatesAvailableTitleText, Height = 22, FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(4, 2, 0, 0) });
                                        SpUpdates.Children.Insert(1, new UpdatesControl(sups1, dtServiceTime));
                                        BorderCountUpdates.Visibility = Visibility.Visible;
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                Globals.Log.Error(ex.Message);
                            }
                        }
                    }
                }
            });

            PbContainer.Visibility = Visibility.Hidden;
            SpUpdates.Visibility = Visibility.Visible;

            if (SpUpdates.Children.Count == 0)
            {
                Dispatcher.Invoke(() =>
                {
                    SpUpdates.Children.Insert(0, new TextBlock
                    {
                        Text = Globals.Settings.UpdatesSettings.TopTitleNotAvailable,
                        Margin = new Thickness(8),
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                    });
                });
            }

            _buildingBuildSupLayout = false;
        }

        private async void BtUpdates_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                tcSchedules.SelectedIndex = 2;
                RestoreButtons();

                if (sender is Grid grid)
                {
                    _activeGrid = grid;
                    grid.Background = MainWnd.Background;

                    foreach (var obj in grid.Children)
                    {
                        if (obj is DockPanel dp)
                        {
                            foreach (var child in dp.Children)
                            {
                                if (child is TextBlock tb)
                                {
                                    tb.Foreground = new SolidColorBrush(Colors.White);
                                }

                                if (child is System.Windows.Controls.Image image)
                                {
                                    image.Source = new BitmapImage(new Uri("Images/w_Updates.png", UriKind.Relative));
                                }
                            }
                        }
                    }
                }
            });
        }

        private async void BtUpdates_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                BuildSupLayout();
            });
        }

        private Grid _activeGrid;

        private void RestoreButtons()
        {
            foreach (var bt in SpButtons.Children)
            {
                if (bt is Grid grid)
                {
                    grid.Background = new SolidColorBrush(Colors.Transparent);

                    if (grid.Name.Equals("BtFuUpdate"))
                    {
                        foreach (var obj in grid.Children)
                        {
                            if (obj is DockPanel dp)
                            {
                                foreach (var child in dp.Children)
                                {
                                    if (child is TextBlock tb)
                                    {
                                        tb.Foreground = new SolidColorBrush(Colors.Black);
                                    }

                                    /*
                                    if (child is ImageAwesome imageAwesome)
                                    {
                                        imageAwesome.Foreground = System.Windows.Media.Brushes.Black;
                                    }
                                    */
                                }
                            }
                        }
                    }
                    else if (grid.Name.Equals("BtRestart") || grid.Name.Equals("BtPlanner") || grid.Name.Equals("BtFeedBack"))
                    {
                        foreach (var obj in grid.Children)
                        {
                            if (obj is DockPanel dp)
                            {
                                foreach (var child in dp.Children)
                                {
                                    if (child is TextBlock tb)
                                    {
                                        tb.Foreground = new SolidColorBrush(Colors.Black);
                                    }

                                    if (child is ImageAwesome imageAwesome)
                                    {
                                        imageAwesome.Foreground = System.Windows.Media.Brushes.Black;
                                    }
                                }
                            }
                        }
                    }
                    else if (grid.Name.Equals("BtUpdates"))
                    {
                        foreach (var obj in grid.Children)
                        {
                            if (obj is DockPanel dp)
                            {
                                foreach (var child in dp.Children)
                                {
                                    if (child is TextBlock tb)
                                    {
                                        tb.Foreground = new SolidColorBrush(Colors.Black);
                                    }

                                    if (child is System.Windows.Controls.Image image)
                                    {
                                        image.Source = new BitmapImage(new Uri("Images/Updates.png", UriKind.Relative));
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var obj in grid.Children)
                        {
                            if (obj is DockPanel dp)
                            {
                                foreach (var child in dp.Children)
                                {
                                    if (child is TextBlock tb)
                                    {
                                        tb.Foreground = new SolidColorBrush(Colors.Black);
                                    }

                                    if (child is System.Windows.Controls.Image image)
                                    {
                                        image.Source = new BitmapImage(new Uri("Images/App.png", UriKind.Relative));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private async void BtFuUpdate_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                tcSchedules.SelectedIndex = 0;
                RestoreButtons();

                if (sender is Grid grid)
                {
                    _activeGrid = grid;
                    grid.Background = MainWnd.Background;

                    foreach (var obj in grid.Children)
                    {
                        if (obj is DockPanel dp)
                        {
                            foreach (var child in dp.Children)
                            {
                                if (child is TextBlock tb)
                                {
                                    tb.Foreground = new SolidColorBrush(Colors.White);
                                }
                            }
                        }
                    }
                }
            });
        }

        private void BtFuUpdate_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            BuildIpuAppLayout();
        }

        private async void BtApps_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                tcSchedules.SelectedIndex = 1;
                RestoreButtons();

                if (sender is Grid grid)
                {
                    _activeGrid = grid;
                    grid.Background = MainWnd.Background;

                    foreach (var obj in grid.Children)
                    {
                        if (obj is DockPanel dp)
                        {
                            foreach (var child in dp.Children)
                            {
                                if (child is TextBlock tb)
                                {
                                    tb.Foreground = new SolidColorBrush(Colors.White);
                                }

                                if (child is System.Windows.Controls.Image image)
                                {
                                    image.Source = new BitmapImage(new Uri("Images/w_App.png", UriKind.Relative));
                                }
                            }
                        }
                    }
                }
            });
        }

        private void BtApps_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            BuildAppLayout();
        }

        private bool _buildingBuildAvailableAppLayout = false;

        private async void BuildAvailableAppLayout()
        {
            if (!Globals.Settings.ActiveTabs.AvailableApps)
            {
                return;
            }

            if (_buildingBuildAvailableAppLayout)
            {
                return;
            }

            _buildingBuildAvailableAppLayout = true;

            SpAvailableApps.Children.Clear();
            PbContainer.Visibility = Visibility.Visible;
            SpAvailableApps.Visibility = Visibility.Hidden;
            BorderCountAvailableApps.Visibility = Visibility.Hidden;

            if (SqlCe.GetAutoEnforceFlag())
            {
                Dispatcher.Invoke(() =>
                {
                    SpAvailableApps.Children.Insert(0, new TextBlock
                    {
                        Text = Globals.Settings.ServiceConfig.ServiceCycleTabText,
                        Margin = new Thickness(8),
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        Background = System.Windows.Media.Brushes.Yellow,
                    });
                });

                PbContainer.Visibility = Visibility.Hidden;
                SpAvailableApps.Visibility = Visibility.Visible;

                _buildingBuildAvailableAppLayout = false;

                return;
            }

            await Task.Run(() =>
            {
                var dtServiceTime = CommonUtils.GetNextServiceCycleAsDateTime();
                var pendingSchedules = SqlCe.GetPendingSchedules();
                var appsAvailable = CcmUtils.AvailableApps.GroupBy(x => x.Id).Select(y => y.Last()).ToList();
                var totalAppsCount = appsAvailable.Count;
                Globals.Log.Information($"Available apps Count = {appsAvailable.Count()}");

                if (appsAvailable.Count() < 1)
                {
                    Dispatcher.Invoke(() =>
                    {
                        BorderCountAvailableApps.Visibility = Visibility.Hidden;
                    });
                }
                else
                {
                    var appList = new List<AvailableAppsControl>();

                    if (pendingSchedules.Where(x => x.EnforcementType.Equals("APP")).Any())
                    {
                        var pendingapps = pendingSchedules.Where(x => x.EnforcementType.Equals("APP")).OrderBy(x => x.EnforcementTime);

                        foreach (var pendingapp in pendingapps)
                        {
                            var apps = appsAvailable.Where(x => x.Id.Trim().Equals(pendingapp.ObjectId.Trim()) && x.Revision.Trim().Equals(pendingapp.Revision.Trim()));

                            if (apps.Count() == 1)
                            {
                                try
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        SpAvailableApps.Children.Add(new AvailableAppsControl(apps.First(), pendingapp.EnforcementTime, pendingapp.Id, dtServiceTime));
                                        totalAppsCount--;
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Globals.Log.Error(ex.Message);
                                }
                            }
                        }
                    }

                    var nowEnforcing = SqlCe.GetEnforcingSchedules();

                    foreach (var anyApp in appsAvailable)
                    {
                        if (pendingSchedules.Where(x => x.ObjectId.Trim().Equals(anyApp.Id.Trim()) && x.Revision.Trim().Equals(anyApp.Revision.Trim())).Any())
                        {
                            continue;
                        }

                        try
                        {
                            Dispatcher.Invoke(() =>
                            {
                                SpAvailableApps.Children.Insert(0, new AvailableAppsControl(anyApp, dtServiceTime));

                                if (anyApp.InstallState.Equals("Installed"))
                                {
                                    totalAppsCount--;
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Globals.Log.Error(ex.Message);
                        }
                    }

                    if (totalAppsCount > 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            BorderCountAvailableApps.Visibility = Visibility.Visible;
                            TextBlockCountAvailableApps.Text = totalAppsCount.ToString();
                        });
                    }
                }
            });

            Dispatcher.Invoke(() =>
            {
                SpAvailableApps.Children.Insert(0, new TextBlock
                {
                    Text = SpAvailableApps.Children.Count > 0 ? Globals.Settings.AvailableAppsSettings.TopTitle : Globals.Settings.AvailableAppsSettings.TopTitleNotAvailable,
                    Margin = new Thickness(8),
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                });
            });

            PbContainer.Visibility = Visibility.Hidden;
            SpAvailableApps.Visibility = Visibility.Visible;

            _buildingBuildAvailableAppLayout = false;
        }

        private async void BtMoreApps_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                tcSchedules.SelectedIndex = 3;
                RestoreButtons();

                if (sender is Grid grid)
                {
                    _activeGrid = grid;
                    grid.Background = Background;

                    foreach (var obj in grid.Children)
                    {
                        if (obj is DockPanel dp)
                        {
                            foreach (var child in dp.Children)
                            {
                                if (child is TextBlock tb)
                                {
                                    tb.Foreground = new SolidColorBrush(Colors.White);
                                }

                                if (child is System.Windows.Controls.Image image)
                                {
                                    image.Source = new BitmapImage(new Uri("Images/w_App.png", UriKind.Relative));
                                }
                            }
                        }
                    }
                }
            });
        }

        private async void BtMoreApps_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                BuildAvailableAppLayout();
            });
        }

        private bool _buildingFeedback = false;

        private void BtFeedBack_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_buildingFeedback)
            {
                return;
            }

            _buildingFeedback = true;
            FeedbackGrid.Children.Clear();

            if (Globals.Settings.FeedbackConfig.FeedBackType == FeedbackType.None)
            {
                _buildingFeedback = false;
                return;
            }
            else if (Globals.Settings.FeedbackConfig.FeedBackType == FeedbackType.Url)
            {
                if (!string.IsNullOrEmpty(Globals.Settings.FeedbackConfig.Url))
                {
                    FeedbackGrid.Children.Add(new WebBrowserControl(new Uri(Globals.Settings.FeedbackConfig.Url)));
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(Globals.Settings.FeedbackConfig.MailSettings.MailTo) || !string.IsNullOrEmpty(Globals.Settings.FeedbackConfig.MailSettings.SmtpServer))
                {
                    FeedbackGrid.Children.Add(new MailFeedbackControl(Globals.Settings.FeedbackConfig.MailSettings));
                }
            }

            _buildingFeedback = false;
        }

        private async void BtFeedBack_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                tcSchedules.SelectedIndex = 6;
                RestoreButtons();

                if (sender is Grid grid)
                {
                    _activeGrid = grid;
                    grid.Background = MainWnd.Background;

                    foreach (var obj in grid.Children)
                    {
                        if (obj is DockPanel dp)
                        {
                            foreach (var child in dp.Children)
                            {
                                if (child is TextBlock tb)
                                {
                                    tb.Foreground = new SolidColorBrush(Colors.White);
                                }

                                if (child is ImageAwesome imageAwesome)
                                {
                                    imageAwesome.Foreground = System.Windows.Media.Brushes.White;
                                }
                            }
                        }
                    }
                }
            });
        }

        private void TabApps_Unselected(object sender, RoutedEventArgs e)
        {
            SpApps.Children.Clear();
        }

        private void TabAvailableApps_Unselected(object sender, RoutedEventArgs e)
        {
            SpAvailableApps.Children.Clear();
        }

        private void TabUpdates_Unselected(object sender, RoutedEventArgs e)
        {
            SpUpdates.Children.Clear();
        }

        private void TabFeedback_Unselected(object sender, RoutedEventArgs e)
        {
        }

        private void MainWnd_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MainWnd_Loaded;
            DataContext = Globals.Settings;
            ReadWindowPosion();
        }

        private void ReadWindowPosion()
        {
            try
            {
                var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Onevinn\\DeploymentScheduler");

                var oTop = key.GetValue("Top");
                if (oTop != null)
                    Top = Convert.ToInt64(oTop);

                var oLeft = key.GetValue("Left");
                if (oLeft != null)
                    Left = Convert.ToInt64(oLeft);

                var oWidth = key.GetValue("Width");
                if (oWidth != null)
                    Width = Convert.ToInt64(oWidth);

                var oHeight = key.GetValue("Height");
                if (oHeight != null)
                    Height = Convert.ToInt64(oHeight);

                var oState = key.GetValue("State");
                if (oState != null)
                    WindowState = (WindowState)oState;
            }
            catch { }
        }

        private void MainWnd_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveWindowPosition();
        }

        private void SaveWindowPosition()
        {
            RegistryKey key;
            key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Onevinn\\DeploymentScheduler");
            key.SetValue("Top", Top, RegistryValueKind.QWord);
            key.SetValue("Left", Left, RegistryValueKind.QWord);
            key.SetValue("Width", Width, RegistryValueKind.QWord);
            key.SetValue("Height", Height, RegistryValueKind.QWord);
            key.SetValue("State", Convert.ToInt32(WindowState), RegistryValueKind.DWord);
            key.Close();
        }

        private void MainWnd_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5 && _activeGrid != null)
            {
                if (_activeGrid == BtApps)
                {
                    BtApps_MouseLeftButtonDown(BtApps, null);
                    BtApps_MouseLeftButtonUp(BtApps, null);
                }
                else if (_activeGrid == BtUpdates)
                {
                    BtUpdates_MouseLeftButtonDown(BtUpdates, null);
                    BtUpdates_MouseLeftButtonUp(BtUpdates, null);
                }
                else if (_activeGrid == BtMoreApps)
                {
                    BtMoreApps_MouseLeftButtonDown(BtMoreApps, null);
                    BtMoreApps_MouseLeftButtonUp(BtMoreApps, null);
                }
                else if (_activeGrid == BtRestart)
                {
                    BtRestart_MouseLeftButtonDown(BtRestart, null);
                    BtRestart_MouseLeftButtonUp(BtRestart, null);
                }
                else if (_activeGrid == BtPlanner)
                {
                    BtPlanner_MouseLeftButtonDown(BtPlanner, null);
                    BtPlanner_MouseLeftButtonUp(BtPlanner, null);
                }
                else if (_activeGrid == BtFeedBack)
                {
                    BtFeedBack_MouseLeftButtonDown(BtFeedBack, null);
                    BtFeedBack_MouseLeftButtonUp(BtFeedBack, null);
                }
            }
        }

        private async void BtRestart_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                tcSchedules.SelectedIndex = 4;
                RestoreButtons();

                if (sender is Grid grid)
                {
                    _activeGrid = grid;
                    grid.Background = MainWnd.Background;

                    foreach (var obj in grid.Children)
                    {
                        if (obj is DockPanel dp)
                        {
                            foreach (var child in dp.Children)
                            {
                                if (child is TextBlock tb)
                                {
                                    tb.Foreground = new SolidColorBrush(Colors.White);
                                }

                                if (child is ImageAwesome imageAwesome)
                                {
                                    imageAwesome.Foreground = System.Windows.Media.Brushes.White;
                                }
                            }
                        }
                    }
                }
            });
        }

        private async void BtRestart_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                BuildRestart();
            });
        }

        private void TabRestart_Unselected(object sender, RoutedEventArgs e)
        {
            SpRestart.Children.Clear();
        }

        private bool _buildingBuildRestart = false;

        private void BuildRestart()
        {
            if (_buildingBuildRestart)
            {
                return;
            }

            _buildingBuildRestart = true;
            SpRestart.Children.Clear();

            if (SqlCe.GetAutoEnforceFlag())
            {
                Dispatcher.Invoke(() =>
                {
                    SpRestart.Children.Insert(0, new TextBlock
                    {
                        Text = Globals.Settings.ServiceConfig.ServiceCycleTabText,
                        Margin = new Thickness(8),
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        Background = System.Windows.Media.Brushes.Yellow,
                    });
                });

                PbContainer.Visibility = Visibility.Hidden;
                SpRestart.Visibility = Visibility.Visible;

                _buildingBuildRestart = false;

                return;
            }

            if (SqlCe.GetRestartSchedule() != null)
            {
                SpRestart.Children.Add(new RestartControl { TopTitle = Globals.Settings.RestartSettings.TopTitle });
            }
            else
            {
                SpRestart.Children.Insert(0, new TextBlock
                {
                    Text = Globals.Settings.RestartSettings.TopTitleNotAvailable,
                    Margin = new Thickness(8),
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                });
            }

            _buildingBuildRestart = false;
        }

        private async void BtPlanner_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                tcSchedules.SelectedIndex = 5;
                RestoreButtons();

                if (sender is Grid grid)
                {
                    _activeGrid = grid;
                    grid.Background = MainWnd.Background;

                    foreach (var obj in grid.Children)
                    {
                        if (obj is DockPanel dp)
                        {
                            foreach (var child in dp.Children)
                            {
                                if (child is TextBlock tb)
                                {
                                    tb.Foreground = new SolidColorBrush(Colors.White);
                                }

                                if (child is ImageAwesome imageAwesome)
                                {
                                    imageAwesome.Foreground = System.Windows.Media.Brushes.White;
                                }
                            }
                        }
                    }
                }
            });
        }

        private bool _buildingPlannerLayout = false;

        private async void BtPlanner_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_buildingPlannerLayout)
            {
                return;
            }

            _buildingPlannerLayout = true;

            PbContainer.Visibility = Visibility.Visible;
            SpPlanner.Visibility = Visibility.Hidden;
            IEnumerable<CMApplication> apps = null;
            IEnumerable<Update> upds = null;
            SpPlanner.Children.Clear();

            if (SqlCe.GetAutoEnforceFlag())
            {
                Dispatcher.Invoke(() =>
                {
                    SpPlanner.Children.Insert(0, new TextBlock
                    {
                        Text = Globals.Settings.ServiceConfig.ServiceCycleTabText,
                        Margin = new Thickness(8),
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        Background = System.Windows.Media.Brushes.Yellow,
                    });
                });

                PbContainer.Visibility = Visibility.Hidden;
                SpPlanner.Visibility = Visibility.Visible;

                _buildingPlannerLayout = false;

                return;
            }

            await Task.Run(() =>
            {
                apps = CcmUtils.RequiredApps.Where(x => !x.IsIpuApplication);
                var jsonSup = SqlCe.GetSupData("STD");

                if (!string.IsNullOrEmpty(jsonSup))
                {
                    upds = JsonConvert.DeserializeObject<List<Update>>(jsonSup);
                }
            });

            var dtServiceTime = CommonUtils.GetNextServiceCycleAsDateTime();

            SpPlanner.Children.Add(new ScheduleAllControl(apps, upds, dtServiceTime));
            SpPlanner.Children.Add(new AutoDeployControl());

            PbContainer.Visibility = Visibility.Hidden;
            SpPlanner.Visibility = Visibility.Visible;

            _buildingPlannerLayout = false;
        }

        private void TabPlanner_Unselected(object sender, RoutedEventArgs e)
        {
            SpPlanner.Children.Clear();
        }

        private void TabFeatureUpdates_Unselected(object sender, RoutedEventArgs e)
        {
            SpFeatureUpdates.Children.Clear();
        }
    }
}
