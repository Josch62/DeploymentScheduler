using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using SchedulerCommon.Ccm;

namespace OnevinnTrayIcon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            StatusGrid.SelectionChanged += (obj, e) =>
              Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                StatusGrid.UnselectAll()));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                SetDataContext();
            }
            catch { }
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            Close();
        }

        private void SetDataContext()
        {
            var context = new ObservableCollection<TrayStatus>();
            var apps = CcmUtils.RequiredApps.Where(x => !x.InstallState.Equals("Installed") && x.Deadline > DateTime.Now).ToList();
            var updates = CcmUtils.GetUpdatesStatus();

            foreach (var obj in apps)
            {
                context.Add(new TrayStatus
                {
                    Name = obj.Name,
                    EvaluationStateText = "-",
                    ToolTipText = obj.EvaluationStateText,
                    PercentComplete = obj.PercentComplete,
                });
            }

            foreach (var obj in updates)
            {
                context.Add(new TrayStatus
                {
                    Name = obj.Name,
                    EvaluationStateText = obj.EvaluationStateText,
                    ToolTipText = obj.EvaluationStateText,
                    PercentComplete = obj.PercentComplete,
                });
            }

            SizeChanged -= Window_SizeChanged;
            StatusGrid.ItemsSource = null;
            SizeChanged += Window_SizeChanged;
            StatusGrid.ItemsSource = context;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            Top = desktopWorkingArea.Bottom - (Height + 10);
            Left = desktopWorkingArea.Right - (Width + 130);
        }

        private bool _refreshing = false;

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                if (_refreshing)
                {
                    return;
                }

                _refreshing = true;
                var tmpCur = Cursor;
                Cursor = Cursors.Wait;

                try
                {
                    SetDataContext();
                }
                catch { }

                _refreshing = false;
                Cursor = tmpCur;
            }
        }
    }
}
