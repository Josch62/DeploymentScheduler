using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using SchedulerSettings;
using SchedulerSettings.Models;
using UserScheduler.Common;
using UserScheduler.Natives;

namespace UserScheduler.Windows
{
    /// <summary>
    /// Interaction logic for RestartWindow.xaml.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification ="Lazy")]
    public partial class RestartWindow : Window
    {
        #region ScaleValue Depdencies

        public static readonly DependencyProperty ScaleValueProperty = DependencyProperty.Register("ScaleValue", typeof(double), typeof(RestartWindow), new UIPropertyMetadata(1.0, new PropertyChangedCallback(OnScaleValueChanged), new CoerceValueCallback(OnCoerceScaleValue)));

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
            if (o is RestartWindow mainWindow)
                return mainWindow.OnCoerceScaleValue((double)value);
            else
                return value;
        }

        private static void OnScaleValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is RestartWindow mainWindow)
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
            //CalculateScale();
        }

        private void CalculateScale()
        {
            var yScale = ActualHeight / 175.0d;
            var xScale = ActualWidth / 400.0d;
            var value = Math.Min(xScale, yScale);
            ScaleValue = (double)OnCoerceScaleValue(MainGrid, value);
        }

        #endregion

        private readonly Timer _countDownTimer = new Timer();
        private readonly DateTime _startTime = DateTime.Now;
        private readonly CountdownWindowSettings _settings = SettingsUtils.Settings.CountdownWindowSettings;
        private int _allowedTime = 900;
        private int _restoreInterval = 30;
        private int _keepOnTop = 120;
        private int _restoreCounter = 0;

        public RestartWindow()
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

        private void RestartWnd_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= RestartWnd_Loaded;

            InfoText.Text = _settings.InfoText;
            _allowedTime = _settings.TotalTime;
            _restoreInterval = _settings.RestoreInterval;
            _keepOnTop = _settings.KeepOnTop;
            Progress.Maximum = _settings.TotalTime;

            _countDownTimer.Interval = 1000;
            _countDownTimer.AutoReset = true;
            _countDownTimer.Elapsed += CountDownTimer_Elapsed;
            _countDownTimer.Start();
        }

        private void CountDownTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_allowedTime <= _keepOnTop)
                {
                    if (WindowState == WindowState.Minimized)
                    {
                        WindowState = WindowState.Normal;
                        Globals.Log.Information("Countdown window was restored from Taskbar.");
                    }

                    BtMinimize.Visibility = Visibility.Hidden;
                }

                if (_restoreCounter++ >= _restoreInterval)
                {
                    if (WindowState == WindowState.Minimized)
                    {
                        WindowState = WindowState.Normal;
                        Globals.Log.Information("Countdown window was restored from Taskbar.");
                    }

                    _restoreCounter = 0;
                }

                var timeLeft = _startTime.AddSeconds(_allowedTime--) - _startTime;

                if (_allowedTime <= 0)
                {
                    _countDownTimer.Stop();
                    BtRestartNow_Click(this, null);
                    return;
                }

                CountDownText.Text = timeLeft.ToString(@"hh\:mm\:ss");
                Progress.Value = Progress.Maximum - _allowedTime;
            });
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
        }

        private void BtMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            Globals.Log.Information("User minimized countdown window.");
        }

        private void BtRestartNow_Click(object sender, RoutedEventArgs e)
        {
            Closing -= RestartWnd_Closing;
            Globals.Log.Information("User pressed 'Restart now' in countdown window.");
            RestartComputer();
            Close();
        }

        private void RestartWnd_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }

        private void RestartComputer()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = $"/r /f /t 0",
                    WindowStyle = ProcessWindowStyle.Hidden,
                });
            }
            catch (Exception ex)
            {
                Globals.Log.Error(ex.Message);
            }
        }
    }
}
