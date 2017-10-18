using System;

namespace Extensions
{
    public static class Other
    {
        public static bool IsUrl(this string url)
        {
            try
            {
                if (!url.StartsWithI("http:") && !url.StartsWithI("https:") && !url.StartsWithI("file:")) return false;
                var uri = new Uri(url, UriKind.Absolute);
                return uri.IsAbsoluteUri;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsLong(this string text)
        {
            long value;
            return long.TryParse(text, out value);
        }

        public static long ToLong(this string text)
        {
            long value;
            long.TryParse(text, out value);
            return value;
        }
    }
}