using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SchedulerCommon.Ccm;
using SchedulerCommon.Common;
using SchedulerCommon.Sql;
using SchedulerSettings;
using SchedulerSettings.Models;
using UserScheduler.Common;
using UserScheduler.Natives;

namespace UserScheduler.Windows
{
    /// <summary>
    /// Interaction logic for ConfirmWindow.xaml.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification ="Lazy")]
    public partial class ConfirmWindow : Window
    {
        #region ScaleValue Depdencies

        public static readonly DependencyProperty ScaleValueProperty = DependencyProperty.Register("ScaleValue", typeof(double), typeof(ConfirmWindow), new UIPropertyMetadata(1.0, new PropertyChangedCallback(OnScaleValueChanged), new CoerceValueCallback(OnCoerceScaleValue)));

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
            if (o is ConfirmWindow mainWindow)
                return mainWindow.OnCoerceScaleValue((double)value);
            else
                return value;
        }

        private static void OnScaleValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is ConfirmWindow mainWindow)
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
            var yScale = ActualHeight / 175.0d;
            var xScale = ActualWidth / 400.0d;
            var value = Math.Min(xScale, yScale);
            ScaleValue = (double)OnCoerceScaleValue(MainGrid, value);
        }

        #endregion

        private readonly ConfirmWindowSettings _settings = SettingsUtils.Settings.ConfirmWindowSettings;

        public ConfirmWindow()
        {
            InitializeComponent();
            ApplyBranding();
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
                BannerGrid.Background = (Brush)new BrushConverter().ConvertFrom(branding.Color);
                Globals.Log.Information("Loaded SC custom color");
            }

            if (!string.IsNullOrEmpty(branding.Orgname))
            {
                OrgName.Text = branding.Orgname;
                Globals.Log.Information("Loaded SC custom organization name");
            }
            else
            {
                OrgName.Text = string.Empty;
            }
        }

        private void RestartWnd_Loaded(object sender, RoutedEventArgs e)
        {
            if (Globals.Args.Exist("ServiceSchedule"))
            {
                var nextServiceTime = CommonUtils.GetNextServiceCycleAsDateTime();

                if (nextServiceTime != null)
                {
                    RestartText.Text = _settings.InfoText.Replace("%TIME%", nextServiceTime.ToString());
                    _dtClose = ((DateTime)nextServiceTime).AddMinutes(-2);
                }
            }
            else
            {
                var allPendingSchedules = SqlCe.GetPendingSchedules();
                var first = allPendingSchedules.Where(x => x.EnforcementTime <= DateTime.Now.AddHours(4) && x.HasRaisedConfirm).First();

                RestartText.Text = _settings.InfoText.Replace("%TIME%", first.EnforcementTime.ToString());
                _dtClose = first.EnforcementTime.AddMinutes(-2);
            }

            AutoCloseLoop();
        }

        private DateTime _dtClose;

        private void AutoCloseLoop()
        {
            Task.Run(() =>
            {
                while (DateTime.Now < _dtClose)
                {
                    System.Threading.Thread.Sleep(120000);
                }

                Globals.Log.Information($"Confirm window automatically closing.");

                Dispatcher.Invoke(() =>
                {
                    Close();
                });
            });
        }

        private void BtConfirm_Click(object sender, RoutedEventArgs e)
        {
            Globals.Log.Information($"User '{Environment.UserName}' pressed button 'Confirm'.");
            Close();
        }

        private void BtView_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            Globals.Log.Information($"User '{Environment.UserName}' pressed button 'View'.");
            Close();
        }
    }
}
