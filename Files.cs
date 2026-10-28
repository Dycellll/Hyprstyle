static class Files
{
    public static void EnsureParent(string path)
    {
        var parent = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);
    }

    public static void CopyFile(string source, string destination)
    {
        EnsureParent(destination);

        string tempPath = destination + ".tmp";

        File.Copy(source, tempPath, overwrite: true);
        File.Move(tempPath, destination, overwrite: true);
    }

    public static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, directory);
            Directory.CreateDirectory(Path.Join(destination, relative));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var destinationFile = Path.Join(destination, relative);

            EnsureParent(destinationFile);
            File.Copy(file, destinationFile, overwrite: true);
        }
    }

    public static void ReplaceDirectory(string source, string destination)
    {
        string parent = Path.GetDirectoryName(destination)
            ?? throw new Exception($"No parent directory for {destination}");

        string name = Path.GetFileName(destination);
        string stagingDir = Path.Join(parent, $".{name}.new");
        string backupDir = Path.Join(parent, $".{name}.old");

        DeleteDirectory(stagingDir);
        DeleteDirectory(backupDir);

        CopyDirectory(source, stagingDir);

        if (Directory.Exists(destination))
            Directory.Move(destination, backupDir);

        Directory.Move(stagingDir, destination);

        DeleteDirectory(backupDir);
    }

    public static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }

    public static void DeleteFile(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
