using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

namespace Extensions
{
    public static class Text
    {
        public const string NumberChars = "1234567890";
        public const string PunctuationChars1 = "!@#£$%^&*()";
        public const string PunctuationChars2 = "`~-_=+[{]}\\|;:'\",<.>/?";
        public const string SpecialPasswordChars = "!\"£$%^&*()-=+[]{}#';:@~,./<>?\\`";
        public const string UpperCaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public static string PunctuationChars = PunctuationChars1 + PunctuationChars2;
        public static string LowerCaseChars = UpperCaseChars.ToLower();
        public static string AlphabetChars = UpperCaseChars + LowerCaseChars;
        public static string AlphaNumericChars = UpperCaseChars + LowerCaseChars + NumberChars;

        public static bool ContainsAlphaNumeric(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            return Regex.IsMatch(input, "[a-zA-Z0-9]");
        }

        public static bool IsAlphaNumeric(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            return !Regex.IsMatch(input, "[^a-zA-Z0-9]");
        }

        public static string ToAlphaNumericOnly(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            var rgx = new Regex("[^a-zA-Z0-9]");
            return rgx.Replace(input, "");
        }

        public static bool ContainsAlpha(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            return Regex.IsMatch(input, "[a-zA-Z]");
        }

        public static bool IsAlpha(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            return !Regex.IsMatch(input, "[^a-zA-Z]");
        }

        public static string ToAlphaOnly(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            var rgx = new Regex("[^a-zA-Z]");
            return rgx.Replace(input, "");
        }

        public static bool IsNumber(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            return Regex.IsMatch(input, "^\\d+$");
        }

        public static bool IsCompanyNumber(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            return Regex.IsMatch(input, "^[0-9A-Za-z]{8}$");
        }

        public static bool ContainsNumber(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            return Regex.IsMatch(input, "[0-9]");
        }

        public static string ToNumber(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            var rgx = new Regex("[^0-9]");
            return rgx.Replace(input, "");
        }

        /// <summary>
        ///     Qoutes string and escapes fishy('\',"') chars.
        /// </summary>
        /// <param name="text">Text to quote.</param>
        /// <returns></returns>
        public static string QuoteString(this string text)
        {
            // String is already quoted-string.
            if (text != null && text.StartsWith("\"") && text.EndsWith("\""))
            {
                return text;
            }

            var retVal = new StringBuilder();

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (c == '\\')
                {
                    retVal.Append("\\\\");
                }
                else if (c == '\"')
                {
                    retVal.Append("\\\"");
                }
                else
                {
                    retVal.Append(c);
                }
            }

            return "\"" + retVal + "\"";
        }

        /// <summary>
        ///     Unquotes and unescapes escaped chars specified text. For example "xxx" will become to 'xxx', "escaped quote \"",
        ///     will become to escaped 'quote "'.
        /// </summary>
        /// <param name="text">Text to unquote.</param>
        /// <returns></returns>
        public static string UnQuoteString(this string text)
        {
            var startPosInText = 0;
            var endPosInText = text.Length;

            //--- Trim. We can't use standard string.Trim(), it's slow. ----//
            for (var i = 0; i < endPosInText; i++)
            {
                var c = text[i];
                if (c == ' ' || c == '\t')
                {
                    startPosInText++;
                }
                else
                {
                    break;
                }
            }
            for (var i = endPosInText - 1; i > 0; i--)
            {
                var c = text[i];
                if (c == ' ' || c == '\t')
                {
                    endPosInText--;
                }
                else
                {
                    break;
                }
            }
            //--------------------------------------------------------------//

            // All text trimmed
            if (endPosInText - startPosInText <= 0)
            {
                return "";
            }

            // Remove starting and ending quotes.         
            if (text[startPosInText] == '\"')
            {
                startPosInText++;
            }
            if (text[endPosInText - 1] == '\"')
            {
                endPosInText--;
            }

            // Just '"'
            if (endPosInText == startPosInText - 1)
            {
                return "";
            }

            var chars = new char[endPosInText - startPosInText];

            var posInChars = 0;
            var charIsEscaped = false;
            for (var i = startPosInText; i < endPosInText; i++)
            {
                var c = text[i];

                // Escaping char
                if (!charIsEscaped && c == '\\')
                {
                    charIsEscaped = true;
                }
                // Escaped char
                else if (charIsEscaped)
                {
                    // TODO: replace \n,\r,\t,\v ???
                    chars[posInChars] = c;
                    posInChars++;
                    charIsEscaped = false;
                }
                // Normal char
                else
                {
                    chars[posInChars] = c;
                    posInChars++;
                    charIsEscaped = false;
                }
            }

            return new string(chars, 0, posInChars);
        }

        public static bool IsNullOrWhiteSpace(this string input)
        {
            return string.IsNullOrWhiteSpace(input);
        }

        public static T Convert<T>(this string input)
        {
            if (!string.IsNullOrWhiteSpace(input))
            {
                var converter = TypeDescriptor.GetConverter(typeof (T));
                if (converter != null && converter.IsValid(input)) return (T) converter.ConvertFromString(input);
            }
            return default(T);
        }

        public static HashSet<string> FindAll(this string text, string matchPattern, bool IgnoreCase = false)
        {
            var results = new HashSet<string>(IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(text)) return results;

            var rx = new Regex(matchPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            // Find matches.
            var matches = rx.Matches(text);
            // Report on each match.
            foreach (Match match in matches)
            {
                results.Add(match.Value);
            }
            return results;
        }

        public static string FindFirst(this string text, string matchPattern, bool IgnoreCase = false)
        {
            var results = new HashSet<string>(IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(text)) return null;

            var rx = new Regex(matchPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            // Find matches.
            var matches = rx.Matches(text);
            // Report on each match.
            foreach (Match match in matches)
            {
                return match.Value;
            }
            return null;
        }

        public static string Format(this string text, params string[] args)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            if (args.Length == 0)
            {
                var match = Regex.Match(text, @"\{\d+\}");
                while (match.Success)
                {
                    text = text.Replace(match.Groups[0].Value, "");
                    match = Regex.Match(text, @"\{\d+\}");
                }
            }
            else
                text = string.Format(text, args);

            return text;
        }

        public static bool IsASCII(this string value)
        {
            // ASCII encoding replaces non-ascii with question marks, so we use UTF8 to see if multi-byte sequences are there
            return Encoding.UTF8.GetByteCount(value) == value.Length;
        }

        public static string Clean(this string text, string allowedChars)
        {
            if (text == null) return null;

            string newText = null;
            var chars = allowedChars.ToCharArray();
            for (var i = 0; i < text.Length; i++)
            {
                if (allowedChars.IndexOf(text[i]) > -1) newText += text[i];
            }
            return newText;
        }

        public static string TrimI(this string source, string trimChars)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(trimChars)) return source;
            return source.Trim(trimChars.ToCharArray());
        }

        public static string TrimI(this string source, params char[] trimChars)
        {
            if (string.IsNullOrEmpty(source)) return source;
            return trimChars == null || trimChars.Length == 0 ? source.Trim() : source.Trim(trimChars);
        }

        public static string TrimSuffix(this string source, string suffix)
        {
            if (source.EndsWith(suffix, StringComparison.CurrentCultureIgnoreCase)) source = source.Remove(source.Length - suffix.Length);
            return source;
        }

        public static string TrimEndI(this string source, string trimChars)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(trimChars)) return source;
            return source.TrimEndI(trimChars.ToCharArray());
        }

        public static string TrimEndI(this string source, params char[] trimChars)
        {
            if (string.IsNullOrEmpty(source)) return source;
            return trimChars == null || trimChars.Length == 0 ? source.TrimEnd() : source.TrimEnd(trimChars);
        }

        public static string TrimStartI(this string source, string trimChars)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(trimChars)) return source;
            return source.TrimStartI(trimChars.ToCharArray());
        }

        public static string TrimStartI(this string source, params char[] trimChars)
        {
            if (string.IsNullOrEmpty(source)) return source;
            return trimChars == null || trimChars.Length == 0 ? source.TrimStart() : source.TrimStart(trimChars);
        }

        public static string StripAlpha(this string text)
        {
            if (text == null) return null;

            string newText = null;

            for (var i = 0; i < text.Length; i++)
            {
                if (AlphabetChars.IndexOf(text[i]) < 0) newText += text[i];
            }
            return newText;
        }

        public static string StripAlphaNumeric(this string text)
        {
            if (text == null) return null;

            string newText = null;

            for (var i = 0; i < text.Length; i++)
            {
                if (AlphaNumericChars.IndexOf(text[i]) < 0) newText += text[i];
            }
            return newText;
        }

        public static string StripNumeric(this string text)
        {
            if (text == null) return null;

            string newText = null;

            for (var i = 0; i < text.Length; i++)
            {
                if (NumberChars.IndexOf(text[i]) < 0) newText += text[i];
            }
            return newText;
        }


        public static string Strip(this string text, string excludeChars)
        {
            if (text == null) return null;

            string newText = null;
            for (var i = 0; i < text.Length; i++)
            {
                if (excludeChars.IndexOf(text[i]) < 0) newText += text[i];
            }
            return newText;
        }

        public static string Coalesce(this string text, params string[] options)
        {
            if (!string.IsNullOrWhiteSpace(text)) return text;
            foreach (var option in options)
                if (!string.IsNullOrWhiteSpace(option)) return option;

            return null;
        }

        public static string CleanIdChars(this string text)
        {
            return text.Clean(AlphaNumericChars + "_-");
        }

        public static string ToProper(this string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text)) return text;
                var cultureInfo = Thread.CurrentThread.CurrentCulture;
                var TextInfo = cultureInfo.TextInfo;
                return TextInfo.ToTitleCase(text);
            }
            catch
            {
                return text;
            }
        }

        //Checks if a byte sequence starts with another byte sequence
        public static bool StartsWith(this string text, byte[] subStr)
        {
            if (text.Length < subStr.Length || subStr.Length < 1) return false;
            for (var i = 0; i < subStr.Length; i++)
            {
                if (text[i] != subStr[i]) return false;
            }
            return true;
        }

        public static string ToValidFileName(this string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return fileName;
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }

        public static bool AllSameChars(this string text)
        {
            if (string.IsNullOrEmpty(text)) throw new ArgumentException("Missing argument");
            if (text.Length == 1) return true;
            return new string(text[0], text.Length) == text;
        }

        public static int LineCount(this string text, string newLineChars = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            if (string.IsNullOrWhiteSpace(newLineChars)) newLineChars = Environment.NewLine;
            text = text.Replace(newLineChars, "\n");
            var args = text.Split('\n');
            return args.Length;
        }

        public static List<string> GetLines(this string text, bool removeEmpty = true, bool trimLines = true, string newLineChars = null)
        {
            var lines = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return lines;
            if (string.IsNullOrWhiteSpace(newLineChars)) newLineChars = Environment.NewLine;
            text = text.Replace(newLineChars, "\n");
            var args = text.Split("\n".ToCharArray(), removeEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
            lines.AddRange(args);
            for (var i = lines.Count - 1; i > -1; i--)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    lines.RemoveAt(i);
                else if (trimLines)
                    lines[i] = lines[i].Trim();
            }
            return lines;
        }

        public static IEnumerable<string> GetSubStrings(this string input, string start, string end)
        {
            Regex r = new Regex(Regex.Escape(start) +"(.*?)"  +Regex.Escape(end));
            MatchCollection matches = r.Matches(input);
            foreach (Match match in matches)
                yield return match.Groups[1].Value;
        }

        /// <summary>
        ///     Returns all characters before the first occurence of a string
        /// </summary>
        public static string BeforeFirst(this string text, string seperator, StringComparison comparisionType = StringComparison.OrdinalIgnoreCase, bool inclusive = false, bool includeWhenNoSeperator = true)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            var i = text.IndexOf(seperator, 0, comparisionType);
            if (i > -1) return text.Substring(0, inclusive ? i + 1 : i);
            return includeWhenNoSeperator ? text : null;
        }

        /// <summary>
        ///     Returns all characters before the first occurence of any specified character
        /// </summary>
        public static string BeforeFirstAny(this string text, string seperators, StringComparison comparisionType = StringComparison.OrdinalIgnoreCase, bool inclusive = false)
        {
            if (comparisionType.IsAny(StringComparison.OrdinalIgnoreCase, StringComparison.CurrentCultureIgnoreCase, StringComparison.InvariantCultureIgnoreCase))
            {
                text = text.ToLower();
                seperators = seperators.ToLower();
            }

            var i = text.IndexOfAny(seperators.ToCharArray(), 0);
            if (i > -1) return text.Substring(0, inclusive ? i + 1 : i);
            return text;
        }

        /// <summary>
        ///     Returns all characters before the last occurence of a string
        /// </summary>
        public static string BeforeLast(this string text, string seperator, StringComparison comparisionType = StringComparison.OrdinalIgnoreCase, bool inclusive = false, bool includeWhenNoSeperator = true)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            var i = text.LastIndexOf(seperator, text.Length - 1, comparisionType);
            if (i > -1) return text.Substring(0, inclusive ? i + 1 : i);
            return includeWhenNoSeperator ? text : null;
        }

        /// <summary>
        ///     Returns all characters after the first occurence of a string
        /// </summary>
        public static string AfterFirst(this string text, string seperator, StringComparison comparisionType = StringComparison.OrdinalIgnoreCase, bool includeSeperator = false, bool includeWhenNoSeperator = true)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            var i = text.IndexOf(seperator, 0, comparisionType);
            if (i > -1) return text.Substring(includeSeperator ? i : i + seperator.Length);
            return includeWhenNoSeperator ? text : null;
        }

        /// <summary>
        ///     Returns all characters after the last occurence of a string
        /// </summary>
        public static string AfterLast(this string text, string seperator, StringComparison comparisionType = StringComparison.OrdinalIgnoreCase, bool includeSeperator = false, bool includeWhenNoSeperator = true)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            var i = text.LastIndexOf(seperator, text.Length - 1, comparisionType);
            if (i > -1) return text.Substring(includeSeperator ? i : i + 1);
            return includeWhenNoSeperator ? text : null;
        }

        /// <summary>
        ///     Returns all characters after the last occurence of any specified character
        /// </summary>
        public static string AfterLastAny(this string text, string seperators, StringComparison comparisionType = StringComparison.OrdinalIgnoreCase, bool inclusive = false)
        {
            if (comparisionType.IsAny(StringComparison.OrdinalIgnoreCase, StringComparison.CurrentCultureIgnoreCase, StringComparison.InvariantCultureIgnoreCase))
            {
                text = text.ToLower();
                seperators = seperators.ToLower();
            }

            var i = text.LastIndexOfAny(seperators.ToCharArray(), text.Length - 1);
            if (i > -1) return text.Substring(inclusive ? i : i + 1);
            return null;
        }

        public static bool EqualsI(this string original, params string[] target)
        {
            if (string.IsNullOrWhiteSpace(original)) original = "";
            for (var i = 0; i < target.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(target[i])) target[i] = "";
                if (original.Equals(target[i], StringComparison.InvariantCultureIgnoreCase)) return true;
            }
            return false;
        }

        public static bool EqualsEmail(this string email1, params string[] emails)
        {
            email1 = email1.GetEmailAddress();
            if (string.IsNullOrWhiteSpace(email1)) return false;

            foreach (var email in emails)
            {
                if (string.IsNullOrWhiteSpace(email)) continue;

                var email2 = email.GetEmailAddress();

                if (string.IsNullOrWhiteSpace(email2)) continue;

                if (email1.EqualsI(email2)) return true;
            }
            return false;
        }

        public static bool EqualsUrl(this string url1, string url2)
        {
            url1 = url1.Trim("\r\n /\\?&".ToCharArray());
            url2 = url2.Trim("\r\n /\\?&".ToCharArray());
            return url1.EqualsI(url2);
        }

        public static bool EqualsUrlHost(this string url1, string url2)
        {
            return new Uri(url1).Host.EqualsI(new Uri(url2).Host);
        }

        public static bool EqualsVersion(this string original, string target)
        {
            var originalVersionParts = original.Split('.');
            var targetVersionParts = target.Split('.');

            string originalStr = null;
            string targetStr = null;
            for (var i = 0; i < 4; i++)
            {
                originalStr = i < originalVersionParts.Length ? originalVersionParts[i] : originalStr == "*" ? originalStr : "0";
                targetStr = i < targetVersionParts.Length ? targetVersionParts[i] : "0";
                if (originalStr != "*" && originalVersionParts[i] != targetVersionParts[i]) return false;
            }
            return true;
        }

        public static string Pad(this string text, char character)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            var mask = new string(character, text.Length);
            return mask;
        }

        public static string Mask(this string text, char character, int count)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            var mask = text.Pad(character);
            if (count > 0) return mask.Substring(0, count) + text.Substring(count);
            if (count < 0) return mask.Substring(0, text.Length - Math.Abs(count)) + text.Substring(text.Length - Math.Abs(count), Math.Abs(count));
            return mask;
        }

        public static bool Contains(this string[] array, string text)
        {
            if (array == null || array.Length < 1) return false;
            foreach (var str in array)
                if (str.EqualsI(text)) return true;
            return false;
        }

        public static bool IsCustomerCode(this string customerCode)
        {
            if (string.IsNullOrWhiteSpace(customerCode)) return false;
            customerCode = customerCode.ToUpper().Trim();
            if (customerCode.Length != 8) return false;
            var CharPool = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            foreach (var c in customerCode)
                if (CharPool.IndexOf(c) == -1) return false;
            return true;
        }

        public static bool ContainsI(this string source, string pattern)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrEmpty(pattern)) return false;
            return source.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool ContainsAnyI(this string source, List<string> words)
        {
            return ContainsAnyI(source, words.ToArray());
        }

        public static bool ContainsAnyI(this string source, params string[] words)
        {
            return IndexOfAnyI(source, words) > -1;
        }


        public static bool ContainsAny(this string text, params char[] characters)
        {
            if (string.IsNullOrWhiteSpace(text) || characters == null || characters.Length < 1) return false;
            return text.IndexOfAny(characters) > -1;
        }

        public static int IndexOfAnyI(this string source, List<string> words)
        {
            return IndexOfAnyI(source, words.ToArray());
        }

        public static int IndexOfAnyI(this string source, params string[] words)
        {
            if (string.IsNullOrWhiteSpace(source) || words == null || words.Length < 1) return -1;

            for (var i = 0; i < words.Length; i++)
            {
                if (source.ContainsI(words[i])) return i;
            }
            return -1;
        }

        public static bool ContainsAnyWords(this string source, List<string> words)
        {
            return ContainsAnyWords(source, words.ToArray());
        }

        public static bool ContainsAnyWords(this string source, params string[] words)
        {
            if (string.IsNullOrWhiteSpace(source)) return false;
            foreach (var word in words)
            {
                if (source.ContainsWordI(word)) return true;
            }
            return false;
        }

        public static bool ContainsAllWords(this string source, params string[] words)
        {
            if (string.IsNullOrWhiteSpace(source)) return false;
            foreach (var word in words)
            {
                if (!source.ContainsWordI(word)) return false;
            }
            return true;
        }

        public static bool EndsWithI(this string source, params string[] strings)
        {
            if (string.IsNullOrWhiteSpace(source)) return false;
            foreach (var str in strings)
            {
                if (string.IsNullOrWhiteSpace(str)) continue;
                if (source.ToLower().EndsWith(str.ToLower())) return true;
            }
            return false;
        }

        public static bool StartsWithAny(this string source, params char[] chars)
        {
            return source.Length > 0 && source[0].IsAny(chars);
        }

        public static bool EndsWithAny(this string source, params char[] chars)
        {
            return source.Length > 0 && source[source.Length - 1].IsAny(chars);
        }

        public static bool StartsWithAnyWords(this string source, params string[] words)
        {
            if (string.IsNullOrWhiteSpace(source)) return false;
            foreach (var word in words)
            {
                if (source.StartsWithWord(word)) return true;
            }
            return false;
        }

        public static bool ContainsWordI(this string source, string word)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(word)) return false;
            var pattern = string.Format(@"\b{0}\b", word);
            return Regex.IsMatch(source, pattern, RegexOptions.IgnoreCase);
        }

        public static bool StartsWithWord(this string source, string word, bool ignoreCase = true)
        {
            var words = source.ToWords();
            foreach (var item in words)
            {
                if (string.IsNullOrWhiteSpace(item)) continue;
                if (item.Equals(word, ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture)) return true;
                break;
            }
            return false;
        }

        public static string GetWord(this string source, int index)
        {
            var words = source.ToWords().ToListOrEmpty();
            if (index < words.Count && words.Count > 0) return words[index];
            return null;
        }

        public static bool ContainsWordEndingWithI(this string source, params string[] ends)
        {
            foreach (var word in source.ToWords())
            {
                if (string.IsNullOrWhiteSpace(word)) continue;
                if (source.ToLower().EndsWith(word.ToLower())) return true;
            }
            return false;
        }

        public static bool IsHtml(this string Html)
        {
            if (string.IsNullOrEmpty(Html)) return false;
            Html = Html.ToLower();
            var h1 = Html.IndexOf("<html>");
            if (h1 == -1) h1 = Html.IndexOf("<html");
            var h2 = Html.IndexOf("<body>");
            if (h2 == -1) h2 = Html.IndexOf("<body");
            var h3 = Html.IndexOf("</body>");
            var h4 = Html.IndexOf("</html>");

            return (h4 > h3) && (h3 > h2) && (h2 > h1) && (h1 > -1);
        }

        [DebuggerStepThrough]
        public static bool IsXml(this string xml, string rootName = null)
        {
            if (xml.IsNullOrWhiteSpace()) return false;

            var xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.LoadXml(xml);
            }
            catch (XmlException)
            {
                return false;
            }
            return rootName == null || xmlDoc.DocumentElement.Name.EqualsI(rootName);
        }

        public static bool IsInteger(this string strToCheck)
        {
            var r = 0;
            return int.TryParse(strToCheck, out r);
        }

        public static bool IsGuid(this string strToCheck)
        {
            Guid g;
            return Guid.TryParse(strToCheck, out g);
        }

        public static bool IsRealNumber(this string strToCheck)
        {
            if (string.IsNullOrWhiteSpace(strToCheck) || strToCheck.Length < 1) return false;
            var objNotWholePattern = new Regex(@"^[0-9\.]$");
            return !objNotWholePattern.IsMatch(strToCheck);
        }

        public static string ReplaceAllEx(this string original, string pattern, string replacement)
        {
            if (!string.IsNullOrWhiteSpace(original) && !string.IsNullOrEmpty(pattern) && pattern != replacement)
                original = Regex.Replace(original, pattern, replacement, RegexOptions.IgnoreCase);

            return original;
        }

        public static string ReplaceAll(this string original, string source, string replacement)
        {
            if (!string.IsNullOrWhiteSpace(original) && !string.IsNullOrWhiteSpace(source) && source != replacement)
                while (original.IndexOf(source) > -1)
                    original = original.Replace(source, replacement);

            return original;
        }

        public static string ReplaceAll(this string original, char[] chars, string replacement)
        {
            if (!string.IsNullOrWhiteSpace(original))
                foreach (var ch in chars)
                {
                    var pattern = ch.ToString();
                    while (pattern != replacement && original.IndexOf(pattern) > -1)
                        original = original.Replace(pattern, replacement);
                }
            return original;
        }

        public static string ReplaceIAll(this string original, string pattern, string replacement)
        {
            if (original != null && pattern != null)
                while (!pattern.EqualsI(replacement) && original.ToLower().IndexOf(pattern.ToLower()) > -1)
                    original = original.ReplaceI(pattern, replacement);

            return original;
        }

        public static object Parse(this string text)
        {
            bool boolValue;
            int intValue;
            long bigintValue;
            double doubleValue;
            DateTime dateValue;

            // Place checks higher if if-else statement to give higher priority to type.

            if (bool.TryParse(text, out boolValue))
                return boolValue;
            if (int.TryParse(text, out intValue))
                return intValue;
            if (long.TryParse(text, out bigintValue))
                return bigintValue;
            if (double.TryParse(text, out doubleValue))
                return doubleValue;
            if (DateTime.TryParse(text, out dateValue))
                return dateValue;

            return text;
        }

        /// <summary>
        ///     Super-fast Case-Insensitive text replace
        /// </summary>
        /// <param name="text">The original text string</param>
        /// <param name="fromStr">The string to search for</param>
        /// <param name="toStr">The string to replace with</param>
        /// <returns></returns>
        public static string ReplaceI(this string original, string pattern, string replacement = null)
        {
            if (string.IsNullOrWhiteSpace(original)) return null;
            if (string.IsNullOrWhiteSpace(replacement)) replacement = "";
            int count, position0, position1;
            count = position0 = position1 = 0;
            var upperString = original.ToUpper();
            var upperPattern = pattern.ToUpper();
            var inc = original.Length/pattern.Length*
                      (replacement.Length - pattern.Length);
            var chars = new char[original.Length + Math.Max(0, inc)];
            while ((position1 = upperString.IndexOf(upperPattern,
                position0)) != -1)
            {
                for (var i = position0; i < position1; ++i)
                    chars[count++] = original[i];
                for (var i = 0; i < replacement.Length; ++i)
                    chars[count++] = replacement[i];
                position0 = position1 + pattern.Length;
            }
            if (position0 == 0) return original;
            for (var i = position0; i < original.Length; ++i)
                chars[count++] = original[i];
            return new string(chars, 0, count);
        }

        public static string ReplaceWholeWord(this string original, string wordToFind, string replacement, RegexOptions regexOptions = RegexOptions.None)
        {
            var pattern = string.Format(@"\b{0}\b", wordToFind);
            var ret = Regex.Replace(original, pattern, replacement, regexOptions);
            return ret;
        }

        public static IEnumerable<string> ToWords(this string text)
        {
            if (string.IsNullOrWhiteSpace(text)) yield break;
            // Match the regular expression pattern against a text string.
            var m = Regex.Match(text, @"\w+");
            while (m.Success)
            {
                yield return m.Groups[0].Value;
                m = m.NextMatch();
            }
        }

        public static string ReplaceWithCase(this string original, string pattern, string replacement)
        {
            original = original.Replace(pattern.ToUpper(), replacement.ToUpper());
            original = original.Replace(pattern.ToLower(), replacement.ToLower());
            original = original.Replace(pattern.ToProper(), replacement.ToProper());
            original = original.ReplaceI(pattern, replacement);
            return original;
        }

        public static bool StartsWithI(this string original, params string[] texts)
        {
            if (string.IsNullOrWhiteSpace(original)) return false;
            if (texts != null)
                foreach (var text in texts)
                    if (text != null && original.ToLower().StartsWith(text.ToLower())) return true;
            return false;
        }

        public static int IndexOfStartsWithI(this string original, params string[] texts)
        {
            if (string.IsNullOrWhiteSpace(original)) return -1;
            for (var i = 0; i < texts.Length; i++)
                if (original.ToLower().StartsWith(texts[i].ToLower())) return i;
            return -1;
        }

        public static string Left(this string original, int length)
        {
            if (string.IsNullOrWhiteSpace(original) || length >= original.Length) return original;
            return original.Substring(0, length);
        }

        public static string Right(this string original, int length)
        {
            if (string.IsNullOrWhiteSpace(original) || length >= original.Length) return original;
            return original.Substring(original.Length - length, length);
        }

        public static bool LikeAny(this string input, IEnumerable<string> patterns)
        {
            foreach (var pattern in patterns)
                if (input.Like(pattern)) return true;

            return false;
        }

        public static bool Like(this string input, string pattern)
        {
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrEmpty(pattern)) return false;
            input = input.ToLower();
            pattern = pattern.ToLower();
            if (input == pattern) return true;
            var expression = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".").Replace("+", "\\+") + "$";
            return Regex.IsMatch(input, expression);
        }

        public static bool IsEscaped(this Match match, string originalText, string escapePrefix = @"\", string unescapePrefix = @"\\", bool ignoreCase = true)
        {
            if (!match.Success) return false;
            if (ignoreCase)
            {
                escapePrefix = escapePrefix == null ? null : escapePrefix.ToLower();
                unescapePrefix = unescapePrefix == null ? null : unescapePrefix.ToLower();
            }
            var prefix = originalText.Substring(0, match.Index);
            return prefix.EndsWith(escapePrefix) && (string.IsNullOrWhiteSpace(unescapePrefix) || !prefix.EndsWith(unescapePrefix));
        }

        public static bool Match(this string input, string pattern, bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pattern)) return false;
            if (!ignoreCase && input == pattern) return true;
            if (ignoreCase && input.ToLower() == pattern.ToLower()) return true;
            return ignoreCase ? Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase) : Regex.IsMatch(input, pattern);
        }

        public static bool Match(this string input, string pattern, bool ignoreCase, params string[] escapePrefixes)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pattern)) return false;
            if (!ignoreCase && input == pattern) return true;
            if (ignoreCase && input.ToLower() == pattern.ToLower()) return true;

            var regex = ignoreCase ? new Regex(pattern, RegexOptions.IgnoreCase) : new Regex(pattern);

            var results = regex.Matches(input);

            if (results != null && results.Count > 0)
            {
                if (escapePrefixes == null || escapePrefixes.Length < 1) return true;

                foreach (Match match in results)
                {
                    var bad = false;
                    foreach (var escapePrefix in escapePrefixes)
                    {
                        //Ignore fields starting with breaking space
                        if (match.IsEscaped(input, escapePrefix, null, ignoreCase))
                        {
                            bad = true;
                            break;
                        }
                    }
                    if (!bad) return true;
                }
            }
            return false;
        }

        public static string ToText(this Stream stream)
        {
            using (var reader = new StreamReader(stream)) return reader.ReadToEnd();
        }

        /// <summary>
        ///     Parses a valid email address out of the input string and return it.
        ///     Null is returned if no address is found.
        /// </summary>
        public static string RegexGetGroup(string pattern, string group, string input)
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var match = regex.Match(input);
            if (match.Success)
            {
                return match.Result("${" + group + "}");
            }
            return null;
        }

        public static string ToCSV(this DataTable table)
        {
            var result = new StringBuilder();
            for (var i = 0; i < table.Columns.Count; i++)
            {
                result.Append("\"" + table.Columns[i].ColumnName + "\"");
                result.Append(i == table.Columns.Count - 1 ? "\n" : ",");
            }

            foreach (DataRow row in table.Rows)
            {
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    result.Append("\"" + row[i] + "\"");
                    result.Append(i == table.Columns.Count - 1 ? "\n" : ",");
                }
            }

            return result.ToString();
        }

        public static string ToShortString(this Guid guid)
        {
            return guid.ToString().Strip("- {}");
        }

        /// <summary>
        ///     Word wraps the given text to fit within the specified width.
        /// </summary>
        /// <param name="text">Text to be word wrapped</param>
        /// <param name="width">
        ///     Width, in characters, to which the text
        ///     should be word wrapped
        /// </param>
        /// <returns>The modified text</returns>
        public static string WordWrap(this string text, int width)
        {
            int pos, next;
            var sb = new StringBuilder();

            // Lucidity check
            if (width < 1)
                return text;

            // Parse each line of text
            for (pos = 0; pos < text.Length; pos = next)
            {
                // Find end of line
                var eol = text.IndexOf(Environment.NewLine, pos);
                if (eol == -1)
                    next = eol = text.Length;
                else
                    next = eol + Environment.NewLine.Length;

                // Copy this line of text, breaking into smaller lines as needed
                if (eol > pos)
                {
                    do
                    {
                        var len = eol - pos;
                        if (len > width)
                            len = BreakLine(text, pos, width);
                        sb.Append(text, pos, len);
                        sb.Append(Environment.NewLine);

                        // Trim whitespace following break
                        pos += len;
                        while (pos < eol && char.IsWhiteSpace(text[pos]))
                            pos++;
                    } while (eol > pos);
                }
                else sb.Append(Environment.NewLine); // Empty line
            }
            return sb.ToString();
        }

        /// <summary>
        ///     Locates position to break the given line so as to avoid
        ///     breaking words.
        /// </summary>
        /// <param name="text">String that contains line of text</param>
        /// <param name="pos">Index where line of text starts</param>
        /// <param name="max">Maximum line length</param>
        /// <returns>The modified line length</returns>
        private static int BreakLine(string text, int pos, int max)
        {
            // Find last whitespace in line
            var i = max;
            while (i >= 0 && !char.IsWhiteSpace(text[pos + i]))
                i--;

            // If no whitespace found, break at maximum length
            if (i < 0)
                return max;

            // Find start of whitespace
            while (i >= 0 && char.IsWhiteSpace(text[pos + i]))
                i--;

            // Return length of text before whitespace
            return i + 1;
        }

        public static string ErrorBreak(this string text)
        {
            var result = text.WordWrap(80);
            result = result.Trim();
            result = result.Replace(Environment.NewLine, Environment.NewLine + "\t\t");
            return result.TrimEnd('\t');
        }

        [DebuggerStepThrough]
        public static bool IsBase64Encoded(this string str)
        {
            try
            {
                // If no exception is caught, then it is possibly a base64 encoded string
                var data = System.Convert.FromBase64String(str);
                // The part that checks if the string was properly padded to the
                // correct length was borrowed from d@anish's solution
                return str.Replace(" ", "").Length%4 == 0;
            }
            catch
            {
                // If exception is caught, then it is not a base64 encoded string
                return false;
            }
        }

        [DebuggerStepThrough]
        public static string FromBase64(this string base64String)
        {
            if (!string.IsNullOrWhiteSpace(base64String))
            {
                try
                {
                    // If no exception is caught, then it is possibly a base64 encoded string
                    var bytes = System.Convert.FromBase64String(base64String);
                    // The part that checks if the string was properly padded to the
                    // correct length was borrowed from d@anish's solution
                    if (base64String.Replace(" ", "").Length%4 == 0) base64String = Encoding.UTF8.GetString(bytes);
                }
                catch
                {
                }
            }
            return base64String;
        }

        public static string ToBase64(this string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                text = System.Convert.ToBase64String(bytes);
            }
            return text;
        }

        public static string EncodeUrlBase64(this string base64String)
        {
            if (!string.IsNullOrWhiteSpace(base64String))
            {
                base64String = base64String.Replace('+', '-');
                base64String = base64String.Replace('/', '_');
                base64String = base64String.Replace('=', '!');
            }
            return base64String;
        }

        public static string DecodeUrlBase64(this string base64String)
        {
            if (!string.IsNullOrWhiteSpace(base64String))
            {
                base64String = base64String.Replace('-', '+');
                base64String = base64String.Replace('_', '/');
                base64String = base64String.Replace('!', '=');
            }
            return base64String;
        }

        public static int LevenshteinCompute(this string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }

        public static bool IsUK(this string country)
        {
            return string.IsNullOrWhiteSpace(country) || country.EqualsI("England", "United Kingdom", "GB", "UK", "Great Britain", "Britain", "Scotland", "Wales", "Northern Ireland");
        }
        public static bool IsUKregion(this string country)
        {
            return country.EqualsI("England", "Scotland", "Wales", "Northern Ireland");
        }

    }
}