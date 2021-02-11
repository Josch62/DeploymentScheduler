using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using SchedulerCommon.Ccm;
using UserScheduler.Common;
using UserScheduler.Natives;

namespace UserScheduler.Windows
{
    /// <summary>
    /// Interaction logic for IpuDialog.xaml.
    /// </summary>
    public partial class IpuDialog : Window
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private readonly bool _isRebootDialog = false;
        private int _countDown = 300;

        #region ScaleValue Depdencies

#pragma warning disable SA1202 // Elements must be ordered by access
        public static readonly DependencyProperty ScaleValueProperty = DependencyProperty.Register("ScaleValue", typeof(double), typeof(IpuDialog), new UIPropertyMetadata(1.0, new PropertyChangedCallback(OnScaleValueChanged), new CoerceValueCallback(OnCoerceScaleValue)));
#pragma warning restore SA1202 // Elements must be ordered by access

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
            if (o is IpuDialog mainWindow)
                return mainWindow.OnCoerceScaleValue((double)value);
            else
                return value;
        }

        private static void OnScaleValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is IpuDialog mainWindow)
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
            var yScale = ActualHeight / 230.0d;
            var xScale = ActualWidth / 450.0d;
            var value = Math.Min(xScale, yScale);
            ScaleValue = (double)OnCoerceScaleValue(MainGrid, value);
        }

        #endregion
        public IpuDialog(bool reboot = false)
        {
            InitializeComponent();
            _isRebootDialog = reboot;
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

        private void BtGo_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void BtAbort_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(1);
        }

        private void IpuDialogWnd_Loaded(object sender, RoutedEventArgs e)
        {
            var settings = Globals.Settings.IpuApplication;

            if (_isRebootDialog)
            {
                _countDown = settings.Dialog2Time;
                DialogText.Text = settings.Dialog2;
                BtGo.Content = settings.Dialog2StartButtonText;
            }
            else
            {
                _countDown = settings.Dialog1Time;
                BtTitleMinimize.Visibility = Visibility.Visible;
                DialogText.Text = settings.Dialog1;
                BtGo.Content = settings.Dialog1StartButtonText;
                BtAbort.Content = settings.Dialog1AbortButtonText;
                BtAbort.Visibility = Visibility.Visible;
            }

            Counter.Text = TimeSpan.FromSeconds(_countDown).ToString(@"hh\:mm\:ss");

            _timer.Interval = new TimeSpan(0, 0, 1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_countDown <= 0)
            {
                Environment.Exit(0);
                return;
            }

            Counter.Text = TimeSpan.FromSeconds(_countDown--).ToString(@"hh\:mm\:ss");
        }

        private void BtTitleMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BannerGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
