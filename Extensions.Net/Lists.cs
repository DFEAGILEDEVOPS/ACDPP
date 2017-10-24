using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Net;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace Extensions
{
    public static class Lists
    {
        public static string[] SplitI(this string list, string separators = ";,", int maxItems = 0, StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries)
        {
            if (string.IsNullOrWhiteSpace(list)) return new string[0];
            if (separators == null) throw new ArgumentNullException("separators");
            if (separators == string.Empty) return list.ToCharArray().Select(c => c.ToString()).ToArray();

            if (maxItems > 0) return list.Split(separators.ToCharArray(), maxItems, options);
            return list.Split(separators.ToCharArray(), options);
        }

        public static bool ContainsSame<T>(this IEnumerable<T> source, IEnumerable<T> target)
        {
            return !source.Except(target).Any() && !target.Except(source).Any();
        }

        /// <summary>
        ///     Splits string into string arrays. This split method won't split qouted strings, but only text outside of qouted
        ///     string.
        ///     For example: '"text1, text2",text3' will be 2 parts: "text1, text2" and text3.
        /// </summary>
        /// <param name="text">Text to split.</param>
        /// <param name="splitChar">Char that splits text.</param>
        /// <param name="unquote">If true, splitted parst will be unqouted if they are qouted.</param>
        /// <param name="count">Maximum number of substrings to return.</param>
        /// <returns>Returns splitted string.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>text</b> is null reference.</exception>
        public static string[] SplitQuotedString(this string text, char splitChar, bool unquote = false, int count = int.MaxValue)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            var splitParts = new List<string>(); // Holds splitted parts
            var startPos = 0;
            var inQuotedString = false; // Holds flag if position is quoted string or not
            var lastChar = '0';

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                // We have exceeded maximum allowed splitted parts.
                if (splitParts.Count + 1 >= count)
                {
                    break;
                }

                // We have quoted string start/end.
                if (lastChar != '\\' && c == '\"')
                {
                    inQuotedString = !inQuotedString;
                }
                // We have escaped or normal char.
                //else{

                // We igonre split char in quoted-string.
                if (!inQuotedString)
                {
                    // We have split char, do split.
                    if (c == splitChar)
                    {
                        if (unquote)
                        {
                            splitParts.Add(text.Substring(startPos, i - startPos).UnQuoteString());
                        }
                        else
                        {
                            splitParts.Add(text.Substring(startPos, i - startPos));
                        }

                        // Store new split part start position.
                        startPos = i + 1;
                    }
                }
                //else{

                lastChar = c;
            }

            // Add last split part to splitted parts list
            if (unquote)
            {
                splitParts.Add(text.Substring(startPos, text.Length - startPos).UnQuoteString());
            }
            else
            {
                splitParts.Add(text.Substring(startPos, text.Length - startPos));
            }

            return splitParts.ToArray();
        }

        public static void Add(this IEnumerable<string> target, params string[] source)
        {
            foreach (var item in source)
                target.Add(item);
        }

        public static IEnumerable<string> Insert(this IEnumerable<string> target, int index, params string[] source)
        {
            var result = target.ToList();
            foreach (var item in source.Reverse())
                result.Insert(index, item);

            return result;
        }

        public static string[] Add(this string[] target, params string[] source)
        {
            var result = new List<string>(target);
            foreach (var item in source)
                result.Add(item);
            return result.ToArray();
        }

        public static string[] Insert(this string[] target, int index, params string[] source)
        {
            var result = new List<string>(target);
            foreach (var item in source.Reverse())
                result.Insert(index, item);
            return result.ToArray();
        }

        public static void Add(this HashSet<string> target, IEnumerable<string> source)
        {
            foreach (var item in source)
                target.Add(item);
        }

        public static IEnumerable<string> DistinctI(this IEnumerable<string> list, bool ignoreCase = true)
        {
            return list.Distinct(ignoreCase ? StringComparer.CurrentCultureIgnoreCase : StringComparer.CurrentCulture);
        }

        public static IEnumerable<string> UniqueI(this IEnumerable<string> list, bool ignoreCase = true)
        {
            return list.Distinct(ignoreCase ? StringComparer.CurrentCultureIgnoreCase : StringComparer.CurrentCulture);
        }

        public static bool Same(this IEnumerable<string> list, bool ignoreCase = true)
        {
            return list.DistinctI(ignoreCase).Count() == 1;
        }

        public static bool ContainsI(this IEnumerable<string> list, params string[] text)
        {
            if (list == null || text == null || text.Length == 0) return false;
            var li = list.ToList();
            return text.Any(t => li.Any(l => l.EqualsI(t)));
        }

        public static bool ContainsI(this string source, params string[] list)
        {
            if (string.IsNullOrWhiteSpace(source)) return false;
            return list.Any(source.ContainsI);
        }

        public static int IndexOfI(this string[] list, string text)
        {
            return list.ToList().IndexOfI(text);
        }

        public static int IndexOfI(this IEnumerable<string> list, string text)
        {
            return list.ToList().FindIndex(l => l.EqualsI(text));
        }

        public static bool ContainsLike(this IEnumerable<string> list, params string[] patterns)
        {
            return patterns.Any(pattern => list.Any(text => text.Like(pattern)));
        }

        public static bool ContainsLike(this ConcurrentList<string> list, string pattern)
        {
            foreach (var text in list)
                if (text.Like(pattern)) return true;
            return false;
        }

        public static bool ContainsLikeEmail(this IEnumerable<string> list, string email)
        {
            email = email.GetEmailAddress();
            foreach (var text in list)
                if (text.GetEmailAddress().Like(email)) return true;
            return false;
        }

        public static bool ContainsEmail(this IEnumerable<string> list, string email)
        {
            if (string.IsNullOrWhiteSpace(email) || list == null || list.Count() == 0) return false;

            email = email.GetEmailAddress();
            foreach (var pattern in list)
                if (pattern.EqualsEmail(email)) return true;

            return false;
        }

        public static bool ContainsAllEmails(this IEnumerable<string> inputEmails)
        {
            var found = false;
            foreach (var email in inputEmails)
            {
                if (string.IsNullOrWhiteSpace(email)) continue;
                var address = email.GetEmailAddress();
                if (string.IsNullOrWhiteSpace(address)) return false;
                if (!address.IsEmailAddress()) return false;
                found = true;
            }
            return found;
        }

        public static bool EqualsI(this List<string> sourceList, List<string> targetList, bool ignoreCase = false)
        {
            if (targetList != null && sourceList == null) return false;
            if (targetList == null && sourceList != null) return false;
            if (targetList.Count != sourceList.Count) return false;

            sourceList.Sort();
            targetList.Sort();
            for (var i = 0; i < sourceList.Count; i++)
            {
                if (ignoreCase && !sourceList[i].EqualsI(targetList[i])) return false;
                if (!ignoreCase && !sourceList[i].Equals(targetList[i])) return false;
            }

            return true;
        }

        public static bool EqualsEmail(this List<string> sourceList, List<string> targetList)
        {
            if (targetList.Count != sourceList.Count) return false;

            sourceList.Sort();
            targetList.Sort();
            for (var i = 0; i < sourceList.Count; i++)
            {
                if (!sourceList[i].EqualsEmail(targetList[i])) return false;
            }

            return true;
        }

        public static string ToQueryString(this SerializableDictionary<int, string> collection)
        {
            if (collection == null || collection.Count < 1) return null;
            string data = null;
            foreach (var key in collection.Keys)
            {
                if (string.IsNullOrWhiteSpace(collection[key])) continue;
                if (data != null) data += "&";
                data += key + "=" + collection[key];
            }
            return data;
        }

        public static NameValueCollection NotExclude(this NameValueCollection collection, params string[] keys)
        {
            var results = new NameValueCollection();
            foreach (var key in keys)
            {
                if (collection.ContainsKey(key))
                    results[key] = collection[key];
            }
            return results;
        }

        public static NameValueCollection Exclude(this NameValueCollection collection, params string[] keys)
        {
            var results = new NameValueCollection(collection);
            foreach (var key in keys)
            {
                results.Remove(key);
            }
            return results;
        }

        public static NameValueCollection ToCollection(this List<NameValueElement> list)
        {
            var results = new NameValueCollection();
            foreach (var element in list)
            {
                if (string.IsNullOrWhiteSpace(element.Name) || string.IsNullOrWhiteSpace(element.Value)) continue;
                results[element.Name] = element.Value;
            }
            if (results.Count < 1) return null;
            return results;
        }

        public static bool Equals(List<NameValueElement> source, List<NameValueElement> target)
        {
            if (source == null) return target == null;
            if (source.Count != target.Count) return false;
            source = source.OrderBy(e => e.Name).ThenBy(e => e.Value).ToList();
            target = target.OrderBy(e => e.Name).ThenBy(e => e.Value).ToList();
            for (var i = 0; i < source.Count; i++)
                if (source[i].Name != target[i].Name || source[i].Value != target[i].Value) return false;
            return true;
        }

        public static bool Equals(this NameValueCollection source, NameValueCollection target)
        {
            if (source == null) return target == null;
            if (source.Count != target.Count) return false;
            if (!source.AllKeys.Contains(target.AllKeys)) return false;
            return source.AllKeys.All(key => target[key] == source[key]);
        }

        public static string ToQueryString(this Dictionary<string,string> collection)
        {
            var data = "";
            if (collection != null)
                foreach (string key in collection.Keys)
                {
                    if (string.IsNullOrWhiteSpace(collection[key])) continue;
                    if (!string.IsNullOrWhiteSpace(data)) data += "&";
                    var value = collection[key];
                    if (string.IsNullOrWhiteSpace(key))
                        data += value;
                    else
                        data += HttpUtility.UrlEncode(key) + "=" + value;
                }
            return data;
        }


        public static string ToQueryString(this NameValueCollection collection)
        {
            var data = "";
            if (collection != null)
                foreach (string key in collection.Keys)
                {
                    if (string.IsNullOrWhiteSpace(collection[key])) continue;
                    if (!string.IsNullOrWhiteSpace(data)) data += "&";
                    var value = collection[key].SplitI(",").Select(v => HttpUtility.UrlEncode(v.TrimI())).ToDelimitedString(",");
                    if (string.IsNullOrWhiteSpace(key))
                        data += value;
                    else
                        data += HttpUtility.UrlEncode(key) + "=" + value;
                }
            return data;
        }

        public static string ToCookieString(this NameValueCollection collection, char equals = '=', char semicolon = ';')
        {
            string data = null;
            foreach (string key in collection.Keys)
            {
                if (string.IsNullOrWhiteSpace(collection[key])) continue;
                if (data != null) data += semicolon;
                data += key + equals + HttpUtility.UrlEncode(collection[key]);
            }
            return data;
        }

        public static List<T> ToList<T>(this string collection, string delimiters)
        {
            if (string.IsNullOrWhiteSpace(collection)) return null;
            var list = new List<T>();

            if (!string.IsNullOrWhiteSpace(collection))
            {
                var args = collection.Split(delimiters.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (args.Length > 0)
                {
                    foreach (var arg in args)
                    {
                        if (arg == null) continue;
                        var a = arg.Trim();
                        if (a == "") continue;
                        var value = arg.Convert<T>();

                        list.Add(value);
                    }
                }
            }
            return list;
        }

        public static NameValueCollection ToQueryString(this string querystring)
        {
            return string.IsNullOrWhiteSpace(querystring) ? null : HttpUtility.ParseQueryString(querystring);
        }

        public static Dictionary<string, string> ToFieldDictionary(this string collection, string delimiters)
        {
            var values = collection.ToFieldCollection(delimiters);

            return values.Cast<string>().ToDictionary(k => k, v => values[v]);
        }

        public static NameValueList ToNameValueList(this string collection, string delimiters)
        {
            if (string.IsNullOrWhiteSpace(collection)) return null;
            var nv = new NameValueList();
            if (!string.IsNullOrEmpty(collection))
            {
                var args = collection.Split(delimiters.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (args.Length > 0)
                {
                    foreach (var arg in args)
                    {
                        if (arg == null) continue;
                        var a = arg.Trim();
                        if (a == "") continue;
                        var args2 = a.Split("=".ToCharArray());
                        var name = args2[0].Trim();
                        if (delimiters.Contains("&")) name = name.TrimStart('?');
                        if (args2.Length == 1)
                            nv.Add(HttpUtility.UrlDecode(name), null);
                        else if (args2.Length == 2)
                            nv.Add(HttpUtility.UrlDecode(name), HttpUtility.UrlDecode(args2[1].Trim()));
                    }
                }
            }
            return nv;
        }

        public static NameValueCollection ToFieldCollection(this string collection, string delimiters)
        {
            if (string.IsNullOrWhiteSpace(collection)) return null;
            var nv = new NameValueCollection(StringComparer.CurrentCultureIgnoreCase);
            if (!string.IsNullOrEmpty(collection))
            {
                var args = collection.Split(delimiters.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (args.Length > 0)
                {
                    foreach (var arg in args)
                    {
                        if (arg == null) continue;
                        var a = arg.Trim();
                        if (a == "") continue;
                        var args2 = a.Split("=".ToCharArray());
                        var name = args2[0].Trim();
                        if (delimiters.Contains("&")) name = name.TrimStart('?');
                        if (args2.Length == 1)
                            nv.Add(HttpUtility.UrlDecode(name), null);
                        else if (args2.Length == 2)
                            nv.Add(HttpUtility.UrlDecode(name), HttpUtility.UrlDecode(args2[1].Trim()));
                    }
                }
            }
            return nv;
        }

        public static NameValueCollection ToNameValueCollection(this Dictionary<string,string> dictionary)
        {
            return dictionary.Aggregate(new NameValueCollection(),(seed, current) => 
            {
                seed.Add(current.Key, current.Value);
                return seed;
            });
        }
        public static string ToDelimitedString<T>(this IEnumerable<T> list, string delimiter = ",", string appendage = null)
        {
            if (list == null) return null;
            string result = null;

            foreach (var item in list)
            {
                if (item == null) continue;
                var text = item.ToString();
                if (string.IsNullOrWhiteSpace(text)) continue;
                if (result != null && !string.IsNullOrEmpty(delimiter)) result += delimiter;
                result += text + appendage;
            }
            return result;
        }

        public static bool ContainsKey(this NameValueCollection collection, params string[] keys)
        {
            foreach (var key in keys)
                if (collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) return true;

            return false;
        }

        public static bool ContainsKey<T>(this Dictionary<string, T> dictionary, params string[] keys)
        {
            foreach (var key in keys)
                if (dictionary.ContainsKey(key)) return true;

            return false;
        }

        public static bool AddRange<T>(this Dictionary<string, T> target, Dictionary<string, T> source)
        {
            foreach (var key in source.Keys)
                target[key] = source[key];

            return false;
        }

        public static Dictionary<string, string> ToArguments(this string[] args, bool ignoreCase = true)
        {
            var arguments = new Dictionary<string, string>(ignoreCase ? StringComparer.InvariantCultureIgnoreCase : StringComparer.InvariantCulture);
            if (args != null)
                for (var i = 0; i < args.Length; i++)
                {
                    args[i] = args[i].TrimStartI("\" /\\-".ToCharArray()).TrimEndI('\"', ' ');

                    string key;
                    string value = null;
                    var i2 = args[i].IndexOfAny(":=".ToCharArray());
                    if (i2 < 1)
                        key = args[i];
                    else
                    {
                        key = args[i].Substring(0, i2);
                        value = args[i].Substring(i2 + 1);
                    }

                    arguments[key] = value.TrimStartI(":=".ToCharArray());
                }
            return arguments;
        }

        public static IEnumerable<string> TrimI(this IEnumerable<string> list, params char[] trimChars)
        {
            foreach (var item in list)
                yield return item.TrimI(trimChars);
        }

        public static IEnumerable<string> TrimStartI(this IEnumerable<string> list, params char[] trimChars)
        {
            foreach (var item in list)
                yield return item.TrimStartI(trimChars);
        }

        public static IEnumerable<string> TrimEndI(this IEnumerable<string> list, params char[] trimChars)
        {
            foreach (var item in list)
                yield return item.TrimEndI(trimChars);
        }

        public static string ToEncapsulatedString<T>(this IEnumerable<T> list, string prefix, string suffix, string seperator = null, string lastSeperator = null, bool allowDuplicates = true)
        {
            return list.ToList().ToEncapsulatedString(prefix, suffix, seperator, lastSeperator, allowDuplicates);
        }

        public static string ToEncapsulatedString<T>(this List<T> list, string prefix, string suffix, string seperator = null, string lastSeperator = null, bool allowDuplicates = true)
        {
            if (list == null) return null;
            string result = null;

            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var text = item.ToString();
                if (string.IsNullOrWhiteSpace(text)) continue;
                if (result != null) result += i == list.Count - 1 ? lastSeperator : seperator;
                var str = item.ToString();
                if (allowDuplicates || !str.StartsWithI(prefix)) result += prefix;
                result += str;
                if (allowDuplicates || !str.EndsWithI(suffix)) result += suffix;
            }
            return result;
        }

        public static string ToStyle(this Hashtable list)
        {
            if (list == null || list.Count < 1) return null;
            string result = null;

            foreach (string key in list.Keys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                var value = Convert.ToString(list[key]);
                if (!string.IsNullOrWhiteSpace(result)) result += ";";
                result += key + ": " + value;
            }
            return result;
        }

        public static NameValueElement[] ToNameValueElementArray(this NameValueCollection collection)
        {
            if (collection == null) return null;
            var results = new List<NameValueElement>();
            foreach (string key in collection.Keys)
            {
                results.Add(new NameValueElement(key, collection[key]));
            }
            return results.ToArray();
        }

        public static NameValueElement[] ToNameValueElementArray(this Dictionary<string, string> collection)
        {
            if (collection == null) return null;
            var results = new List<NameValueElement>();
            foreach (var key in collection.Keys)
            {
                results.Add(new NameValueElement(key, collection[key]));
            }
            return results.ToArray();
        }

        public static NameValueSetting[] ToNameValueSettingArray(this NameValueCollection collection)
        {
            if (collection == null) return null;
            var results = new List<NameValueSetting>();
            foreach (string key in collection.Keys)
            {
                results.Add(new NameValueSetting(key, collection[key]));
            }
            return results.ToArray();
        }

        public static KeyValuePair<string, object>[] ToKeyValuePairArray(this NameValueCollection collection)
        {
            if (collection == null || collection.Count < 1) return null;

            var namedItems = new List<KeyValuePair<string, object>>();

            //Declare all variables
            foreach (string key in collection.Keys)
                namedItems.Add(new KeyValuePair<string, object>(key, collection[key]));

            return namedItems.ToArray();
        }

        public static NameValueCollection ToNameValueCollection(this NameValueElement[] arrayNameValueElement)
        {
            if (arrayNameValueElement == null) return null;
            var results = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);
            foreach (var element in arrayNameValueElement)
            {
                results[element.Name] = element.Value;
            }
            return results;
        }

        public static Dictionary<string, string> ToDictionary(this NameValueCollection collection)
        {
            if (collection == null) return null;

            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in collection.Keys)
                parameters.Add(key, collection[key]);

            return parameters;
        }

        public static Dictionary<string, string> ToDictionary(this NameValueElement[] arrayNameValueElement)
        {
            if (arrayNameValueElement == null) return null;
            var results = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var element in arrayNameValueElement)
            {
                results[element.Name] = element.Value;
            }
            return results;
        }

        public static void Clear(this List<NameValueElement> collection, string name)
        {
            if (collection == null || collection.Count < 1) return;

            for (var i = collection.Count - 1; i > -1; i--)
            {
                if (collection[i].Name.EqualsI(name)) collection.RemoveAt(i);
            }
        }

        public static void Add(this List<NameValueElement> collection, string name, string value)
        {
            collection.Add(new NameValueElement(name, value));
        }

        public static bool Contains(this List<NameValueElement> collection, string name)
        {
            return collection.IndexOf(name) > -1;
        }

        public static bool Contains(this IEnumerable<string> target, IEnumerable<string> source, bool ignoreCase = true)
        {
            foreach (var item in source)
            {
                if (ignoreCase && !target.ContainsI(item)) return false;
                if (!ignoreCase && !target.Contains(item)) return false;
            }

            return target.Count() > 0;
        }

        public static string Set(this List<NameValueElement> collection, string name, string value)
        {
            var i = collection.IndexOf(name);
            if (i > -1)
                return collection[i].Value = value;
            collection.Add(name, value);

            return null;
        }

        public static string Find(this List<NameValueElement> collection, string name)
        {
            var i = collection.IndexOf(name);
            if (i > -1) return collection[i].Value;
            return null;
        }

        public static int IndexOf(this List<NameValueElement> collection, string name)
        {
            if (collection == null) return -1;
            for (var i = 0; i < collection.Count; i++)
            {
                if (collection[i].Name.EqualsI(name)) return i;
            }
            return -1;
        }

        public static int IndexOf(this List<Uri> collection, Uri uri, bool ignoreScheme)
        {
            for (var i = 0; i < collection.Count; i++)
            {
                if (!collection[i].Host.EqualsI(uri.Host)) continue;
                if (!collection[i].PathAndQuery.EqualsI(uri.PathAndQuery)) continue;
                if (ignoreScheme || !collection[i].Scheme.EqualsI(uri.Scheme)) continue;
                return i;
            }
            return -1;
        }

        public static NameValueCollection ToNameValueCollection(this NameValueSetting[] arrayNameValueSetting)
        {
            if (arrayNameValueSetting == null) return null;
            var results = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);
            foreach (var setting in arrayNameValueSetting)
            {
                results[setting.Name] = setting.Value;
            }
            return results;
        }

        public static void AddRange<T>(this HashSet<T> targetCollection, IEnumerable<T> collection)
        {
            foreach (var item in collection)
            {
                targetCollection.Add(item);
            }
        }
        public static void AddRange<T>(this SortedSet<T> targetCollection, IEnumerable<T> collection)
        {
            foreach (var item in collection)
            {
                targetCollection.Add(item);
            }
        }

        public static void AddRange(this Dictionary<string, NameValueCollection> targetCollection, Dictionary<string, NameValueCollection> collection)
        {
            foreach (var key in collection.Keys)
            {
                targetCollection[key] = collection[key];
            }
        }

        public static List<string> MergeI(this List<string> source, string[] collection, int length = -1)
        {
            var c = 0;
            if (collection != null)
                foreach (var item in collection)
                {
                    if (string.IsNullOrWhiteSpace(item)) continue;
                    if (source.ContainsI(item)) continue;

                    if (c < length || length == -1)
                    {
                        source.Add(item);
                        c++;
                    }
                }
            return source;
        }

        public static List<string> MergeI(this List<string> source, List<string> collection, int length = -1)
        {
            var c = 0;
            foreach (var item in collection)
            {
                if (string.IsNullOrWhiteSpace(item)) continue;
                if (source.ContainsI(item)) continue;

                if (c < length || length == -1)
                {
                    source.Add(item);
                    c++;
                }
            }
            return source;
        }

        public static List<string> RemoveI(this List<string> source, List<string> collection)
        {
            return source.RemoveI(collection.ToArray());
        }

        public static List<string> RemoveI(this List<string> source, params string[] collection)
        {
            foreach (var item in collection)
            {
                if (string.IsNullOrWhiteSpace(item)) continue;
                var i = source.IndexOfI(item);
                if (i > -1) source.RemoveAt(i);
            }
            return source;
        }

        public static bool Remove<T, T1>(this ConcurrentDictionary<T, T1> dictionary, T key)
        {
            T1 value;
            return dictionary.TryRemove(key, out value);
        }

        public static DataTable ToDataTable(this SerializableDictionary<string, string> fields, string tableName)
        {
            var table = new DataTable(tableName.ToValidFileName());
            //Add the columns
            foreach (var key in fields.Keys)
            {
                table.Columns.Add(key);
            }

            //Add the records
            var row = table.NewRow();
            foreach (var key in fields.Keys)
            {
                row[key] = fields[key];
            }
            table.Rows.Add(row);
            return table;
        }

        public static List<T> ToList<T>(this ICollection collection)
        {
            var list = new List<T>(collection.Count);

            list.AddRange(collection.Cast<T>());

            return list;
        }

        public static List<T> ToListOrNull<T>(this IEnumerable<T> collection)
        {
            if (collection == null) return null;

            return collection.ToList();
        }

        public static List<T> ToListOrEmpty<T>(this IEnumerable<T> collection)
        {
            if (collection == null) return new List<T>();

            return collection.ToList();
        }

        public static IEnumerable<T> Randomise<T>(this IList<T> list)
        {
            var indexes = Enumerable.Range(0, list.Count).ToArray();
            var generator = new Random();

            for (var i = 0; i < list.Count; ++i)
            {
                var position = generator.Next(i, list.Count);

                yield return list[indexes[position]];

                indexes[position] = indexes[i];
            }
        }

        public static T RandomPick<T>(this IList<T> list)
        {
            if (list == null || list.Count < 1) return default(T);
            if (list.Count == 1) return list[0];

            var generator = new Random();
            int index = generator.Next(0, list.Count - 1);
            return list[index];
        }

        public static IEnumerable<T> Page<T>(this IEnumerable<T> list, int pageSize, int page)
        {
            var skip = (page - 1)*pageSize;
            return list.Skip(skip).Take(pageSize);
        }

        public static IQueryable<T> Page<T>(this IQueryable<T> query, int pageSize, int page)
        {
            var skip = (page - 1)*pageSize;
            return query.Skip(skip).Take(pageSize);
        }

        public static IEnumerable<IEnumerable<T>> GetPermutations<T>(this IEnumerable<T> list, int length)
        {
            if (length == 1) return list.Select(t => new[] {t});

            return GetPermutations(list, length - 1)
                .SelectMany(t => list.Where(e => !t.Contains(e)),
                    (t1, t2) => t1.Concat(new[] {t2}));
        }

        public static TResult MaxOrDefault<T, TResult>(this IEnumerable<T> enumerable, Func<T, TResult> func)
        {
            return enumerable.Select(func).DefaultIfEmpty().Max();
        }
    }

    [Serializable]
    public class NameValueSetting
    {
        public NameValueSetting()
        {
        }

        public NameValueSetting(string name, string value)
        {
            Name = name;
            Value = value;
        }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }
    }

    [Serializable]
    public class NameValueElement
    {
        public NameValueElement()
        {
        }

        public NameValueElement(string name, string value)
        {
            Name = name;
            Value = value;
        }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlText]
        public string Value { get; set; }
    }
}