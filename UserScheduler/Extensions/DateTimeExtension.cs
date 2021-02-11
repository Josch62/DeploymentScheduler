using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserScheduler.Extensions
{
    public static class DateTimeExtension
    {
        public static DateTime DropSeconds(this DateTime dt)
        {
            var tempDt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
            return RoundUp(tempDt.AddMinutes(-5));
        }

        private static DateTime RoundUp(DateTime date)
        {
            return new DateTime(date.Ticks - (date.Ticks % (TimeSpan.TicksPerMinute * 5))).AddMinutes(5);
        }
    }
}
