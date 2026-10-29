static class Undo
{
    public sealed record Entry(string Type, string Style, string? Previous, bool Existed);

    private static string Dir => $"{Config.ConfigDir}/undo";

    private static string ActionFile => Path.Join(Dir, "action");

    private static string SnapshotDir => Path.Join(Dir, "snapshot");

    public static Entry? Read()
    {
        try
        {
            if (!File.Exists(ActionFile))
                return null;

            var values = new Dictionary<string, string>();

            foreach (var line in File.ReadAllLines(ActionFile))
            {
                string[] parts = line.Split('=', 2);

                if (parts.Length == 2)
                    values[parts[0]] = parts[1];
            }

            if (!values.TryGetValue("type", out var type) || !values.TryGetValue("style", out var style))
                return null;

            bool existed = values.GetValueOrDefault("existed") == "true";
            string previous = values.GetValueOrDefault("previous") ?? "";

            bool needsSnapshot = type == "set" || (type == "save" && existed);

            if (type != "set" && type != "save")
                return null;

            if (needsSnapshot && !Directory.Exists(SnapshotDir))
                return null;

            return new Entry(type, style, previous == "" ? null : previous, existed);
        }
        catch
        {
            return null;
        }
    }

    public static void BeginSet(string style)
    {
        string? previous = Styles.CurrentName();

        Reset();
        Directory.CreateDirectory(SnapshotDir);

        foreach (var component in Config.Components)
        {
            try
            {
                component.Save(SnapshotDir);
            }
            catch
            {
            }
        }

        Write("set", style, previous, false);
    }

    public static void BeginSave(string style, string path)
    {
        bool existed = Styles.IsStyle(path);
        string? previous = Styles.CurrentName();

        Reset();
        Directory.CreateDirectory(Dir);

        if (existed)
            MoveDirectory(path, SnapshotDir);

        Write("save", style, previous, existed);
    }

    public static void Forget(string style)
    {
        if (Read() is { Type: "save" } entry && (entry.Style == style || entry.Style.StartsWith(style + "/")))
            Reset();
    }

    public static void Perform()
    {
        var entry = Read();

        if (entry == null)
        {
            Cli.Fail("Nothing to undo.");
            return;
        }

        if (entry.Type == "set")
            UndoSet(entry);
        else
            UndoSave(entry);
    }

    private static void UndoSet(Entry entry)
    {
        StyleManager.ApplyComponents(SnapshotDir);
        RestoreCurrent(entry);
        Reset();

        Cli.Finish($"Undid loading {entry.Style}");
    }

    private static void UndoSave(Entry entry)
    {
        string path = Styles.PathOf(entry.Style);

        if (Directory.Exists(path) && !Styles.IsStyle(path) && !entry.Existed)
        {
            Cli.Fail($"'{entry.Style}' isn't a style anymore, so there's nothing to undo.");
            return;
        }

        Files.DeleteDirectory(path);

        if (entry.Existed)
        {
            Files.EnsureParent(path);
            MoveDirectory(SnapshotDir, path);
        }

        Styles.RemoveEmptyFolders();
        RestoreCurrent(entry);
        Reset();

        Cli.Finish(entry.Existed
            ? $"Restored the old version of {entry.Style}"
            : $"Removed {entry.Style}");
    }

    private static void RestoreCurrent(Entry entry)
    {
        if (entry.Previous != null && Styles.IsStyle(Styles.PathOf(entry.Previous)))
            Styles.SetCurrent(Styles.PathOf(entry.Previous));
        else
            Styles.ClearCurrent();
    }

    private static void Write(string type, string style, string? previous, bool existed)
    {
        File.WriteAllLines(ActionFile,
        [
            $"type={type}",
            $"style={style}",
            $"previous={previous ?? ""}",
            $"existed={(existed ? "true" : "false")}"
        ]);
    }

    private static void Reset() => Files.DeleteDirectory(Dir);

    private static void MoveDirectory(string source, string destination)
    {
        try
        {
            Directory.Move(source, destination);
        }
        catch (IOException)
        {
            Files.DeleteDirectory(destination);
            Files.CopyDirectory(source, destination);
            Files.DeleteDirectory(source);
        }
    }
}
