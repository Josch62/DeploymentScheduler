using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using SchedulerCommon.Sql;

namespace UserScheduler.UserControls
{
    /// <summary>
    /// Interaction logic for AutoDeployControl.xaml
    /// </summary>
    public partial class AutoDeployControl : UserControl
    {
        private readonly List<string> _l24hours = new List<string> { "00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23" };
        private readonly List<string> _l12hours = new List<string> { "12", "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11" };
        private readonly List<string> _minutes = new List<string> { "00", "05", "10", "15", "20", "25", "30", "35", "40", "45", "50", "55" };
        private readonly List<string> _ampm = new List<string> { "AM", "PM" };
        private readonly bool _is24HourEnvironement = DateTimeFormatInfo.CurrentInfo.ShortTimePattern.Contains("H");

        public AutoDeployControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = Globals.Settings.PlannerSettings;

            var hours = _is24HourEnvironement ? _l24hours : _l12hours;

            HourMo.ItemsSource = hours;
            HourTu.ItemsSource = hours;
            HourWe.ItemsSource = hours;
            HourTh.ItemsSource = hours;
            HourFr.ItemsSource = hours;
            HourSa.ItemsSource = hours;
            HourSu.ItemsSource = hours;

            MinuteMo.ItemsSource = _minutes;
            MinuteTu.ItemsSource = _minutes;
            MinuteWe.ItemsSource = _minutes;
            MinuteTh.ItemsSource = _minutes;
            MinuteFr.ItemsSource = _minutes;
            MinuteSa.ItemsSource = _minutes;
            MinuteSu.ItemsSource = _minutes;

            AmPmMo.ItemsSource = _ampm;
            AmPmTu.ItemsSource = _ampm;
            AmPmWe.ItemsSource = _ampm;
            AmPmTh.ItemsSource = _ampm;
            AmPmFr.ItemsSource = _ampm;
            AmPmSa.ItemsSource = _ampm;
            AmPmSu.ItemsSource = _ampm;

            HourMo.SelectedIndex = 0;
            HourTu.SelectedIndex = 0;
            HourWe.SelectedIndex = 0;
            HourTh.SelectedIndex = 0;
            HourFr.SelectedIndex = 0;
            HourSa.SelectedIndex = 0;
            HourSu.SelectedIndex = 0;

            MinuteMo.SelectedIndex = 0;
            MinuteTu.SelectedIndex = 0;
            MinuteWe.SelectedIndex = 0;
            MinuteTh.SelectedIndex = 0;
            MinuteFr.SelectedIndex = 0;
            MinuteSa.SelectedIndex = 0;
            MinuteSu.SelectedIndex = 0;

            AmPmMo.SelectedIndex = 0;
            AmPmTu.SelectedIndex = 0;
            AmPmWe.SelectedIndex = 0;
            AmPmTh.SelectedIndex = 0;
            AmPmFr.SelectedIndex = 0;
            AmPmSa.SelectedIndex = 0;
            AmPmSu.SelectedIndex = 0;

            AmPmMo.Visibility = _is24HourEnvironement ? Visibility.Hidden : Visibility.Visible;
            AmPmTu.Visibility = _is24HourEnvironement ? Visibility.Hidden : Visibility.Visible;
            AmPmWe.Visibility = _is24HourEnvironement ? Visibility.Hidden : Visibility.Visible;
            AmPmTh.Visibility = _is24HourEnvironement ? Visibility.Hidden : Visibility.Visible;
            AmPmFr.Visibility = _is24HourEnvironement ? Visibility.Hidden : Visibility.Visible;
            AmPmSa.Visibility = _is24HourEnvironement ? Visibility.Hidden : Visibility.Visible;
            AmPmSu.Visibility = _is24HourEnvironement ? Visibility.Hidden : Visibility.Visible;

            var json = SqlCe.GetAutoEnforceSchedules();

            if (!string.IsNullOrEmpty(json))
            {
                SetSchedules(json);
            }
        }

        private void BtSchedule_Click(object sender, RoutedEventArgs e)
        {
            var schedulesList = BuildSchedule();
            var json = JsonConvert.SerializeObject(schedulesList);
            SqlCe.SetAutoEnforceSchedules(json);
        }

        private void Hyperlink_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private List<AutoUpdateSchedule> BuildSchedule()
        {
            return new List<AutoUpdateSchedule>
            {
                new AutoUpdateSchedule { IsActive = (bool)CbSunday.IsChecked, DayOfWeek = 0, Hour = HourSu.Text, Minute = MinuteSu.Text, AmPm = AmPmSu.Text },
                new AutoUpdateSchedule { IsActive = (bool)CbMonday.IsChecked, DayOfWeek = 1, Hour = HourMo.Text, Minute = MinuteMo.Text, AmPm = AmPmMo.Text },
                new AutoUpdateSchedule { IsActive = (bool)CbTuesday.IsChecked, DayOfWeek = 2, Hour = HourTu.Text, Minute = MinuteTu.Text, AmPm = AmPmTu.Text },
                new AutoUpdateSchedule { IsActive = (bool)CbWednesday.IsChecked, DayOfWeek = 3, Hour = HourWe.Text, Minute = MinuteWe.Text, AmPm = AmPmWe.Text },
                new AutoUpdateSchedule { IsActive = (bool)CbThursday.IsChecked, DayOfWeek = 4, Hour = HourTh.Text, Minute = MinuteTh.Text, AmPm = AmPmTh.Text },
                new AutoUpdateSchedule { IsActive = (bool)CbFriday.IsChecked, DayOfWeek = 5, Hour = HourFr.Text, Minute = MinuteFr.Text, AmPm = AmPmFr.Text },
                new AutoUpdateSchedule { IsActive = (bool)CbSaturday.IsChecked, DayOfWeek = 6, Hour = HourSa.Text, Minute = MinuteSa.Text, AmPm = AmPmSa.Text },
            };
        }

        private void SetSchedules(string json)
        {
            var list = JsonConvert.DeserializeObject<List<AutoUpdateSchedule>>(json);

            foreach (var day in list)
            {
                switch (day.DayOfWeek)
                {
                    case 0:
                        CbSunday.IsChecked = day.IsActive;
                        HourSu.SelectedItem = day.Hour;
                        MinuteSu.SelectedItem = day.Minute;
                        AmPmSu.SelectedItem = day.AmPm;
                        break;

                    case 1:
                        CbMonday.IsChecked = day.IsActive;
                        HourMo.SelectedItem = day.Hour;
                        MinuteMo.SelectedItem = day.Minute;
                        AmPmMo.SelectedItem = day.AmPm;
                        break;

                    case 2:
                        CbTuesday.IsChecked = day.IsActive;
                        HourTu.SelectedItem = day.Hour;
                        MinuteTu.SelectedItem = day.Minute;
                        AmPmTu.SelectedItem = day.AmPm;
                        break;

                    case 3:
                        CbWednesday.IsChecked = day.IsActive;
                        HourWe.SelectedItem = day.Hour;
                        MinuteWe.SelectedItem = day.Minute;
                        AmPmWe.SelectedItem = day.AmPm;
                        break;

                    case 4:
                        CbThursday.IsChecked = day.IsActive;
                        HourTh.SelectedItem = day.Hour;
                        MinuteTh.SelectedItem = day.Minute;
                        AmPmTh.SelectedItem = day.AmPm;
                        break;

                    case 5:
                        CbFriday.IsChecked = day.IsActive;
                        HourFr.SelectedItem = day.Hour;
                        MinuteFr.SelectedItem = day.Minute;
                        AmPmFr.SelectedItem = day.AmPm;
                        break;

                    case 6:
                        CbSaturday.IsChecked = day.IsActive;
                        HourSa.SelectedItem = day.Hour;
                        MinuteSa.SelectedItem = day.Minute;
                        AmPmSa.SelectedItem = day.AmPm;
                        break;
                }
            }
        }
    }
}
