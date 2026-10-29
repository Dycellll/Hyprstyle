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

        // Keep the source mtime so later copies can recognise "unchanged" by size + mtime.
        File.SetLastWriteTimeUtc(tempPath, File.GetLastWriteTimeUtc(source));
        File.Move(tempPath, destination, overwrite: true);
    }

    private static bool Unchanged(FileInfo source, string destination)
    {
        try
        {
            var target = new FileInfo(destination);

            return target.Exists &&
                target.LinkTarget == null &&
                target.Length == source.Length &&
                target.LastWriteTimeUtc == source.LastWriteTimeUtc;
        }
        catch
        {
            return false;
        }
    }

    // Copies a file only if size or mtime differ.
    public static void CopyFileIfChanged(string source, string destination)
    {
        if (!Unchanged(new FileInfo(source), destination))
            CopyFile(source, destination);
    }

    private static int CopyParallelism => Math.Max(Environment.ProcessorCount, 2);

    // Mirrors source into destination: copies new/changed files in parallel, skips unchanged ones.
    // Returns the set of relative paths that exist in source (for pruning).
    private static void MirrorInto(string source, string destination, out HashSet<string> files, out HashSet<string> dirs)
    {
        Directory.CreateDirectory(destination);

        var fileSet = new HashSet<string>(StringComparer.Ordinal);
        var dirSet = new HashSet<string>(StringComparer.Ordinal);
        var work = new List<(string Source, string Target)>();

        foreach (var entry in new DirectoryInfo(source).EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(source, entry.FullName);

            if (entry is DirectoryInfo)
            {
                dirSet.Add(relative);
                Directory.CreateDirectory(Path.Join(destination, relative));
            }
            else
            {
                fileSet.Add(relative);
                work.Add((entry.FullName, Path.Join(destination, relative)));
            }
        }

        Parallel.ForEach(
            work,
            new ParallelOptions { MaxDegreeOfParallelism = CopyParallelism },
            item => CopyFileIfChanged(item.Source, item.Target));

        files = fileSet;
        dirs = dirSet;
    }

    public static void CopyDirectory(string source, string destination) =>
        MirrorInto(source, destination, out _, out _);

    // Replaces destination's contents with source's, touching only what differs.
    // Falls back to the old stage-and-swap when destination is a symlink or a plain file.
    public static void ReplaceDirectory(string source, string destination)
    {
        var info = new DirectoryInfo(destination);

        if (!info.Exists || info.LinkTarget != null)
        {
            ReplaceDirectorySwap(source, destination);
            return;
        }

        HashSet<string> files, dirs;

        try
        {
            MirrorInto(source, destination, out files, out dirs);
        }
        catch (IOException)
        {
            // Something at the destination clashes with the style (a file where a folder should
            // be, a symlink, ...). Use the original stage-and-swap, which replaces it wholesale.
            ReplaceDirectorySwap(source, destination);
            return;
        }

        // Drop anything that isn't in the style anymore (the swap used to do this implicitly).
        foreach (var entry in info.EnumerateFileSystemInfos("*", SearchOption.AllDirectories).ToList())
        {
            string relative = Path.GetRelativePath(destination, entry.FullName);

            try
            {
                if (entry is DirectoryInfo dir)
                {
                    if (!dirs.Contains(relative) && dir.Exists)
                        dir.Delete(recursive: true);
                }
                else if (!files.Contains(relative) && entry.Exists)
                {
                    entry.Delete();
                }
            }
            catch (DirectoryNotFoundException)
            {
            }
        }
    }

    private static void ReplaceDirectorySwap(string source, string destination)
    {
        string parent = Path.GetDirectoryName(destination)
            ?? throw new Exception($"No parent directory for {destination}");

        string name = Path.GetFileName(destination);
        string stagingDir = Path.Join(parent, $".{name}.new");
        string backupDir = Path.Join(parent, $".{name}.old");

        DeleteDirectory(stagingDir);
        DeleteDirectory(backupDir);

        CopyDirectory(source, stagingDir);

        if (Directory.Exists(destination) || new DirectoryInfo(destination).LinkTarget != null)
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
