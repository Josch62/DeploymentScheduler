using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SchedulerCommon.Ccm;
using SchedulerCommon.Logging;
using SchedulerCommon.Sql;

namespace SchedulerCommon.Common
{
    public static class CommonUtils
    {
        private static readonly bool _is24HourEnvironement = DateTimeFormatInfo.CurrentInfo.ShortTimePattern.Contains("H");
        private static readonly ServiceEventSource _log = ServiceEventSource.Log;

        public static DateTime? GetNextServiceCycleAsDateTime()
        {
            var retDt = (DateTime?)DateTime.MinValue;

            var nextAutoTime = GetNextAutoSchedulesAsDateTime();

            var nextServiceCycle = SqlCe.GetServiceSchedule();
            var dtNextServiceCycle = nextServiceCycle != null ? nextServiceCycle.ExecuteTime : DateTime.MaxValue;
            retDt = nextAutoTime < dtNextServiceCycle ? nextAutoTime : dtNextServiceCycle;

            return retDt == DateTime.MaxValue ? null : retDt;
        }

        public static DateTime GetNextAutoSchedulesAsDateTime()
        {
            var workList = new List<DateTime>();
            List<AutoUpdateSchedule> autoSchedules = null;
            var jsonAutoSchedules = SqlCe.GetAutoEnforceSchedules();

            if (!string.IsNullOrEmpty(jsonAutoSchedules))
            {
                autoSchedules = JsonConvert.DeserializeObject<List<AutoUpdateSchedule>>(jsonAutoSchedules).OrderBy(x => x.DayOfWeek).ToList();

                if (!autoSchedules.Where(x => x.IsActive).Any())
                {
                    return DateTime.MaxValue;
                }
            }
            else
            {
                return DateTime.MaxValue;
            }

            var now = DateTime.Now;
            var day = (int)now.DayOfWeek;
            var thisweek = Convert.ToDateTime(now.AddDays(-day).ToString("yyyy-MM-dd"));

            foreach (var autoSchedule in autoSchedules)
            {
                if (!autoSchedule.IsActive)
                {
                    continue;
                }

                try
                {
                    var tempdate = thisweek.AddDays(autoSchedule.DayOfWeek);
                    var aus = _is24HourEnvironement ? $"{tempdate:yyyy-MM-dd} {autoSchedule.Hour}:{autoSchedule.Minute}" : $"{tempdate:yyyy-MM-dd} {autoSchedule.Hour}:{autoSchedule.Minute} {autoSchedule.AmPm}";
                    var dt = Convert.ToDateTime(aus);

                    if (dt < now.AddSeconds(-10))
                    {
                        dt = dt.AddDays(7);
                    }

                    workList.Add(dt);
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message);
                }
            }

            return workList.OrderBy(x => x).First();
        }

        public static List<DateTime> GetAutoSchedulesAsDtList()
        {
            var workList = new List<DateTime>();
            List<AutoUpdateSchedule> autoSchedules = null;
            var jsonAutoSchedules = SqlCe.GetAutoEnforceSchedules();

            if (!string.IsNullOrEmpty(jsonAutoSchedules))
            {
                autoSchedules = JsonConvert.DeserializeObject<List<AutoUpdateSchedule>>(jsonAutoSchedules).OrderBy(x => x.DayOfWeek).ToList();

                if (!autoSchedules.Where(x => x.IsActive).Any())
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

            var now = DateTime.Now;
            var day = (int)now.DayOfWeek;
            var thisweek = Convert.ToDateTime(now.AddDays(-day).ToString("yyyy-MM-dd"));

            foreach (var autoSchedule in autoSchedules)
            {
                if (!autoSchedule.IsActive)
                {
                    continue;
                }

                try
                {
                    var tempdate = thisweek.AddDays(autoSchedule.DayOfWeek);
                    var aus = _is24HourEnvironement ? $"{tempdate:yyyy-MM-dd} {autoSchedule.Hour}:{autoSchedule.Minute}" : $"{tempdate:yyyy-MM-dd} {autoSchedule.Hour}:{autoSchedule.Minute} {autoSchedule.AmPm}";
                    var dt = Convert.ToDateTime(aus);

                    if (dt < now.AddSeconds(-10))
                    {
                        dt = dt.AddDays(7);
                    }

                    workList.Add(dt);
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message);
                }
            }

            return workList;
        }
    }
}
