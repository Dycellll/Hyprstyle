using System.Text;
using System.Text.Json;

static class StyleManager
{
    private static string DisplayName(string style)
    {
        var aliases = Styles.ReadAliases(style);
        string name = Path.GetFileName(style);

        string label = aliases.Count == 0 ? name : $"{name} ({string.Join(", ", aliases)})";
        string description = Styles.ReadDescription(style);

        return description == "" ? label : $"{label} - {description}";
    }

    public static void ListStyles()
    {
        var styles = Styles.FindStyles();

        if (styles.Count == 0)
        {
            Console.WriteLine("No styles found.");
            return;
        }

        string? current = Styles.CurrentPath();

        Console.WriteLine();
        Console.WriteLine("╭─ Available styles");

        var grouped = new Dictionary<string, List<string>>();

        foreach (var style in styles)
        {
            string[] parts = Styles.NameOf(style).Split(Path.DirectorySeparatorChar);
            string folder = parts.Length == 1 ? "" : parts[0];

            if (!grouped.ContainsKey(folder))
                grouped[folder] = [];

            grouped[folder].Add(style);
        }

        foreach (var folder in grouped.Keys.Where(x => x != "").OrderBy(x => x))
        {
            Console.WriteLine("│");
            Console.WriteLine($"├─ 📁 {folder}");

            var entries = grouped[folder];

            for (int i = 0; i < entries.Count; i++)
            {
                string branch = i == entries.Count - 1 ? "└─" : "├─";
                string marker = Path.GetFullPath(entries[i]) == current ? "★" : " ";

                Console.WriteLine($"│  {branch} {marker} {DisplayName(entries[i])}");
            }
        }

        Console.WriteLine("│");

        if (grouped.TryGetValue("", out var rootStyles))
        {
            foreach (var style in rootStyles)
            {
                string marker = Path.GetFullPath(style) == current ? "★" : " ";
                Console.WriteLine($"│ {marker} {DisplayName(style)}");
            }
        }

        Console.WriteLine("│");
        Console.WriteLine("╰────────────────");
    }

    public static void SaveStyle(string name)
    {
        if (!Styles.IsValidName(name))
        {
            Cli.Fail($"'{name}' isn't a valid style name.");
            return;
        }

        if (Styles.AliasOwner(name) is string owner)
        {
            Cli.Fail($"'{name}' is already an alias of '{Styles.NameOf(owner)}'.");
            return;
        }

        if (Styles.ParentStyle(name) is string parent)
        {
            Cli.Fail($"'{parent}' is a style, so it can't also be a folder.");
            return;
        }

        string path = Styles.PathOf(name);

        bool blocked = File.Exists(path) ||
            (Directory.Exists(path) && !Styles.IsStyle(path) && Directory.EnumerateFileSystemEntries(path).Any());

        if (blocked)
        {
            Cli.Fail($"'{name}' already exists and isn't a style.");
            return;
        }

        var aliases = Styles.ReadAliases(path);
        string description = Styles.ReadDescription(path);

        Timing.Measure("undo snapshot", () => Undo.BeginSave(name, path));

        Timing.Measure("prepare style folder", () =>
        {
            Files.DeleteDirectory(path);
            Directory.CreateDirectory(path);
            File.Create(Path.Join(path, Styles.Marker)).Dispose();
            Styles.WriteAliases(path, aliases);
            Styles.WriteDescription(path, description);
        });

        int total = Config.Components.Count;
        int done = 0;

        foreach (var component in Config.Components)
        {
            try
            {
                Cli.Progress("Saving", component.Name, ++done, total);
                Timing.Measure($"  save {component.Name}", () => component.Save(path));
            }
            catch (Exception e)
            {
                Cli.Error($"Failed {component.Name}: {e.Message}");
            }
        }

        Timing.Measure("preview", () => Previews.Capture(path));
        Timing.Measure("set current", () => Styles.SetCurrent(path));
        Cli.Finish($"Saved style: {name}");
    }

    private static void LoadComponent(StyleComponent component, string path, int number, int total, List<string> failures)
    {
        try
        {
            Cli.Progress("Applying", component.Name, number, total);
            Timing.Measure($"  load {component.Name}", () => component.Load(path));
        }
        catch (Exception e)
        {
            string message = $"Failed {component.Name}: {e.Message}";

            Cli.Error(message);

            lock (failures)
                failures.Add(message);
        }
    }

    public static void SetStyle(string name)
    {
        string? path = Styles.FindStyle(name);

        if (path == null)
        {
            Cli.Fail($"Style '{name}' does not exist.");
            return;
        }

        Timing.Measure("undo snapshot", () => Undo.BeginSet(Styles.NameOf(path)));
        Timing.Measure("set current", () => Styles.SetCurrent(path));
        ApplyComponents(path);

        Cli.Finish($"Applied style: {Styles.NameOf(path)}");
    }

    public static void ApplyComponents(string path)
    {
        using var transition = Config.Components.Count > 0 ? Timing.Measure("transition start", Transition.Begin) : null;

        var hyprNames = Config.HyprConfigComponents;
        var hyprFirst = Config.Components.Where(c => hyprNames.Contains(c.Name)).ToList();
        var hyprRest = Config.Components.Where(c => !hyprNames.Contains(c.Name)).ToList();

        int total = Config.Components.Count;
        int done = 0;
        int finished = 0;
        var failures = new List<string>();

        void Load(StyleComponent component)
        {
            LoadComponent(component, path, Interlocked.Increment(ref done), total, failures);
            transition?.Progress(Interlocked.Increment(ref finished), total);
        }

        bool single = Config.SingleHyprReload;

        // Hyprland config files first, so the reload below sees all of them.
        Timing.Measure("hypr config components", () => Concurrent.ForEach(hyprFirst, Load));

        // Nothing Hyprland reads changed, so there's nothing to reload yet.
        if (!single && hyprFirst.Count > 0)
            Timing.Measure("hyprctl reload #1", Hyprland.Reload);

        Timing.Measure("other components", () => Concurrent.ForEach(hyprRest, Load));

        if (Config.Components.Count > 0)
            Timing.Measure("hyprctl reload #2", Hyprland.Reload);

        transition?.Reveal();
        Timing.Mark("hooks done");

        // Failures are always shown on the CLI (above); also surface them as notifications.
        if (failures.Count > 0)
            Cli.NotifyAll(failures.Select(f => $"hyprstyle: {f}").ToList());
    }

    public static void ReloadStyle()
    {
        string? name = Styles.CurrentName();

        if (name == null)
        {
            Cli.Fail("No style selected.");
            return;
        }

        SetStyle(name);
    }

    public static void RenameStyle(string name, string newName)
    {
        string? path = Styles.FindStyle(name);

        if (path == null)
        {
            Cli.Fail($"Style '{name}' does not exist.");
            return;
        }

        if (!Styles.IsValidName(newName))
        {
            Cli.Fail($"'{newName}' isn't a valid style name.");
            return;
        }

        string target = Styles.PathOf(newName);

        if (File.Exists(target) || Directory.Exists(target))
        {
            Cli.Fail($"'{newName}' already exists.");
            return;
        }

        if (Styles.AliasOwner(newName) is string owner)
        {
            Cli.Fail($"'{newName}' is already an alias of '{Styles.NameOf(owner)}'.");
            return;
        }

        if (Styles.ParentStyle(newName) is string parent)
        {
            Cli.Fail($"'{parent}' is a style, so it can't also be a folder.");
            return;
        }

        if (target.StartsWith(path + Path.DirectorySeparatorChar))
        {
            Cli.Fail($"Can't move '{Styles.NameOf(path)}' inside itself.");
            return;
        }

        bool wasCurrent = Styles.IsCurrent(path);

        Undo.Forget(Styles.NameOf(path));
        Files.EnsureParent(target);
        Directory.Move(path, target);

        if (wasCurrent)
            Styles.SetCurrent(target);

        Styles.RemoveEmptyFolders();
        Console.WriteLine($"Renamed {Styles.NameOf(path)} to {newName}");
    }

    public static void AddAlias(string name, string alias)
    {
        string? path = Styles.FindStyle(name);

        if (path == null)
        {
            Cli.Fail($"Style '{name}' does not exist.");
            return;
        }

        if (!Styles.IsValidAlias(alias))
        {
            Cli.Fail("Aliases can't be empty, start with a dot, or contain spaces or slashes.");
            return;
        }

        if (Styles.AliasOwner(alias) is string owner)
        {
            Cli.Fail(Path.GetFullPath(owner) == Path.GetFullPath(path)
                ? $"'{Styles.NameOf(path)}' already has the alias '{alias}'."
                : $"'{alias}' is already an alias of '{Styles.NameOf(owner)}'.");
            return;
        }

        if (Styles.IsStyle(Styles.PathOf(alias)))
        {
            Cli.Fail($"'{alias}' is already the name of a style.");
            return;
        }

        var aliases = Styles.ReadAliases(path);
        aliases.Add(alias);
        Styles.WriteAliases(path, aliases);

        Console.WriteLine($"Added alias '{alias}' to {Styles.NameOf(path)}");
    }

    public static void RemoveAlias(string name, string alias)
    {
        string? path = Styles.FindStyle(name);

        if (path == null)
        {
            Cli.Fail($"Style '{name}' does not exist.");
            return;
        }

        var aliases = Styles.ReadAliases(path);

        if (!aliases.Remove(alias))
        {
            Cli.Fail($"'{Styles.NameOf(path)}' has no alias '{alias}'.");
            return;
        }

        Styles.WriteAliases(path, aliases);
        Console.WriteLine($"Removed alias '{alias}' from {Styles.NameOf(path)}");
    }

    public static void SetDescription(string name, string text)
    {
        string? path = Styles.FindStyle(name);

        if (path == null)
        {
            Cli.Fail($"Style '{name}' does not exist.");
            return;
        }

        string description = Styles.CleanDescription(text);

        if (description.Length > Styles.MaxDescriptionLength)
        {
            Cli.Fail($"Descriptions can be at most {Styles.MaxDescriptionLength} characters, yours is {description.Length}.");
            return;
        }

        Styles.WriteDescription(path, description);

        Console.WriteLine(description == ""
            ? $"Removed the description from {Styles.NameOf(path)}"
            : $"Updated the description of {Styles.NameOf(path)}");
    }

    public static void UndoLast() => Undo.Perform();

    public static void DeleteStyle(string name)
    {
        name = name.TrimEnd('/');

        if (!Styles.IsValidName(name))
        {
            Cli.Fail($"'{name}' isn't a valid style name.");
            return;
        }

        string path = Styles.PathOf(name);

        if (!Directory.Exists(path))
        {
            Cli.Fail($"Style '{name}' does not exist.");
            return;
        }

        if (Styles.CurrentIsInside(path))
            Styles.ClearCurrent();

        Undo.Forget(name);

        bool isStyle = Styles.IsStyle(path);

        Directory.Delete(path, true);
        Styles.RemoveEmptyFolders();

        Console.WriteLine($"Deleted {(isStyle ? "style" : "folder")}: {name}");
    }

    public static void CurrentStyle()
    {
        string? name = Styles.CurrentName();

        Console.WriteLine(name == null ? "" : Path.GetFileName(name));
    }

    public static void PrintJson()
    {
        string? current = Styles.CurrentName();
        string? latest = Updater.LatestKnown;
        var styles = Styles.FindStyles();

        using var stream = new MemoryStream();

        using (var json = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteString("version", Updater.CurrentVersion);

            if (latest != null)
                json.WriteString("latestVersion", Updater.Clean(latest));
            else
                json.WriteNull("latestVersion");

            json.WriteBoolean("updateAvailable", latest != null && Updater.IsNewer(latest, Updater.CurrentVersion));
            json.WriteString("styleDir", Config.StyleDir);
            json.WriteString("configPath", Config.ConfigPath);
            json.WriteString("transition", Config.Transition);
            json.WriteNumber("transitionMs", Config.TransitionMs);

            if (current != null)
                json.WriteString("current", current);
            else
                json.WriteNull("current");

            if (Undo.Read() is { } undo)
            {
                json.WriteStartObject("undo");
                json.WriteString("type", undo.Type);
                json.WriteString("style", undo.Style);
                json.WriteEndObject();
            }
            else
            {
                json.WriteNull("undo");
            }

            json.WriteStartArray("folders");

            foreach (var folder in styles
                .Select(s => Path.GetDirectoryName(Styles.NameOf(s)) ?? "")
                .Where(f => f != "")
                .Distinct()
                .OrderBy(f => f))
            {
                json.WriteStringValue(folder);
            }

            json.WriteEndArray();

            json.WriteStartArray("styles");

            foreach (var style in styles)
            {
                string name = Styles.NameOf(style);
                string previewPath = Path.Join(style, Styles.PreviewFile);

                json.WriteStartObject();
                json.WriteString("name", name);
                json.WriteString("displayName", Path.GetFileName(style));
                json.WriteString("folder", Path.GetDirectoryName(name) ?? "");
                json.WriteString("path", style);
                json.WriteBoolean("current", name == current);
                json.WriteString("description", Styles.ReadDescription(style));

                json.WriteStartArray("aliases");

                foreach (var alias in Styles.ReadAliases(style))
                    json.WriteStringValue(alias);

                json.WriteEndArray();

                if (File.Exists(previewPath))
                    json.WriteString("preview", previewPath);
                else
                    json.WriteNull("preview");

                json.WriteString("savedAt", File.GetLastWriteTimeUtc(Path.Join(style, Styles.Marker)));
                json.WriteEndObject();
            }

            json.WriteEndArray();

            json.WriteStartArray("components");

            foreach (var component in Config.Components)
                json.WriteStringValue(component.Name);

            json.WriteEndArray();

            json.WriteStartArray("componentConfigs");

            foreach (var component in Config.ComponentConfigs)
            {
                json.WriteStartObject();
                json.WriteString("name", component.Name);
                json.WriteString("type", component.Type);
                json.WriteString("source", component.Source);
                json.WriteString("saveHook", component.SaveHook);
                json.WriteString("loadHook", component.LoadHook);
                json.WriteBoolean("hyprConfig", Config.HyprConfigComponents.Contains(component.Name));
                json.WriteEndObject();
            }

            json.WriteEndArray();

            json.WriteStartArray("changelogs");

            foreach (var entry in Changelogs.Released)
            {
                json.WriteStartObject();
                json.WriteString("version", $"v{entry.Version}");
                json.WriteString("log", entry.Log);
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        Console.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));
    }
}
