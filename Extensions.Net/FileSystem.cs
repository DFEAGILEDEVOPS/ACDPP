using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Extensions
{
    public static class FileSystem
    {

        public static IEnumerable<FileInfo> AllFilesIn(string path, bool recursively = false)
        {
            return new DirectoryInfo(path)
                .EnumerateFiles("*.*", recursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }
        public static IEnumerable<FileInfo> AllFilesInApplicationFolder()
        {
            var uri = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            return AllFilesIn(Path.GetDirectoryName(uri.LocalPath));
        }

        public static string GetTempPath()
        {
            var result = Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.Machine);
            if (string.IsNullOrWhiteSpace(result)) result = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
            if (!Directory.Exists(result)) Directory.CreateDirectory(result);
            return result;
        }

        public static bool ContainsExtension(this IEnumerable<string> extensions, string extension = null)
        {
            foreach (var ext in extensions)
            {
                if (ext.TrimStart('.').EqualsI(extension.TrimStart('.'))) return true;
            }
            return false;
        }

        public static IEnumerable<string> GetFilenames(this IEnumerable<string> fileNames, IEnumerable<string> extensions)
        {
            foreach (var fileName in fileNames)
            {
                var extension = Path.GetExtension(fileName);
                if (extensions == null || extensions.ContainsExtension(extension)) yield return fileName;
            }
        }

        public static IEnumerable<string> GetExtensions(this IEnumerable<string> fileNames, IEnumerable<string> extensions = null)
        {
            foreach (var fileName in fileNames)
            {
                var extension = Path.GetExtension(fileName);
                if (extensions == null || extensions.ContainsExtension(extension)) yield return extension;
            }
        }

        public static bool IsUnlocked(this FileInfo file)
        {
            // If the file can be opened for exclusive access it means that the file    
            // is no longer locked by another process.    
            try
            {
                using (var inputStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
        }

        public static DirectoryInfo ToDirectory(this string path)
        {
            path = ExpandLocalPath(path);

            // If the file can be opened for exclusive access it means that the file    
            // is no longer locked by another process.    
            try
            {
                var directory = new DirectoryInfo(path);
                CreateTree(directory);
                if (directory.Exists) return directory;
            }
            catch
            {
            }
            return null;
        }

        public static string FindFile(string fileNamePath)
        {
            if (File.Exists(fileNamePath)) return Path.GetFullPath(fileNamePath);
            string result = null;
            if (!string.IsNullOrWhiteSpace(Path.GetDirectoryName(fileNamePath))) return null;
            result = Path.Combine(Environment.CurrentDirectory, fileNamePath);
            if (File.Exists(result)) return result;
            result = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileNamePath);
            if (File.Exists(result)) return result;
            result = Path.Combine(Environment.SystemDirectory, fileNamePath);
            if (File.Exists(result)) return result;
            return null;
        }

        public static bool EqualsFilename(this string source, string target)
        {
            if (!string.IsNullOrWhiteSpace(source)) source = Path.GetFileName(source);
            if (!string.IsNullOrWhiteSpace(target)) target = Path.GetFileName(target);
            return source.EqualsI(target);
        }

        public static bool FileEqualsI(this string source, params string[] targets)
        {
            if (string.IsNullOrWhiteSpace(source) || targets == null || targets.Length < 1) return false;
            var sourceFile = new FileInfo(source);
            foreach (var target in targets)
            {
                if (string.IsNullOrWhiteSpace(target)) continue;
                var targetFile = new FileInfo(target);
                if (EqualsI(sourceFile, targetFile)) return true;
            }
            return false;
        }

        public static bool EqualsI(this FileInfo source, FileInfo target)
        {
            if (source == null && target == null) return true;
            if (source == null) return false;
            if (target == null) return false;
            return source.FullName.EqualsI(target.FullName);
        }

        public static bool EqualsI(this FileInfo source, string target)
        {
            return EqualsI(source, new FileInfo(target));
        }

        public static bool DirectoryEqualsI(this string source, string target)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target)) return false;
            var sourceDir = new DirectoryInfo(source.NormalizePath());
            var targetDir = new DirectoryInfo(target.NormalizePath());
            return EqualsI(sourceDir, targetDir);
        }

        public static bool EqualsI(this DirectoryInfo source, DirectoryInfo target)
        {
            if (source == null && target == null) return true;
            if (source == null) return false;
            if (target == null) return false;
            return source.FullName.EqualsI(target.FullName);
        }

        public static bool EqualsI(this DirectoryInfo source, string targetDir)
        {
            return EqualsI(source, new DirectoryInfo(targetDir));
        }

        public static string NormalizePath(this string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException("path");

            if (path.StartsWithI("file")) path = Path.GetFullPath(new Uri(path).LocalPath);
            return Uri.UnescapeDataString(path.TrimEnd('/', '\\', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        /// Condenses a full path down to one relative to the application path (or defaultPath)
        public static string StripLocalPath(string path, string defaultPath = null)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            if (string.IsNullOrWhiteSpace(defaultPath)) defaultPath = AppDomain.CurrentDomain.BaseDirectory;

            if (path.StartsWithI(defaultPath)) path = path.Substring(defaultPath.Length);
            while (path.StartsWithAny('\\', '/')) path = path.Substring(1);
            return path;
        }

        /// Expands a condensed path relative to the application path (or defaultPath) up to a full path
        public static string ExpandLocalPath(string path, string defaultPath = null)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            if (string.IsNullOrWhiteSpace(defaultPath)) defaultPath = AppDomain.CurrentDomain.BaseDirectory;
            path = path.Replace(@"/", @"\");
            path = path.Replace(@"~\", @".\");
            path = path.Replace(@"\\", @"\");

            if (path.StartsWith(@".\") || path.StartsWith(@"..\"))
            {
                var uri = new Uri(Path.Combine(defaultPath, path));
                return Path.GetFullPath(uri.LocalPath);
            }

            while (path.StartsWithAny('\\', '/')) path = path.Substring(1);
            if (!Path.IsPathRooted(path)) path = Path.Combine(defaultPath, path);
            return path;
        }

        public static void WriteToFile(string text, string filePath, bool overwrite)
        {
            //Delete the file if it already exists
            if (File.Exists(filePath) && overwrite) File.Delete(filePath);
            File.WriteAllText(filePath, text);
        }

        public static string ReadFromFile(string filePath)
        {
            //Return null if the file doesnt exist
            if (!File.Exists(filePath)) return null;
            return File.ReadAllText(filePath);
        }

        public static bool Exists(this DirectoryInfo directory, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            directory.Refresh();
            return directory.GetFiles(searchPattern, searchOption).Length > 1;
        }

        [DebuggerStepThrough]
        public static void Clear(this DirectoryInfo directory, string pattern = "*.*", bool recursive = true, bool deleteFolders = true, bool deleteFiles = true, bool ignoreErrors = true, bool inclusive = false)
        {
            if (directory == null || !directory.Exists) return;
            try
            {
                if (deleteFolders)
                    foreach (var dir in directory.GetDirectories(pattern, SearchOption.TopDirectoryOnly))
                        dir.Delete(recursive);

                if (deleteFiles)
                    foreach (var file in directory.GetFiles(pattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                        file.Delete();

                if (inclusive) directory.Delete();
            }
            catch (Exception ex)
            {
                if (!ignoreErrors) throw;
            }
        }

        public static DateTime GetLastFileAccess(this DirectoryInfo directory)
        {
            var result = DateTime.MinValue;
            var file = directory.GetFiles("*.*", SearchOption.AllDirectories).OrderByDescending(f => f.LastAccessTime).FirstOrDefault();
            if (file != null) result = file.LastAccessTime;
            return result;
        }

        public static void CreateTree(this DirectoryInfo directory)
        {
            if (directory == null) throw new ArgumentNullException("directory");
            directory.Refresh();
            if (directory.Exists) return;
            if (directory.Root.FullName == directory.FullName) return;
            if (directory.Parent != null)
            {
                directory.Parent.Refresh();
                if (!directory.Exists && directory.Parent.FullName != directory.Root.FullName) CreateTree(directory.Parent);
            }
            directory.Create();
            directory.Refresh();
        }

        public static void CreateTree(this string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath)) throw new ArgumentNullException("directoryPath");
            var directory = new DirectoryInfo(directoryPath);
            directory.Refresh();
            if (directory.Exists) return;
            if (directory.Root.FullName == directory.FullName) return;
            if (directory.Parent != null)
            {
                directory.Parent.Refresh();
                if (!directory.Exists && directory.Parent.FullName != directory.Root.FullName) CreateTree(directory.Parent);
            }
            directory.Create();
            directory.Refresh();
        }

        //Deletes all empty folders under the specified path
        public static void DeleteEmpty(this DirectoryInfo directory, bool recursive = true)
        {
            if (!directory.Exists) return;
            var files = directory.GetFiles("*.*", SearchOption.TopDirectoryOnly);
            var dirs = directory.GetDirectories("*.*", SearchOption.TopDirectoryOnly);
            if (dirs.Length > 0)
                foreach (var subDir in dirs)
                    subDir.DeleteEmpty(recursive);

            dirs = directory.GetDirectories("*.*", SearchOption.TopDirectoryOnly);

            if (files.Length == 0 && dirs.Length == 0)
                try
                {
                    directory.Delete();
                }
                catch
                {
                }
        }

        public static void DeleteOld(this DirectoryInfo directory, DateTime maxDate, bool recursive = true)
        {
            if (!directory.Exists) return;
            foreach (var file in directory.GetFiles("*.*", SearchOption.TopDirectoryOnly))
            {
                if (file.CreationTime < maxDate) file.Delete();
            }
            if (!recursive) return;

            var dirs = directory.GetDirectories("*.*", SearchOption.TopDirectoryOnly);
            foreach (var subDir in dirs)
                subDir.DeleteOld(maxDate, recursive);
        }

        public static void DeleteOld(this DirectoryInfo directory, TimeSpan age, bool recursive = true)
        {
            DeleteOld(directory, DateTime.Now + age, recursive);
        }
    }
}