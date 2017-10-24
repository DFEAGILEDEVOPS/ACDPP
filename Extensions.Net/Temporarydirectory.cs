using System;
using System.IO;
using Extensions;

public sealed class TemporaryDirectory : IDisposable
{
    private static string _DefaultDirectory;

    public readonly DirectoryInfo Directory;

    public TemporaryDirectory() : this(Path.GetTempPath())
    {
    }

    public TemporaryDirectory(string parentDirectory)
    {
        var fullName = GetNewName(parentDirectory);

        Directory = System.IO.Directory.CreateDirectory(fullName);
    }

    public static string DefaultDirectory
    {
        get
        {
            if (_DefaultDirectory != null) return _DefaultDirectory;
            return FileSystem.GetTempPath();
        }
        set { _DefaultDirectory = value; }
    }

    public string FullName
    {
        get { return Directory.FullName; }
    }

    public void Dispose()
    {
        Delete();
        GC.SuppressFinalize(this);
    }

    ~TemporaryDirectory()
    {
        Delete();
    }

    private void Delete()
    {
        Directory.Delete(true);
    }

    public static string GetNewName(string parentDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(parentDirectory)) parentDirectory = DefaultDirectory;
        string fullName = null;
        do
        {
            var subDir = Path.GetRandomFileName();
            Path.ChangeExtension(subDir, "tmp");
            fullName = Path.Combine(parentDirectory, subDir);
        } while (System.IO.Directory.Exists(fullName));
        return fullName;
    }
}