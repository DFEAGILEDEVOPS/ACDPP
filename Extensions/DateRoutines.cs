using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Extensions
{
    /// <summary>
    ///     Miscellaneous and parsing methods for DateTime
    /// </summary>
    public static class DateTimeRoutines
    {
        // Summary:
        //     Indicates how to determine and format date intervals when calling date-related
        //     functions.
        public enum DateInterval
        {
            // Summary:
            //     Year
            Year = 0,
            //
            // Summary:
            //     Quarter of year (1 through 4)
            Quarter = 1,
            //
            // Summary:
            //     Month (1 through 12)
            Month = 2,
            //
            // Summary:
            //     Day of year (1 through 366)
            DayOfYear = 3,
            //
            // Summary:
            //     Day of month (1 through 31)
            Day = 4,
            //
            // Summary:
            //     Week of year (1 through 53)
            WeekOfYear = 5,
            //
            // Summary:
            //     Day of week (1 through 7)
            Weekday = 6,
            //
            // Summary:
            //     Hour (0 through 23)
            Hour = 7,
            //
            // Summary:
            //     Minute (0 through 59)
            Minute = 8,
            //
            // Summary:
            //     Second (0 through 59)
            Second = 9
        }

        public static string ToFriendly(this TimeSpan interval, string zeroText = null, int maxParts = 4)
        {
            if (interval <= TimeSpan.Zero) return zeroText;

            string result = null;
            var parts = 0;
            if (interval.Days > 0)
            {
                result += interval.Days;
                result += " day" + (interval.Days > 1 ? "s" : "");
                parts++;
            }

            if (interval.Hours > 0 && parts < maxParts)
            {
                parts++;
                if (result != null) result += parts == maxParts ? " and " : ", ";
                result += interval.Hours;
                result += " hour" + (interval.Hours > 1 ? "s" : "");
            }

            if (interval.Minutes > 0 && parts < maxParts)
            {
                parts++;
                if (result != null) result += parts == maxParts ? " and " : ", ";
                result += interval.Minutes;
                result += " minute" + (interval.Minutes > 1 ? "s" : "");
            }

            if (interval.Days == 0 && interval.Hours == 0 && interval.Seconds > 0 && parts < maxParts)
            {
                parts++;
                if (result != null) result += parts == maxParts ? " and " : ", ";
                result += interval.Seconds;
                result += " second" + (interval.Seconds > 1 ? "s" : "");
            }
            return result;
        }

        public static string ToSmallDateTime(this DateTime dateTime)
        {
            return dateTime.ToString(Time.ShortDateFormat);
        }
        public static DateTime? ToNullable(this DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue) return null;
            return dateTime;
        }

        public static DateTime FromSmallDateTime(this string shortDateTime, bool defaultMaxDateTime = false)
        {
            DateTime dateTime;
            if (DateTime.TryParseExact(shortDateTime, Time.ShortDateFormat, null, DateTimeStyles.AssumeLocal, out dateTime))
                return dateTime;
            return defaultMaxDateTime ? DateTime.MaxValue : DateTime.MinValue;
        }

        public static string ToSmallerDateTime(this DateTime dateTime)
        {
            return dateTime.ToString(Time.ShorterDateFormat);
        }

        public static DateTime FromSmallerDateTime(this string shorterDateTime)
        {
            DateTime dateTime;
            if (DateTime.TryParseExact(shorterDateTime, Time.ShorterDateFormat, null, DateTimeStyles.AssumeLocal, out dateTime))
                return dateTime;
            return DateTime.MinValue;
        }

        public static DateTime FromShortestDateTime(this string shorterDateTime)
        {
            DateTime dateTime;
            if (DateTime.TryParseExact(shorterDateTime, Time.ShortestDateFormat, null, DateTimeStyles.AssumeLocal, out dateTime))
                return dateTime;
            return DateTime.MinValue;
        }

        public static double ToTimestamp(this DateTime date)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            var diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds);
        }

        public static string GetRemainingTime(this TimeSpan interval, DateTime start, DateTime? end = null)
        {
            if (end == null) end = DateTime.Now;

            var expires = start.AddTicks(interval.Ticks);
            var remaining = expires - end.Value;
            string remainingString = null;
            if (remaining.Days > 0)
            {
                remainingString = remaining.Days.ToStringWord() + " day" + (remaining.Hours > 1 ? "s" : "");
                if (remaining.Hours > 0)
                {
                    remainingString += " and " + remaining.Hours.ToStringWord() + " hour" + (remaining.Hours > 1 ? "s" : "");
                }
            }
            else if (remaining.Hours > 0)
            {
                remainingString = remaining.Hours.ToStringWord() + " hour" + (remaining.Hours > 1 ? "s" : "");
                if (remaining.Minutes > 0)
                {
                    remainingString += " and " + remaining.Minutes.ToStringWord() + " minute" + (remaining.Minutes > 1 ? "s" : "");
                }
            }
            else if (remaining.Minutes > 0)
            {
                remainingString = remaining.Minutes.ToStringWord() + " minute" + (remaining.Minutes > 1 ? "s" : "");
                if (remaining.Seconds > 0)
                {
                    remainingString += " and " + remaining.Seconds.ToStringWord() + " second" + (remaining.Seconds > 1 ? "s" : "");
                }
            }

            return remainingString;
        }

        #region miscellaneous methods

        public static int GetYears(this TimeSpan timespan)
        {
            return (int) (timespan.Days/365.2425);
        }

        public static int GetMonths(this TimeSpan timespan)
        {
            return (int) (timespan.Days/30.436875);
        }

        /// <summary>
        ///     Amount of seconds elapsed between 1970-01-01 00:00:00 and the date-time.
        /// </summary>
        /// <param name="date_time">date-time</param>
        /// <returns>seconds</returns>
        public static uint GetSecondsSinceUnixEpoch(this DateTime date_time)
        {
            var t = date_time - new DateTime(1970, 1, 1);
            var ss = (int) t.TotalSeconds;
            if (ss < 0)
                return 0;
            return (uint) ss;
        }

        /// <summary>
        ///     Compares two dates for exact or near equality
        /// </summary>
        /// <param name="source">The source date</param>
        /// <param name="target">The target date</param>
        /// <param name="accuracy">The units to use for calculating accuracy (Second, Minute[Default], Hour, Day)</param>
        /// <param name="variance">
        ///     0 for exact comparison, or a positive integer of maximum number of accuracy units permitted in
        ///     difference
        /// </param>
        /// <returns></returns>
        public static bool Near(this DateTime source, DateTime target, DateInterval accuracy = DateInterval.Minute, double variance = 0)
        {
            var difference = source - target;
            double diff = 0;
            switch (accuracy)
            {
                case DateInterval.Second:
                    diff = difference.TotalSeconds;
                    break;
                case DateInterval.Minute:
                    diff = difference.TotalMinutes;
                    break;
                case DateInterval.Hour:
                    diff = difference.TotalHours;
                    break;
                case DateInterval.Day:
                    diff = difference.TotalDays;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("accuracy");
            }
            if (variance == 0) return diff == 0;
            return Math.Abs(diff) <= Math.Abs(variance);
        }

        #endregion

        #region parsing definitions

        /// <summary>
        ///     Defines a substring where date-time was found and result of conversion
        /// </summary>
        public class ParsedDateTime
        {
            /// <summary>
            ///     DateTime found in the string
            /// </summary>
            public readonly DateTime DateTime;

            /// <summary>
            ///     Index of first char of a date substring found in the string
            /// </summary>
            public readonly int IndexOfDate = -1;

            /// <summary>
            ///     Index of first char of a time substring found in the string
            /// </summary>
            public readonly int IndexOfTime = -1;

            /// <summary>
            ///     True if a date was found within the string
            /// </summary>
            public readonly bool IsDateFound;

            /// <summary>
            ///     True if a time was found within the string
            /// </summary>
            public readonly bool IsTimeFound;

            /// <summary>
            ///     Length a date substring found in the string
            /// </summary>
            public readonly int LengthOfDate = -1;

            /// <summary>
            ///     Length of a time substring found in the string
            /// </summary>
            public readonly int LengthOfTime = -1;

            public ParsedDateTime(int index_of_date, int length_of_date, int index_of_time, int length_of_time, DateTime date_time)
            {
                IndexOfDate = index_of_date;
                LengthOfDate = length_of_date;
                IndexOfTime = index_of_time;
                LengthOfTime = length_of_time;
                DateTime = date_time;
                IsDateFound = index_of_date > -1;
                IsTimeFound = index_of_time > -1;
            }
        }

        /// <summary>
        ///     Date that is accepted in the following cases:
        ///     - no date was parsed by TryParseDateOrTime();
        ///     - no year was found by TryParseDate();
        ///     It is ignored if DefaultDateIsNow = true was set after DefaultDate
        /// </summary>
        public static DateTime DefaultDate
        {
            set
            {
                _DefaultDate = value;
                DefaultDateIsNow = false;
            }
            get
            {
                if (DefaultDateIsNow)
                    return DateTime.Now;
                return _DefaultDate;
            }
        }

        private static DateTime _DefaultDate = DateTime.Now;

        /// <summary>
        ///     If true then DefaultDate property is ignored and DefaultDate is always DateTime.Now
        /// </summary>
        public static bool DefaultDateIsNow = true;

        /// <summary>
        ///     Defines default date-time format.
        /// </summary>
        public enum DateTimeFormat
        {
            /// <summary>
            ///     month number goes before day number
            /// </summary>
            USA_DATE,

            /// <summary>
            ///     day number goes before month number
            /// </summary>
            UK_DATE
            ///// <summary>
            ///// time is specifed through AM or PM
            ///// </summary>
            //USA_TIME,
        }

        #endregion

        #region parsing derived methods for DateTime output

        /// <summary>
        ///     Tries to find date and time within the passed string and return it as DateTime structure.
        /// </summary>
        /// <param name="str">string that contains date and/or time</param>
        /// <param name="default_format">format to be used preferably in ambivalent instances</param>
        /// <param name="date_time">parsed date-time output</param>
        /// <returns>true if both date and time were found, else false</returns>
        public static bool TryParseDateTime(this string str, DateTimeFormat default_format, out DateTime date_time)
        {
            ParsedDateTime parsed_date_time;
            if (!TryParseDateTime(str, default_format, out parsed_date_time))
            {
                date_time = new DateTime(1, 1, 1);
                return false;
            }
            date_time = parsed_date_time.DateTime;
            return true;
        }

        /// <summary>
        ///     Tries to find date and/or time within the passed string and return it as DateTime structure.
        ///     If only date was found, time in the returned DateTime is always 0:0:0.
        ///     If only time was found, date in the returned DateTime is DefaultDate.
        /// </summary>
        /// <param name="str">string that contains date and(or) time</param>
        /// <param name="default_format">format to be used preferably in ambivalent instances</param>
        /// <param name="date_time">parsed date-time output</param>
        /// <returns>true if date and/or time was found, else false</returns>
        public static bool TryParseDateOrTime(this string str, DateTimeFormat default_format, out DateTime date_time)
        {
            ParsedDateTime parsed_date_time;
            if (!TryParseDateOrTime(str, default_format, out parsed_date_time))
            {
                date_time = new DateTime(1, 1, 1);
                return false;
            }
            date_time = parsed_date_time.DateTime;
            return true;
        }

        /// <summary>
        ///     Tries to find time within the passed string and return it as DateTime structure.
        ///     It recognizes only time while ignoring date, so date in the returned DateTime is always 1/1/1.
        /// </summary>
        /// <param name="str">string that contains time</param>
        /// <param name="default_format">format to be used preferably in ambivalent instances</param>
        /// <param name="time">parsed time output</param>
        /// <returns>true if time was found, else false</returns>
        public static bool TryParseTime(this string str, DateTimeFormat default_format, out DateTime time)
        {
            ParsedDateTime parsed_time;
            if (!TryParseTime(str, default_format, out parsed_time, null))
            {
                time = new DateTime(1, 1, 1);
                return false;
            }
            time = parsed_time.DateTime;
            return true;
        }

        /// <summary>
        ///     Tries to find date within the passed string and return it as DateTime structure.
        ///     It recognizes only date while ignoring time, so time in the returned DateTime is always 0:0:0.
        ///     If year of the date was not found then it accepts the current year.
        /// </summary>
        /// <param name="str">string that contains date</param>
        /// <param name="default_format">format to be used preferably in ambivalent instances</param>
        /// <param name="date">parsed date output</param>
        /// <returns>true if date was found, else false</returns>
        public static bool TryParseDate(this string str, DateTimeFormat default_format, out DateTime date)
        {
            ParsedDateTime parsed_date;
            if (!TryParseDate(str, default_format, out parsed_date))
            {
                date = new DateTime(1, 1, 1);
                return false;
            }
            date = parsed_date.DateTime;
            return true;
        }

        #endregion

        #region parsing derived methods for ParsedDateTime output

        /// <summary>
        ///     Tries to find date and time within the passed string and return it as ParsedDateTime object.
        /// </summary>
        /// <param name="str">string that contains date-time</param>
        /// <param name="default_format">format to be used preferably in ambivalent instances</param>
        /// <param name="parsed_date_time">parsed date-time output</param>
        /// <returns>true if both date and time were found, else false</returns>
        public static bool TryParseDateTime(this string str, DateTimeFormat default_format, out ParsedDateTime parsed_date_time)
        {
            if (str.TryParseDateOrTime(default_format, out parsed_date_time)
                && parsed_date_time.IsDateFound
                && parsed_date_time.IsTimeFound
                )
                return true;

            parsed_date_time = null;
            return false;
        }

        /// <summary>
        ///     Tries to find time within the passed string and return it as ParsedDateTime object.
        ///     It recognizes only time while ignoring date, so date in the returned ParsedDateTime is always 1/1/1
        /// </summary>
        /// <param name="str">string that contains date-time</param>
        /// <param name="default_format">format to be used preferably in ambivalent instances</param>
        /// <param name="parsed_time">parsed date-time output</param>
        /// <returns>true if time was found, else false</returns>
        public static bool TryParseTime(this string str, DateTimeFormat default_format, out ParsedDateTime parsed_time)
        {
            return TryParseTime(str, default_format, out parsed_time, null);
        }

        /// <summary>
        ///     Tries to find date and/or time within the passed string and return it as ParsedDateTime object.
        ///     If only date was found, time in the returned ParsedDateTime is always 0:0:0.
        ///     If only time was found, date in the returned ParsedDateTime is DefaultDate.
        /// </summary>
        /// <param name="str">string that contains date-time</param>
        /// <param name="default_format">format to be used preferably in ambivalent instances</param>
        /// <param name="parsed_date_time">parsed date-time output</param>
        /// <returns>true if date or time was found, else false</returns>
        public static bool TryParseDateOrTime(this string str, DateTimeFormat default_format, out ParsedDateTime parsed_date_time)
        {
            parsed_date_time = null;

            ParsedDateTime parsed_date;
            ParsedDateTime parsed_time;
            if (!TryParseDate(str, default_format, out parsed_date))
            {
                if (!TryParseTime(str, default_format, out parsed_time, null))
                    return false;

                var date_time = new DateTime(DefaultDate.Year, DefaultDate.Month, DefaultDate.Day, parsed_time.DateTime.Hour, parsed_time.DateTime.Minute, parsed_time.DateTime.Second);
                parsed_date_time = new ParsedDateTime(-1, -1, parsed_time.IndexOfTime, parsed_time.LengthOfTime, date_time);
            }
            else
            {
                if (!TryParseTime(str, default_format, out parsed_time, parsed_date))
                {
                    var date_time = new DateTime(parsed_date.DateTime.Year, parsed_date.DateTime.Month, parsed_date.DateTime.Day, 0, 0, 0);
                    parsed_date_time = new ParsedDateTime(parsed_date.IndexOfDate, parsed_date.LengthOfDate, -1, -1, date_time);
                }
                else
                {
                    var date_time = new DateTime(parsed_date.DateTime.Year, parsed_date.DateTime.Month, parsed_date.DateTime.Day, parsed_time.DateTime.Hour, parsed_time.DateTime.Minute, parsed_time.DateTime.Second);
                    parsed_date_time = new ParsedDateTime(parsed_date.IndexOfDate, parsed_date.LengthOfDate, parsed_time.IndexOfTime, parsed_time.LengthOfTime, date_time);
                }
            }

            return true;
        }

        #endregion

        #region parsing base methods

        /// <summary>
        ///     Tries to find time within the passed string (relatively to the passed parsed_date if any) and return it as
        ///     ParsedDateTime object.
        ///     It recognizes only time while ignoring date, so date in the returned ParsedDateTime is always 1/1/1
        /// </summary>
        /// <param name="str">string that contains date</param>
        /// <param name="default_format">format to be used preferably in ambivalent instances</param>
        /// <param name="parsed_time">parsed date-time output</param>
        /// <param name="parsed_date">ParsedDateTime object if the date was found within this string, else NULL</param>
        /// <returns>true if time was found, else false</returns>
        [DebuggerStepThrough]
        public static bool TryParseTime(this string str, DateTimeFormat default_format, out ParsedDateTime parsed_time, ParsedDateTime parsed_date)
        {
            parsed_time = null;

            Match m;
            if (parsed_date != null && parsed_date.IndexOfDate > -1)
            {
//look around the found date
                //look for <date> [h]h:mm[:ss] [PM/AM]
                m = Regex.Match(str.Substring(parsed_date.IndexOfDate + parsed_date.LengthOfDate), @"(?<=^\s*,?\s+|^\s*at\s*|^\s*[T\-]\s*)(?'hour'\d{1,2})\s*:\s*(?'minute'\d{2})\s*(?::\s*(?'second'\d{2}))?(?:\s*(?'ampm'AM|am|PM|pm))?(?=$|[^\d\w])", RegexOptions.Compiled);
                if (!m.Success)
                    //look for [h]h:mm:ss <date>
                    m = Regex.Match(str.Substring(0, parsed_date.IndexOfDate), @"(?<=^|[^\d])(?'hour'\d{1,2})\s*:\s*(?'minute'\d{2})\s*(?::\s*(?'second'\d{2}))?(?:\s*(?'ampm'AM|am|PM|pm))?(?=$|[\s,]+)", RegexOptions.Compiled);
            }
            else //look anywere within string
            //look for [h]h:mm[:ss] [PM/AM]
                m = Regex.Match(str, @"(?<=^|\s+|\s*T\s*)(?'hour'\d{1,2})\s*:\s*(?'minute'\d{2})\s*(?::\s*(?'second'\d{2}))?(?:\s*(?'ampm'AM|am|PM|pm))?(?=$|[^\d\w])", RegexOptions.Compiled);

            if (m.Success)
            {
                try
                {
                    var hour = int.Parse(m.Groups["hour"].Value);
                    if (hour < 0 || hour > 23)
                        return false;

                    var minute = int.Parse(m.Groups["minute"].Value);
                    if (minute < 0 || minute > 59)
                        return false;

                    var second = 0;
                    if (!string.IsNullOrEmpty(m.Groups["second"].Value))
                    {
                        second = int.Parse(m.Groups["second"].Value);
                        if (second < 0 || second > 59)
                            return false;
                    }

                    if (string.Compare(m.Groups["ampm"].Value, "PM", true) == 0 && hour < 12)
                        hour += 12;
                    else if (string.Compare(m.Groups["ampm"].Value, "AM", true) == 0 && hour == 12)
                        hour -= 12;

                    var date_time = new DateTime(1, 1, 1, hour, minute, second);
                    parsed_time = new ParsedDateTime(-1, -1, m.Index, m.Length, date_time);
                }
                catch
                {
                    return false;
                }
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Tries to find date within the passed string and return it as ParsedDateTime object.
        ///     It recognizes only date while ignoring time, so time in the returned ParsedDateTime is always 0:0:0.
        ///     If year of the date was not found then it accepts the current year.
        /// </summary>
        /// <param name="str">string that contains date</param>
        /// <param name="default_format">format to be used preferably in ambivalent instances</param>
        /// <param name="parsed_date">parsed date output</param>
        /// <returns>true if date was found, else false</returns>
        public static bool TryParseDate(this string str, DateTimeFormat default_format, out ParsedDateTime parsed_date)
        {
            parsed_date = null;

            if (string.IsNullOrEmpty(str))
                return false;

            //look for dd/mm/yy
            var m = Regex.Match(str, @"(?<=^|[^\d])(?'day'\d{1,2})\s*(?'separator'[\\/\.])+\s*(?'month'\d{1,2})\s*\'separator'+\s*(?'year'\d{2}|\d{4})(?=$|[^\d])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (m.Success)
            {
                DateTime date;
                if ((default_format ^ DateTimeFormat.USA_DATE) == DateTimeFormat.USA_DATE)
                {
                    if (!convert_to_date(int.Parse(m.Groups["year"].Value), int.Parse(m.Groups["day"].Value), int.Parse(m.Groups["month"].Value), out date))
                        return false;
                }
                else
                {
                    if (!convert_to_date(int.Parse(m.Groups["year"].Value), int.Parse(m.Groups["month"].Value), int.Parse(m.Groups["day"].Value), out date))
                        return false;
                }
                parsed_date = new ParsedDateTime(m.Index, m.Length, -1, -1, date);
                return true;
            }

            //look for [yy]yy-mm-dd
            m = Regex.Match(str, @"(?<=^|[^\d])(?'year'\d{2}|\d{4})\s*(?'separator'[\-])\s*(?'month'\d{1,2})\s*\'separator'+\s*(?'day'\d{1,2})(?=$|[^\d])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (m.Success)
            {
                DateTime date;
                if (!convert_to_date(int.Parse(m.Groups["year"].Value), int.Parse(m.Groups["month"].Value), int.Parse(m.Groups["day"].Value), out date))
                    return false;
                parsed_date = new ParsedDateTime(m.Index, m.Length, -1, -1, date);
                return true;
            }

            //look for month dd yyyy
            m = Regex.Match(str, @"(?:^|[^\d\w])(?'month'Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[uarychilestmbro]*\s+(?'day'\d{1,2})(?:-?st|-?th|-?rd|-?nd)?\s*,?\s*(?'year'\d{4})(?=$|[^\d\w])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (!m.Success)
                //look for dd month [yy]yy
                m = Regex.Match(str, @"(?:^|[^\d\w:])(?'day'\d{1,2})(?:-?st\s+|-?th\s+|-?rd\s+|-?nd\s+|-|\s+)(?'month'Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[uarychilestmbro]*(?:\s*,?\s*|-)'?(?'year'\d{2}|\d{4})(?=$|[^\d\w])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (!m.Success)
                //look for yyyy month dd
                m = Regex.Match(str, @"(?:^|[^\d\w])(?'year'\d{4})\s+(?'month'Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[uarychilestmbro]*\s+(?'day'\d{1,2})(?:-?st|-?th|-?rd|-?nd)?(?=$|[^\d\w])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (!m.Success)
                //look for  month dd [yyyy]
                m = Regex.Match(str, @"(?:^|[^\d\w])(?'month'Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[uarychilestmbro]*\s+(?'day'\d{1,2})(?:-?st|-?th|-?rd|-?nd)?(?:\s*,?\s*(?'year'\d{4}))?(?=$|[^\d\w])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var month = -1;
                var index_of_date = m.Index;
                var length_of_date = m.Length;

                switch (m.Groups["month"].Value)
                {
                    case "Jan":
                    case "JAN":
                        month = 1;
                        break;
                    case "Feb":
                    case "FEB":
                        month = 2;
                        break;
                    case "Mar":
                    case "MAR":
                        month = 3;
                        break;
                    case "Apr":
                    case "APR":
                        month = 4;
                        break;
                    case "May":
                    case "MAY":
                        month = 5;
                        break;
                    case "Jun":
                    case "JUN":
                        month = 6;
                        break;
                    case "Jul":
                        month = 7;
                        break;
                    case "Aug":
                    case "AUG":
                        month = 8;
                        break;
                    case "Sep":
                    case "SEP":
                        month = 9;
                        break;
                    case "Oct":
                    case "OCT":
                        month = 10;
                        break;
                    case "Nov":
                    case "NOV":
                        month = 11;
                        break;
                    case "Dec":
                    case "DEC":
                        month = 12;
                        break;
                }

                int year;
                if (!string.IsNullOrEmpty(m.Groups["year"].Value))
                    year = int.Parse(m.Groups["year"].Value);
                else
                    year = DefaultDate.Year;

                DateTime date;
                if (!convert_to_date(year, month, int.Parse(m.Groups["day"].Value), out date))
                    return false;
                parsed_date = new ParsedDateTime(index_of_date, length_of_date, -1, -1, date);
                return true;
            }

            return false;
        }

        [DebuggerStepThrough]
        private static bool convert_to_date(int year, int month, int day, out DateTime date)
        {
            if (year >= 100)
            {
                if (year < 1000)
                {
                    date = new DateTime(1, 1, 1);
                    return false;
                }
            }
            else if (year > 30)
                year += 1900;
            else
                year += 2000;

            try
            {
                date = new DateTime(year, month, day);
            }
            catch
            {
                date = new DateTime(1, 1, 1);
                return false;
            }
            return true;
        }

        #endregion
    }
}