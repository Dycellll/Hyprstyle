static class Styles
{
    public const string Marker = ".style";
    public const string PreviewFile = ".preview.png";
    private const string AliasesFile = ".aliases";

    private static string CurrentLink => Path.Join(Config.StyleDir, "current");

    public static string PathOf(string name) => Path.Join(Config.StyleDir, name);

    public static string NameOf(string path) => Path.GetRelativePath(Config.StyleDir, path);

    public static bool IsStyle(string path) => File.Exists(Path.Join(path, Marker));

    public static bool IsValidName(string name) =>
        !string.IsNullOrWhiteSpace(name) &&
        !Path.IsPathRooted(name) &&
        name != "current" &&
        !name.Split('/').Any(part => part == "" || part.StartsWith('.'));

    public static bool IsValidAlias(string alias) =>
        !string.IsNullOrWhiteSpace(alias) &&
        !alias.StartsWith('.') &&
        !alias.Any(c => char.IsWhiteSpace(c) || c == '/');

    public static string? ParentStyle(string name)
    {
        var parts = name.Split('/');

        for (int i = 1; i < parts.Length; i++)
        {
            string parent = string.Join('/', parts[..i]);

            if (IsStyle(PathOf(parent)))
                return parent;
        }

        return null;
    }

    public static List<string> FindStyles()
    {
        var found = new List<string>();

        if (Directory.Exists(Config.StyleDir))
            Walk(Config.StyleDir, Path.GetFullPath(CurrentLink), found);

        found.Sort(StringComparer.Ordinal);
        return found;
    }

    private static void Walk(string directory, string skip, List<string> found)
    {
        foreach (var sub in Directory.EnumerateDirectories(directory))
        {
            if (Path.GetFullPath(sub) == skip)
                continue;

            if (IsStyle(sub))
                found.Add(sub);
            else
                Walk(sub, skip, found);
        }
    }

    public static string? FindStyle(string nameOrAlias)
    {
        string direct = PathOf(nameOrAlias);

        return IsStyle(direct) ? direct : AliasOwner(nameOrAlias);
    }

    public static string? AliasOwner(string alias) =>
        FindStyles().FirstOrDefault(style => ReadAliases(style).Contains(alias));

    public static List<string> ReadAliases(string stylePath)
    {
        string file = Path.Join(stylePath, AliasesFile);

        if (!File.Exists(file))
            return [];

        return File.ReadAllLines(file)
            .Select(line => line.Trim())
            .Where(line => line != "")
            .ToList();
    }

    public static void WriteAliases(string stylePath, List<string> aliases)
    {
        string file = Path.Join(stylePath, AliasesFile);

        if (aliases.Count == 0)
            File.Delete(file);
        else
            File.WriteAllLines(file, aliases);
    }

    public static string? CurrentPath()
    {
        try
        {
            string? target = new DirectoryInfo(CurrentLink).LinkTarget;

            if (target == null)
                return null;

            string full = Path.GetFullPath(
                Path.IsPathRooted(target) ? target : Path.Join(Config.StyleDir, target));

            return IsStyle(full) ? full : null;
        }
        catch
        {
            return null;
        }
    }

    public static string? CurrentName() =>
        CurrentPath() is string path ? NameOf(path) : null;

    public static bool IsCurrent(string path) =>
        CurrentPath() is string current && current == Normalize(path);

    public static bool CurrentIsInside(string path)
    {
        string root = Normalize(path);

        return CurrentPath() is string current &&
            (current == root || current.StartsWith(root + Path.DirectorySeparatorChar));
    }

    public static void SetCurrent(string path)
    {
        ClearCurrent();
        Directory.CreateSymbolicLink(CurrentLink, Normalize(path));
    }

    public static void ClearCurrent() => File.Delete(CurrentLink);

    public static void RemoveEmptyFolders() => RemoveEmptyFolders(Config.StyleDir);

    private static void RemoveEmptyFolders(string directory)
    {
        foreach (var sub in Directory.GetDirectories(directory))
        {
            if (new DirectoryInfo(sub).LinkTarget != null || IsStyle(sub))
                continue;

            RemoveEmptyFolders(sub);

            if (!Directory.EnumerateFileSystemEntries(sub).Any())
                Directory.Delete(sub);
        }
    }

    private static string Normalize(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
}
