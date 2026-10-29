using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

sealed class ComponentConfig
{
    public ComponentConfig() {}
    public string Name { get; set; } = "";
    public string Type { get; set; } = "copy";
    public string? Source { get; set; }
    public string? SaveHook { get; set; }
    public string? LoadHook { get; set; }
}

sealed class AppSettings
{
    public AppSettings() {}
    public string StyleDir { get; set; } = "~/.config/hyprstyle/styles";
    public List<ComponentConfig> Components { get; set; } = [];
    public List<string> HyprConfigComponents { get; set; } = [];
}

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(AppSettings))]
partial class AppJsonContext : JsonSerializerContext
{
}

static class Config
{
    public static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    public static readonly string ConfigDir = $"{Home}/.config/hyprstyle";
    public static readonly string ConfigPath = $"{ConfigDir}/config.json";

    private static AppSettings? settings;
    private static List<StyleComponent>? components;

    public static string StyleDir => Expand(Settings.StyleDir);

    public static HashSet<string> HyprConfigComponents => new(Settings.HyprConfigComponents);

    public static List<StyleComponent> Components => components ??= Settings.Components
        .Select(BuildComponent)
        .ToList();

    public static List<ComponentConfig> ComponentConfigs => Settings.Components;

    private static AppSettings Settings => settings ??= Load();

    public static string Expand(string? path) =>
        string.IsNullOrEmpty(path) ? "" :
        path.StartsWith('~') ? Home + path[1..] : path;

    public static void EnsureExists()
    {
        if (File.Exists(ConfigPath))
            return;

        Files.EnsureParent(ConfigPath);
        File.WriteAllText(ConfigPath, DefaultConfig);

        Console.WriteLine($"No config found -- wrote a starter one to:");
        Console.WriteLine($"  {ConfigPath}");
        Console.WriteLine();
        Console.WriteLine("Add your components in the GUI (hyprstyle gui, Components tab) or edit the file by hand,");
        Console.WriteLine("then run this command again.");
        Environment.Exit(0);
    }

    private static StyleComponent BuildComponent(ComponentConfig c) => c.Type switch
    {
        "copy" => new CopyComponent(c.Name, Expand(c.Source), c.SaveHook, c.LoadHook),
        "script" => new ScriptComponent(c.Name, c.SaveHook, c.LoadHook),
        _ => throw new Exception($"Component '{c.Name}' has unknown type '{c.Type}' (expected \"copy\" or \"script\")")
    };

    private static AppSettings Load()
    {
        EnsureExists();

        try
        {
            string text = DropRemovedOptions(File.ReadAllText(ConfigPath));

            return JsonSerializer.Deserialize(text, AppJsonContext.Default.AppSettings)
                ?? throw new Exception("Config file is empty.");
        }
        catch (JsonException e)
        {
            Console.WriteLine($"Couldn't parse {ConfigPath}:");
            Console.WriteLine($"  {e.Message}");
            Environment.Exit(1);
            throw;
        }
    }

    private static string DropRemovedOptions(string text)
    {
        string? cleaned = JsonText.RemoveProperty(text, "isDirectory");

        if (cleaned == null)
            return text;

        try
        {
            File.Copy(ConfigPath, ConfigPath + ".bak", overwrite: true);
            File.WriteAllText(ConfigPath, cleaned);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }

        return cleaned;
    }

    public static void SaveComponents(string json)
    {
        AppSettings? incoming;

        try
        {
            incoming = JsonSerializer.Deserialize(json, AppJsonContext.Default.AppSettings);
        }
        catch (JsonException)
        {
            Cli.Fail("Couldn't read the components.");
            return;
        }

        if (incoming == null)
        {
            Cli.Fail("Couldn't read the components.");
            return;
        }

        var entries = incoming.Components;

        foreach (var entry in entries)
        {
            entry.Name = entry.Name.Trim();
            entry.Type = entry.Type.Trim().ToLowerInvariant();
        }

        if (ComponentProblem(entries) is string problem)
        {
            Cli.Fail(problem);
            return;
        }

        var hyprNames = incoming.HyprConfigComponents
            .Where(name => entries.Any(c => c.Name == name))
            .Distinct()
            .ToList();

        EnsureExists();

        string text = File.ReadAllText(ConfigPath);
        string indent = JsonText.IndentOfTopLevelKey(text, "components");

        string? updated = JsonText.SetTopLevelProperty(text, "components", WriteComponents(entries, indent));

        if (updated != null)
            updated = JsonText.SetTopLevelProperty(updated, "hyprConfigComponents", WriteNames(hyprNames, indent));

        if (updated == null)
        {
            Cli.Fail($"Couldn't update {ConfigPath}. Is it valid JSON?");
            return;
        }

        try
        {
            File.Copy(ConfigPath, ConfigPath + ".bak", overwrite: true);
            File.WriteAllText(ConfigPath, updated);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Cli.Fail($"Couldn't write {ConfigPath}: {e.Message}");
            return;
        }

        Console.WriteLine(entries.Count == 1 ? "Saved 1 component" : $"Saved {entries.Count} components");
    }

    public static string? ComponentProblem(List<ComponentConfig> entries)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (entry.Name == "")
                return "Every component needs a name.";

            if (entry.Name.Contains('/') || entry.Name.StartsWith('.'))
                return $"'{entry.Name}' can't contain a slash or start with a dot.";

            if (!seen.Add(entry.Name))
                return $"There's more than one component called '{entry.Name}'.";

            if (entry.Type is not ("copy" or "script"))
                return $"'{entry.Name}' has an unknown type '{entry.Type}'.";
        }

        return null;
    }

    private static string WriteComponents(List<ComponentConfig> entries, string indent) =>
        WriteIndented(json =>
        {
            json.WriteStartArray();

            foreach (var entry in entries)
            {
                json.WriteStartObject();
                json.WriteString("name", entry.Name);
                json.WriteString("type", entry.Type);

                if (entry.Type == "copy")
                    json.WriteString("source", entry.Source?.Trim() ?? "");

                if (!string.IsNullOrWhiteSpace(entry.SaveHook))
                    json.WriteString("saveHook", entry.SaveHook.Trim());

                if (!string.IsNullOrWhiteSpace(entry.LoadHook))
                    json.WriteString("loadHook", entry.LoadHook.Trim());

                json.WriteEndObject();
            }

            json.WriteEndArray();
        }, indent);

    private static string WriteNames(List<string> names, string indent) =>
        WriteIndented(json =>
        {
            json.WriteStartArray();

            foreach (var name in names)
                json.WriteStringValue(name);

            json.WriteEndArray();
        }, indent);

    private static string WriteIndented(Action<Utf8JsonWriter> write, string indent)
    {
        using var stream = new MemoryStream();

        var options = new JsonWriterOptions
        {
            Indented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using (var json = new Utf8JsonWriter(stream, options))
            write(json);

        return Encoding.UTF8.GetString(stream.ToArray()).Replace("\n", "\n" + indent);
    }

    private const string DefaultConfig = """
{
  // Where saved styles live.
  "styleDir": "~/.config/hyprstyle/styles",

  // Everything a style is made of. The easiest way to manage these is the
  // Components tab in the GUI (run: hyprstyle gui), but you can also edit
  // them right here. Every component gets saved into a style on save, and
  // loaded back on load. There are two kinds:
  //
  // 1) "copy" -- copies a file or folder into the style, and puts it back
  //    on load. Add a saveHook / loadHook (shell commands) if the app needs
  //    something extra afterwards, e.g. a reload signal. $STYLE_PATH and
  //    $COMPONENT are set as env vars when the hook runs:
  //
  //    { "name": "kitty", "type": "copy", "source": "~/.config/kitty",
  //      "loadHook": "pkill -USR1 kitty" },
  //
  // 2) "script" -- for anything a simple copy can't handle, e.g. a
  //    program that only remembers which theme is "active" rather
  //    than storing the theme file itself. saveHook and loadHook are
  //    shell commands responsible for the whole operation -- no default save/load;
  //    use $STYLE_PATH to read/write inside the style folder:
  //
  //    { "name": "myapp", "type": "script",
  //      "saveHook": "cp ~/.config/myapp/active.theme $STYLE_PATH/myapp.theme",
  //      "loadHook": "cp $STYLE_PATH/myapp.theme ~/.config/myapp/active.theme && pkill -USR1 myapp" },
  //
  // A "copy" component's source can be left as "" to disable it
  // without deleting the line.
  "components": [

  ],

  // If any component above is one of Hyprland's own config files (e.g.
  // ~/.config/hypr/hyprland.lua), list its name here (as a JSON string, matching "name" above):
  //
  //    "components": [
  //        { "name": "hyprland.lua", "type": "copy",
  //        "source": "~/.config/hypr/hyprland.lua" },
  //    ]
  //
  //    "hyprConfigComponents": [ "hyprland.lua" ]
  //
  // Those load first and get Hyprland reloaded before everything else,
  // so it's never left mid-reload while the rest of the style applies.
  "hyprConfigComponents": [

  ]
}
""";
}
