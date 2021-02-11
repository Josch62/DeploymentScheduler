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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using SchedulerSettings;

namespace IpuProgress
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region ScaleValue Depdencies

        public static readonly DependencyProperty ScaleValueProperty = DependencyProperty.Register("ScaleValue", typeof(double), typeof(MainWindow), new UIPropertyMetadata(1.0, new PropertyChangedCallback(OnScaleValueChanged), new CoerceValueCallback(OnCoerceScaleValue)));

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
            var yScale = ActualHeight / 16.0d;
            var xScale = ActualWidth / 240.0d;
            var value = Math.Min(xScale, yScale);
            ScaleValue = (double)OnCoerceScaleValue(MainGrid, value);
        }

        #endregion

        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - Width;
            Top = desktopWorkingArea.Bottom - Height;

            try
            {
                UpdateProgress();
                ToolTip = SettingsUtils.Settings.IpuApplication.ProgressTooltip;
                SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            }
            catch { }

            _timer.Interval = new TimeSpan(0, 0, 2);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - Width;
            Top = desktopWorkingArea.Bottom - Height;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();

            try
            {
                UpdateProgress();
            }
            catch { }

            _timer.Start();
        }

        public string IPUPhase
        {
            get
            {
                try
                {
                    var reg = Registry.LocalMachine;
                    var pKey = reg.OpenSubKey("SOFTWARE\\Onevinn\\DeploymentScheduler");
                    var oValue = pKey.GetValue("IPUPhase");

                    if (oValue != null)
                    {
                        return oValue.ToString();
                    }
                }
                catch { }

                return string.Empty;
            }
        }

        public double SetupProgress
        {
            get
            {
                try
                {
                    var reg = Registry.LocalMachine;
                    var pKey = reg.OpenSubKey("SYSTEM\\Setup\\MoSetup\\Volatile");
                    var oValue = pKey.GetValue("SetupProgress");

                    if (oValue != null)
                    {
                        var val = Convert.ToInt64(oValue.ToString());
                        return val < 0 || val > 100 ? 0 : val;
                    }
                }
                catch { }

                return 0;
            }
        }

        private void UpdateProgress()
        {
            var patching = false;

            switch (IPUPhase)
            {
                case "1":
                    TbStatusText.Text = SettingsUtils.Settings.IpuApplication.Phase1Text;
                    break;

                case "2":
                    TbStatusText.Text = SettingsUtils.Settings.IpuApplication.Phase2Text;
                    break;

                case "3":
                    TbStatusText.Text = SettingsUtils.Settings.IpuApplication.Phase3Text;
                    patching = true;
                    PbUpgrade.IsIndeterminate = true;
                    break;

                case "4":
                    TbStatusText.Text = SettingsUtils.Settings.IpuApplication.Phase4Text;
                    break;

                case "11":
                    TbStatusText.Text = SettingsUtils.Settings.IpuApplication.FullMediaStatusText;
                    break;

                default:
                    TbStatusText.Text = string.Empty;
                    break;
            }

            if (!patching)
            {
                PbUpgrade.IsIndeterminate = false;
                PbUpgrade.Value = SetupProgress;
            }
        }
    }
}
