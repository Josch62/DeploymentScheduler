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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ConfigurationEditor.Logging;
using ConfigurationEditor.Properties;
using ConfigurationEditor.Windows;
using Microsoft.ConfigurationManagement.AdminConsole.Views.Common;
using Microsoft.Win32;
using SchedulerSettings;

namespace ConfigurationEditor
{
    /// <summary>
    /// Interaction logic for ConfigurationControl.xaml.
    /// </summary>
    public partial class ConfigurationControl : UserControl, IComponentConnector
    {
        private string _fileName;

        public ConfigurationControl()
            : this(null)
        {
        }

        public ConfigurationControl(OverviewControllerBase overviewNodeViewController)
        {
            InitializeComponent();
            Globals.AttachedController = overviewNodeViewController;
            Logger.Start();
            Logger.Log("ConfigurationControl was loaded into console", LogType.Info);
        }

        private void BtOpen_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog
            {
                Filter = "Settings files (*.xml)|*.xml",
            };

            if (d.ShowDialog() == true)
            {
                try
                {
                    Globals.Settings = SettingsUtils.GetSettingsFromFile(d.FileName);
                    DataContext = Globals.Settings;
                    _fileName = d.FileName;

                    FileExclusions.Clear();
                    var filetext = string.Empty;

                    foreach (var file in Globals.Settings.RestartChecks.PendingFileNameExclusions)
                    {
                        if (!string.IsNullOrEmpty(file))
                        {
                            filetext += file + Environment.NewLine;
                        }
                    }

                    if (!string.IsNullOrEmpty(filetext))
                    {
                        FileExclusions.Text = filetext.TrimEnd(Environment.NewLine.ToCharArray());
                    }

                    Logger.Log($"User '{Environment.UserName}' Opened settings file '{_fileName}'", LogType.Info);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to load file '{_fileName}', Exception: '{ex.Message}'", LogType.Error);
                }
            }
        }

        private void BtSave_Click(object sender, RoutedEventArgs e)
        {
            var d = new SaveFileDialog
            {
                FileName = string.IsNullOrEmpty(_fileName) ? "Settings.xml" : _fileName,
            };

            if (!d.ShowDialog() == true)
            {
                return;
            }

            Globals.Settings.RestartChecks.PendingFileNameExclusions.Clear();

            var lines = GetLinesFromTextBox(FileExclusions);

            Globals.Settings.RestartChecks.PendingFileNameExclusions.AddRange(lines);

            SettingsUtils.WriteSettingsToFile(d.FileName, Globals.Settings);

            Logger.Log($"User '{Environment.UserName}' Saved settings file '{d.FileName}'", LogType.Info);
        }

        private void BtNew_Click(object sender, RoutedEventArgs e)
        {
            FileExclusions.Clear();
            DataContext = null;
            Globals.Settings = new SchedulerSettings.Settings();
            DataContext = Globals.Settings;

            Logger.Log($"User '{Environment.UserName}' Loaded default settings.", LogType.Info);
        }

        private List<string> GetLinesFromTextBox(TextBox textBox)
        {
            var lines = new List<string>();
            var lineCount = textBox.LineCount;

            if (lineCount == -1)
            {
                return lines;
            }

            for (var line = 0; line < lineCount; line++)
            {
                var temp = textBox.GetLineText(line);

                if (!string.IsNullOrEmpty(temp))
                {
                    lines.Add(temp.TrimEnd(Environment.NewLine.ToCharArray()));
                }
            }

            return lines;
        }

        private void BtDeploy_Click(object sender, RoutedEventArgs e)
        {
            var dw = new Deploy
            {
                ShowActivated = true,
                Owner = Application.Current.MainWindow,
            };

            dw.ShowDialog();
        }

        private void BtLoad_Click(object sender, RoutedEventArgs e)
        {
            var lw = new Load
            {
                ShowActivated = true,
                Owner = Application.Current.MainWindow,
            };

            lw.SettingsLoaded += Lw_SettingsLoaded;

            lw.ShowDialog();
        }

        private void Lw_SettingsLoaded(object sender, EventArguments.LoadEventArg e)
        {
            var obj = SettingsUtils.StringToSettings<SchedulerSettings.Settings>(e.Settings);

            DataContext = null;
            Globals.Settings = obj;
            DataContext = Globals.Settings;

            FileExclusions.Clear();
            var filetext = string.Empty;

            foreach (var file in Globals.Settings.RestartChecks.PendingFileNameExclusions)
            {
                if (!string.IsNullOrEmpty(file))
                {
                    filetext += file + Environment.NewLine;
                }
            }

            if (!string.IsNullOrEmpty(filetext))
            {
                FileExclusions.Text = filetext.TrimEnd(Environment.NewLine.ToCharArray());
            }
        }

        private void TabTitle_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (sender is TextBox tb)
                {
                    if (tb.Text.Length > 14)
                    {
                        tb.Text = tb.Text.Substring(0, 14);
                    }
                    else if (tb.Text.Length < 1)
                    {
                        tb.Focus();
                        tb.Background = new SolidColorBrush(Colors.LightSalmon);
                        BtDeploy.IsEnabled = false;
                    }
                    else
                    {
                        tb.Background = new SolidColorBrush(Colors.Transparent);
                        BtDeploy.IsEnabled = true;
                    }
                }
            }
            catch { }
        }
    }
}
