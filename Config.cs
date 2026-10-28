using System.Text.Json;
using System.Text.Json.Serialization;

sealed class ComponentConfig
{
    public ComponentConfig() {}
    public string Name { get; set; } = "";
    public string Type { get; set; } = "copy";
    public string? Source { get; set; }
    public bool IsDirectory { get; set; }
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
        Console.WriteLine("Edit it to add your components, then run this command again.");
        Environment.Exit(0);
    }

    private static StyleComponent BuildComponent(ComponentConfig c) => c.Type switch
    {
        "copy" => new CopyComponent(c.Name, Expand(c.Source), c.IsDirectory, c.SaveHook, c.LoadHook),
        "script" => new ScriptComponent(c.Name, c.SaveHook, c.LoadHook),
        _ => throw new Exception($"Component '{c.Name}' has unknown type '{c.Type}' (expected \"copy\" or \"script\")")
    };

    private static AppSettings Load()
    {
        EnsureExists();

        try
        {
            string text = File.ReadAllText(ConfigPath);

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

    private const string DefaultConfig = """
{
  // Where saved styles live.
  "styleDir": "~/.config/hyprstyle/styles",

  // Every component gets saved into a style on save, and loaded back on
  // load. Add your own components below -- two ways:
  //
  // 1) "copy" -- copies a file or folder normally. Add saveHook / loadHook
  //    (shell commands) if the app needs extra commands afterwards, e.g. a
  //    reload signal. $STYLE_PATH and $COMPONENT are set as env vars
  //    when the hook runs:
  //
  //    { "name": "kitty", "type": "copy", "source": "~/.config/kitty",
  //      "isDirectory": true, "loadHook": "pkill -USR1 kitty" },
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
