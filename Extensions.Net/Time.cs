using System;

namespace Extensions
{
    public static class Time
    {
        public const string ShortDateFormat = "yyMMddHHmmss";
        public const string ShorterDateFormat = "yyMMddHHmm";
        public const string ShortestDateFormat = "yyMMdd";

        public static bool Between(this DateTime current, DateTime start, DateTime end, bool inclusive = true)
        {
            if (inclusive) return current >= start && current <= end;
            return current > start && current < end;
        }

        public static int GetTotalMonths(this DateTime startDate, DateTime endDate)
        {
            var result = 0;

            while (endDate.AddMonths(-1) >= startDate)
            {
                result++;
                startDate.AddMonths(1);
            }

            return result;
        }

        public static int GetTotalYears(this DateTime startDate, DateTime endDate)
        {
            var result = 0;

            while (endDate.AddYears(-1) >= startDate)
            {
                result++;
                startDate.AddYears(1);
            }

            return result;
        }

        /// <summary>
        ///     Return the next working hour Mon-Fri 10am-5pm after a provided date
        /// </summary>
        /// <param name="currentDate">The time for which should me moved to a working hour</param>
        /// <param name="randomiseTime">Whether to return a random working time btween 10am-5pm or the earliest i.e 10am exactly</param>
        /// <returns></returns>
        public static DateTime GetNextWorkingTime(this DateTime currentDate, bool randomiseTime = true)
        {
            //Make sure we dont send anything in the past
            if (currentDate < DateTime.Now) currentDate = DateTime.Now;

            //If we have gone past 5pm today then move to next day
            if (currentDate.Hour > 16) currentDate = currentDate.AddDays(1);

            //Make sure we only send on working days
            while (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday)
                currentDate = currentDate.AddDays(1);

            while (currentDate.Hour < 10 || currentDate.Hour > 16)
            {
                if (randomiseTime) //Make sure the hour is between 11am and 5pm
                    currentDate = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, Numeric.Rand(10, 16), Numeric.Rand(0, 59), 0);
                else //Make sure the hour is between 11am exactly at earliest time
                    currentDate = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 10, 0, 0);
            }

            return currentDate;
        }

        public static string ToSimple(this DateTime dateTime, string formatText = "r", string minText = "Never", string maxText = "Forever")
        {
            if (dateTime == DateTime.MinValue && !string.IsNullOrWhiteSpace(minText)) return minText;
            if (dateTime == DateTime.MaxValue && !string.IsNullOrWhiteSpace(maxText)) return maxText;
            return dateTime.ToString(formatText);
        }
    }
}