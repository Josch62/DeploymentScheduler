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
using System.Windows.Shapes;
using SchedulerCommon.Ccm;
using UserScheduler.Common;

namespace UserScheduler.Windows
{
    /// <summary>
    /// Interaction logic for WarningDialog.xaml.
    /// </summary>
    public partial class WarningDialog : Window
    {
        public bool Abort { get; set; } = false;

        #region ScaleValue Depdencies

        public static readonly DependencyProperty ScaleValueProperty = DependencyProperty.Register("ScaleValue", typeof(double), typeof(WarningDialog), new UIPropertyMetadata(1.0, new PropertyChangedCallback(OnScaleValueChanged), new CoerceValueCallback(OnCoerceScaleValue)));

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
            if (o is WarningDialog mainWindow)
                return mainWindow.OnCoerceScaleValue((double)value);
            else
                return value;
        }

        private static void OnScaleValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is WarningDialog mainWindow)
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

        public WarningDialog()
        {
            InitializeComponent();
            ApplyBranding();
        }

        private void WarningWnd_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = Globals.Settings.InstallAllWarningDialogSettings;
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

        private void BTOk_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtCancel_Click(object sender, RoutedEventArgs e)
        {
            Abort = true;
            Close();
        }
    }
}
