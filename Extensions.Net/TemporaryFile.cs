using System;
using System.Diagnostics;
using System.IO;

namespace FreshSkies.mkryptor
{
    public sealed class TemporaryFile : IDisposable
    {
        public readonly string Directory;
        public readonly bool ForceDelete = true;

        public TemporaryFile(string directory = null, string fileName = null, string extension = null, bool createFile = true, bool forceDelete = true)
        {
            if (string.IsNullOrWhiteSpace(directory)) directory = TemporaryDirectory.DefaultDirectory;
            Directory = directory;
            ForceDelete = forceDelete;
            if (forceDelete || string.IsNullOrWhiteSpace(fileName))
                FullName = GetNewName(directory, fileName, extension);
            else
                FullName = Path.Combine(directory, fileName);

            if (createFile) Create();
        }

        public string FullName { get; private set; }

        public string Name
        {
            get { return Path.GetFileName(FullName); }
        }

        public void Dispose()
        {
            Delete();
            GC.SuppressFinalize(this);
        }

        ~TemporaryFile()
        {
            Delete();
        }

        private void Create()
        {
            if (File.Exists(FullName)) File.Delete(FullName);
            using (File.Create(FullName))
            {
            }
            ;
        }

        public static string GetNewName(string directory = null, string fileName = null, string extension = null)
        {
            if (string.IsNullOrWhiteSpace(directory)) directory = TemporaryDirectory.DefaultDirectory;
            string name = null;
            string fullName = null;
            if (string.IsNullOrWhiteSpace(fileName))
            {
                do
                {
                    name = Path.GetRandomFileName();
                    if (!string.IsNullOrWhiteSpace(extension)) name = Path.ChangeExtension(name, extension);
                    fullName = Path.Combine(directory, name);
                } while (File.Exists(fullName));
            }
            else
            {
                fullName = Path.Combine(directory, fileName);
                extension = Path.GetExtension(fileName).TrimStart(' ', '.');
                fileName = Path.GetFileNameWithoutExtension(fileName);

                var c = 1;
                while (File.Exists(fullName))
                {
                    name = fileName + " " + c.ToString("000");
                    if (!string.IsNullOrWhiteSpace(extension)) name += "." + extension;
                    fullName = Path.Combine(directory, name);
                }
            }

            return fullName;
        }

        [DebuggerStepThrough]
        private void Delete()
        {
            if (FullName == null) return;

            try
            {
                File.Delete(FullName);
            }
            catch (Exception ex)
            {
                if (ForceDelete) throw;
            }
            FullName = null;
        }
    }
}