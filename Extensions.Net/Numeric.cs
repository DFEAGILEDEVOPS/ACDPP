using System;

namespace Extensions
{
    public static class Numeric
    {
        public static Random Random = new Random(DateTime.Now.Millisecond);

        public static int Rand(int Min, int Max)
        {
            var result = Random.Next(Min, Max + 1);
            return result;
        }

        public static double Rand(double Min, double Max)
        {
            var result = Random.NextDouble()*(Max - Min) + Min;
            return result;
        }

        public static bool Between(this decimal num, decimal lower, decimal upper, bool inclusive = true)
        {
            return inclusive
                ? lower <= num && num <= upper
                : lower < num && num < upper;
        }

        public static bool Between(this double num, double lower, double upper, bool inclusive = true)
        {
            return inclusive
                ? lower <= num && num <= upper
                : lower < num && num < upper;
        }

        public static bool Between(this int num, int lower, int upper, bool inclusive = true)
        {
            return inclusive
                ? lower <= num && num <= upper
                : lower < num && num < upper;
        }

        public static bool Between(this long num, long lower, long upper, bool inclusive = true)
        {
            return inclusive
                ? lower <= num && num <= upper
                : lower < num && num < upper;
        }

        public static string ToStringWord(this double value)
        {
            return ((int) value).ToStringWord();
        }

        public static string ToLargeStringWord(this double years)
        {
            if (years < 1) return years.ToString("N");
            if (years == 1) return "1";
            if (years < 1e+6) return years.ToString("N0");
            if (years < 1e+9) return (years/1e+6).ToString("0.#") + " million";
            if (years < 1e+12) return (years/1e+9).ToString("0.#") + " billion";
            if (years < 1e+15) return (years/1e+12).ToString("0.#") + " trillion";
            if (years < 1e+18) return (years/1e+15).ToString("0") + " quadrillion";
            if (years < 1e+21) return (years/1e+18).ToString("0") + " quintillion";
            if (years < 1e+24) return (years/1e+21).ToString("0") + " sextillion";
            if (years < 1e+27) return (years/1e+24).ToString("0") + " septillion";
            if (years < 1e+30) return (years/1e+27).ToString("0") + " octillion";
            if (years < 1e+33) return (years/1e+30).ToString("0") + " nonillion";
            if (years < 1e+36) return (years/1e+33).ToString("0") + " decillion";
            if (years < 1e+39) return (years/1e+36).ToString("0") + " undecillion";
            if (years < 1e+42) return (years/1e+39).ToString("0") + " duodecillion";
            if (years < 1e+45) return (years/1e+42).ToString("0") + " tredecillion";
            if (years < 1e+48) return (years/1e+45).ToString("0") + " quattuordecillion";
            if (years < 1e+51) return (years/1e+48).ToString("0") + " quindecillion";
            if (years < 1e+54) return (years/1e+51).ToString("0") + " sexdecillion";
            if (years < 1e+57) return (years/1e+54).ToString("0") + " septendecillion";
            if (years < 1e+60) return (years/1e+57).ToString("0") + " octodecillion";
            if (years < 1e+63) return (years/1e+60).ToString("0") + " novodecillion";
            if (years < 1e+66) return (years/1e+63).ToString("0") + " vigndecillion";

            return years.ToString("g2");
        }

        public static string ToStringWord(this int value)
        {
            switch (value)
            {
                case 1:
                    return "one";
                case 2:
                    return "two";
                case 3:
                    return "three";
                case 4:
                    return "four";
                case 5:
                    return "five";
                case 6:
                    return "six";
                case 7:
                    return "seven";
                case 8:
                    return "eight";
                case 9:
                    return "nine";
                case 10:
                    return "ten";
                case 11:
                    return "eleven";
                case 12:
                    return "twelve";
                case 13:
                    return "thirteen";
                case 14:
                    return "fourteen";
                case 15:
                    return "fifteen";
                case 16:
                    return "sixteen";
                case 17:
                    return "seventeen";
                case 18:
                    return "eightteen";
                case 19:
                    return "nineteen";
                case 20:
                    return "twenty";
            }
            return value.ToString();
        }

        public static string FormatFileSize(double size, string formatString = null, bool roundDown = true)
        {
            if (roundDown)
            {
                if (size < 1024) return size.ToInt32().ToString(formatString) + " b";
                if (size < 1048576) return (size/1024).ToInt32().ToString(formatString) + " kb";
                return (size/10485.76/100).ToInt32().ToString(formatString) + " mb";
            }
            if (size < 1024) return Math.Round(size).ToString(formatString) + " b";
            if (size < 1048576) return Math.Round(size/1024).ToString(formatString) + " kb";
            return (Math.Round(size/10485.76)/100).ToString(formatString) + " mb";
        }

        public static string FormatFileSize(int size, string formatString = null, bool roundDown = true)
        {
            if (roundDown)
            {
                if (size < 1024) return size.ToInt32().ToString(formatString) + " b";
                if (size < 1048576) return (size/1024).ToInt32().ToString(formatString) + " kb";
                return (size/10485.76/100).ToInt32().ToString(formatString) + " mb";
            }
            if (size < 1024) return Math.Round((double) size).ToString(formatString) + " b";
            if (size < 1048576) return Math.Round((double) size/1024).ToString(formatString) + " kb";
            return (Math.Round(size/10485.76)/100).ToString(formatString) + " mb";
        }

        public static bool Contains(this int[] numbers, int value)
        {
            if (numbers == null || numbers.Length < 1) return false;

            foreach (var i in numbers)
                if (i == value) return true;
            return false;
        }

        public static bool Near(this long number1, long number2, double maxDeviation)
        {
            return Near(number1, (decimal) number2, maxDeviation);
        }

        public static bool Near(this decimal number1, decimal number2, double maxDeviation)
        {
            return Math.Abs(number1 - number2) <= (decimal) maxDeviation;
        }

        public static bool GreaterThan(this Version version1, Version version2, int places = 4)
        {
            if (version1.Major < version2.Major) return false;

            var value1 = version1.Major == -1 ? 0 : version1.Major;
            var value2 = version2.Major == -1 ? 0 : version2.Major;
            if (places == 1) return value1 > value2;
            if (value1 < value2) return false;
            if (value1 > value2) return true;

            value1 = version1.Minor == -1 ? 0 : version1.Minor;
            value2 = version2.Minor == -1 ? 0 : version2.Minor;
            if (places == 2) return value1 > value2;
            if (value1 < value2) return false;
            if (value1 > value2) return true;

            value1 = version1.Build == -1 ? 0 : version1.Build;
            value2 = version2.Build == -1 ? 0 : version2.Build;
            if (places == 3) return value1 > value2;
            if (value1 < value2) return false;
            if (value1 > value2) return true;

            value1 = version1.Revision == -1 ? 0 : version1.Revision;
            value2 = version2.Revision == -1 ? 0 : version2.Revision;
            return value1 > value2;
        }


        public static bool EqualsExact(this Version version1, Version version2, int places = 4)
        {
            if (version1.Major != version2.Major) return false;

            var value1 = version1.Major == -1 ? 0 : version1.Major;
            var value2 = version2.Major == -1 ? 0 : version2.Major;
            if (places == 1) return value1 == value2;
            if (value1 != value2) return false;

            value1 = version1.Minor == -1 ? 0 : version1.Minor;
            value2 = version2.Minor == -1 ? 0 : version2.Minor;
            if (value1 != value2) return false;
            if (places == 2) return value1 == value2;

            value1 = version1.Build == -1 ? 0 : version1.Build;
            value2 = version2.Build == -1 ? 0 : version2.Build;
            if (places == 3) return value1 == value2;
            if (value1 != value2) return false;

            value1 = version1.Revision == -1 ? 0 : version1.Revision;
            value2 = version2.Revision == -1 ? 0 : version2.Revision;
            return value1 == value2;
        }

        public static string ToVirtualPath(this Version version)
        {
            return version.Major + "_" + version.Minor;
        }

        public static double Log2(this double num)
        {
            return Math.Log(num)/Math.Log(2);
        }

        public static int RoundOff(this decimal value, int nearest, double minimum = 0)
        {
            return RoundOff((double) value, nearest, minimum);
        }

        public static int RoundOff(this double value, int nearest, double minimum = 0)
        {
            if (minimum > 0 && value < minimum) value = minimum;
            value = Math.Ceiling(value/nearest)*nearest;
            return (int) value;
        }

        public static decimal Min(this decimal value, decimal minimum)
        {
            return (decimal) Min((double) value, (double) minimum);
        }

        public static double Min(this double value, double minimum)
        {
            if (value < minimum) value = minimum;
            return value;
        }

        public static decimal Max(this decimal value, decimal minimum)
        {
            return (decimal) Max((double) value, (double) minimum);
        }

        public static double Max(this double value, double maximum)
        {
            if (value > maximum) value = maximum;
            return value;
        }
    }
}