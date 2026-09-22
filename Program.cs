using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    public static readonly string ConfigPath = $"{Home}/.config/hyprstyle/config.json";

    private static AppSettings? _settings;
    private static List<StyleComponent>? _components;

    public static string StyleDir => Settings.StyleDir is var d && d.StartsWith('~')
        ? Home + d[1..]
        : d;

    public static HashSet<string> HyprConfigComponents => new(Settings.HyprConfigComponents);

    public static List<StyleComponent> Components => _components ??= Settings.Components
        .Select(BuildComponent)
        .ToList();

    private static AppSettings Settings => _settings ??= Load();

    public static string Expand(string? path) =>
        string.IsNullOrEmpty(path) ? "" :
        path.StartsWith('~') ? Home + path[1..] : path;

    private static StyleComponent BuildComponent(ComponentConfig c) => c.Type switch
    {
        "copy" => new CopyComponent(c.Name, Expand(c.Source), c.IsDirectory, c.SaveHook, c.LoadHook),
        "script" => new ScriptComponent(c.Name, c.SaveHook, c.LoadHook),
        _ => throw new Exception($"Component '{c.Name}' has unknown type '{c.Type}' (expected \"copy\" or \"script\")")
    };

    private static AppSettings Load()
    {
        if (!File.Exists(ConfigPath))
        {
            Files.EnsureParent(ConfigPath);
            File.WriteAllText(ConfigPath, DefaultConfig);

            Console.WriteLine($"No config found -- wrote a starter one to:");
            Console.WriteLine($"  {ConfigPath}");
            Console.WriteLine();
            Console.WriteLine("Edit it to add your components, then run this command again.");
            Environment.Exit(0);
        }

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

static class Program
{
    public static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    public static readonly string ConfigPath = $"{Home}/.config/hyprstyle/config.json"; 
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
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            if (!File.Exists(ConfigPath))
            {
                Files.EnsureParent(ConfigPath);
                File.WriteAllText(ConfigPath, DefaultConfig);

                Console.WriteLine($"No config found -- wrote a starter one to:");
                Console.WriteLine($"  {ConfigPath}");
                Console.WriteLine();
                Console.WriteLine("Edit it to add your components, then run this command again.");
                Environment.Exit(0);
            }
            Cli.Usage();
            return;
        }

        Directory.CreateDirectory(Config.StyleDir);

        switch (args[0])
        {
            case "list":
                StyleManager.ListStyles();
                break;

            case "current":
                if (args.Length >= 2 && args[1] == "name")
                    StyleManager.CurrentThemeName();
                else
                    StyleManager.CurrentStyle();
                break;

            case "save" when args.Length == 2:
                StyleManager.SaveStyle(args[1]);
                break;

            case "set" when args.Length == 2:
                StyleManager.SetStyle(args[1]);
                break;

            case "delete" when args.Length == 2:
                StyleManager.DeleteStyle(args[1]);
                break;

            default:
                if (!File.Exists(ConfigPath))
                {
                    Files.EnsureParent(ConfigPath);
                    File.WriteAllText(ConfigPath, DefaultConfig);

                    Console.WriteLine($"No config found -- wrote a starter one to:");
                    Console.WriteLine($"  {ConfigPath}");
                    Console.WriteLine();
                    Console.WriteLine("Edit it to add your components, then run this command again.");
                    Environment.Exit(0);
                }
                Cli.Usage();
                Environment.ExitCode = 1;
                break;
        }
    }
}

static class Processes
{
    public static int Run(string command, string[] args, bool suppressOutput = false, bool check = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            UseShellExecute = false,
            RedirectStandardOutput = suppressOutput,
            RedirectStandardError = suppressOutput
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi);

        if (process == null)
            throw new Exception($"Failed to start {command}");

        process.WaitForExit();

        if (check && process.ExitCode != 0)
            throw new Exception($"{command} exited with code {process.ExitCode}");

        return process.ExitCode;
    }

    public static string RunCapture(string command, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi);

        if (process == null)
            throw new Exception($"Failed to start {command}");

        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception($"{command} exited with code {process.ExitCode}");

        return output;
    }

    public static Process Start(string command, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        return Process.Start(psi) ?? throw new Exception($"Failed to start {command}");
    }

    public static bool IsRunning(string processName) =>
        Process.GetProcessesByName(processName).Length > 0;

    public static void Kill(string processName) =>
        Run("pkill", ["-x", processName], suppressOutput: true);

    public static void KillForce(string processName) =>
        Run("pkill", ["-9", "-x", processName], suppressOutput: true);

    public static void RunHook(string? command, string stylePath, string componentName)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;

        var psi = new ProcessStartInfo
        {
            FileName = "sh",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add(command);
        psi.Environment["STYLE_PATH"] = stylePath;
        psi.Environment["COMPONENT"] = componentName;
        psi.Environment["HOME"] = Config.Home;

        using var process = Process.Start(psi)
            ?? throw new Exception("Failed to start hook shell");

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            string stderr = process.StandardError.ReadToEnd();
            throw new Exception($"hook exited with code {process.ExitCode}{(string.IsNullOrWhiteSpace(stderr) ? "" : $": {stderr.Trim()}")}");
        }
    }
}

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
        if (File.Exists(path) || Directory.Exists(path))
            File.Delete(path);
    }
}

abstract class StyleComponent
{
    public string Name { get; }

    protected StyleComponent(string name)
    {
        Name = name;
    }

    public abstract void Save(string stylePath);
    public abstract void Load(string stylePath);
}

sealed class CopyComponent : StyleComponent
{
    private readonly string Source;
    private readonly bool IsDirectory;
    private readonly string? SaveHook;
    private readonly string? LoadHook;

    public CopyComponent(
        string name,
        string source,
        bool isDirectory,
        string? saveHook,
        string? loadHook)
        : base(name)
    {
        Source = source;
        IsDirectory = isDirectory;
        SaveHook = saveHook;
        LoadHook = loadHook;
    }

    public override void Save(string stylePath)
    {
        if (string.IsNullOrEmpty(Source))
            return;

        if (!File.Exists(Source) && !Directory.Exists(Source))
            return;

        string destination = Path.Join(stylePath, Name);

        if (IsDirectory)
            Files.CopyDirectory(Source, destination);
        else
            Files.CopyFile(Source, destination);

        Processes.RunHook(SaveHook, stylePath, Name);
    }

    public override void Load(string stylePath)
    {
        if (string.IsNullOrEmpty(Source))
            return;

        string saved = Path.Join(stylePath, Name);

        if (!File.Exists(saved) && !Directory.Exists(saved))
            return;

        if (IsDirectory)
            Files.ReplaceDirectory(saved, Source);
        else
            Files.CopyFile(saved, Source);

        Processes.RunHook(LoadHook, stylePath, Name);
    }
}

sealed class ScriptComponent : StyleComponent
{
    private readonly string? SaveHook;
    private readonly string? LoadHook;

    public ScriptComponent(string name, string? saveHook, string? loadHook) : base(name)
    {
        SaveHook = saveHook;
        LoadHook = loadHook;
    }

    public override void Save(string stylePath) => Processes.RunHook(SaveHook, stylePath, Name);
    public override void Load(string stylePath) => Processes.RunHook(LoadHook, stylePath, Name);
}

static class Hyprland
{
    public static JsonArray GetClients()
    {
        string output = Processes.RunCapture("hyprctl", "clients", "-j");

        return JsonNode.Parse(output)?.AsArray()
            ?? throw new Exception("Invalid hyprctl JSON");
    }

    public static List<string> GetWorkspacesForClasses(params string[] classes)
    {
        var result = new List<string>();

        try
        {
            foreach (var node in GetClients())
            {
                if (node is not JsonObject client)
                    continue;

                string? windowClass = client["class"]?.GetValue<string>();

                if (windowClass == null || !classes.Contains(windowClass))
                    continue;

                string? workspace = client["workspace"]?["id"]?.GetValue<int>().ToString();

                if (workspace != null)
                    result.Add(workspace);
            }
        }
        catch
        {
            // hyprctl not answering????
        }

        return result;
    }

    public static List<JsonObject> GetClientsForClasses(params string[] classes)
    {
        var result = new List<JsonObject>();

        try
        {
            foreach (var node in GetClients())
            {
                if (node is JsonObject client &&
                    client["class"]?.GetValue<string>() is string windowClass &&
                    classes.Contains(windowClass))
                {
                    result.Add(client);
                }
            }
        }
        catch
        {
        }

        return result;
    }

    public static void MoveWindow(string address, string workspace)
    {
        string rule = $"hl.dsp.window.move({{ workspace = \"{workspace}\", follow = false, window = \"address:{address}\" }})";

        Processes.Run("hyprctl", ["dispatch", rule], suppressOutput: true);
    }

    public static string? WaitForNewWindow(string[] classes, HashSet<string> alreadySeen, int timeoutMs, int pollMs = 60)
    {
        int waited = 0;

        while (waited < timeoutMs)
        {
            foreach (var client in GetClientsForClasses(classes))
            {
                string? address = client["address"]?.GetValue<string>();

                if (!string.IsNullOrEmpty(address) && alreadySeen.Add(address))
                    return address;
            }

            Thread.Sleep(pollMs);
            waited += pollMs;
        }

        return null;
    }

    public static void WaitUntilClosed(string[] classes, int timeoutMs, int pollMs = 60)
    {
        int waited = 0;

        while (waited < timeoutMs && GetClientsForClasses(classes).Count > 0)
        {
            Thread.Sleep(pollMs);
            waited += pollMs;
        }
    }

    public static void Reload() =>
        Processes.Run("hyprctl", ["reload"], suppressOutput: true);
}

static class StyleManager
{
    private const string Marker = ".style";
    private static readonly string CurrentLink = Path.Join(Config.StyleDir, "current");

    public static List<string> FindStyles()
    {
        if (!Directory.Exists(Config.StyleDir))
            return [];

        string current = Path.GetFullPath(CurrentLink);
        string separator = Path.DirectorySeparatorChar.ToString();

        return Directory
            .EnumerateFiles(Config.StyleDir, Marker, SearchOption.AllDirectories)
            .Select(Path.GetDirectoryName)
            .Where(x => x != null)
            .Cast<string>()
            .Where(x =>
            {
                string full = Path.GetFullPath(x);
                return full != current && !full.StartsWith(current + separator);
            })
            .OrderBy(x => x)
            .ToList();
    }

    public static void ListStyles()
    {
        var styles = FindStyles();

        if (styles.Count == 0)
        {
            Console.WriteLine("No styles found.");
            return;
        }

        string? current = ResolveCurrent();

        Console.WriteLine();
        Console.WriteLine("╭─ Available styles");

        var grouped = new Dictionary<string, List<string>>();

        foreach (var style in styles)
        {
            string relative = Path.GetRelativePath(Config.StyleDir, style);
            string[] parts = relative.Split(Path.DirectorySeparatorChar);
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
                bool last = i == entries.Count - 1;
                string branch = last ? "└─" : "├─";
                string marker = current != null && Path.GetFullPath(entries[i]) == current ? "★" : " ";
                string name = Path.GetFileName(entries[i]);

                Console.WriteLine($"│  {branch} {marker} {name}");
            }
        }

        Console.WriteLine("│");

        if (grouped.TryGetValue("", out var rootStyles))
        {
            foreach (var style in rootStyles)
            {
                string marker = current != null && Path.GetFullPath(style) == current ? "★" : " ";
                Console.WriteLine($"│ {marker} {Path.GetFileName(style)}");
            }
        }

        Console.WriteLine("│");
        Console.WriteLine("╰────────────────");
    }

    public static void SaveStyle(string name)
    {
        string path = Path.Join(Config.StyleDir, name);

        if (File.Exists(path) || Directory.Exists(path))
        {
            if (File.Exists(path))
                File.Delete(path);
            else
                Directory.Delete(path, true);
        }

        Directory.CreateDirectory(path);
        File.Create(Path.Join(path, Marker)).Dispose();

        int total = Config.Components.Count;
        int current = 0;

        foreach (var component in Config.Components)
        {
            try
            {
                Cli.Progress("Saving", component.Name, ++current, total);
                component.Save(path);
            }
            catch (Exception e)
            {
                Cli.Error($"Failed {component.Name}: {e.Message}");
            }
        }

        SetCurrentLink(path);
        Cli.Finish($"Saved style: {name}");
    }

    private static void LoadComponent(StyleComponent component, string path, int current, int total)
    {
        try
        {
            Cli.Progress("Applying", component.Name, current, total);
            component.Load(path);
        }
        catch (Exception e)
        {
            Cli.Error($"Failed {component.Name}: {e.Message}");
        }
    }

    public static void SetStyle(string name)
    {
        string path = Path.Join(Config.StyleDir, name);

        if (!Directory.Exists(path))
        {
            Console.WriteLine($"Style '{name}' does not exist.");
            return;
        }

        SetCurrentLink(path);

        var hyprConfigComponents = Config.HyprConfigComponents;
        var hyprFirst = Config.Components.Where(c => hyprConfigComponents.Contains(c.Name)).ToList();
        var hyprRest = Config.Components.Where(c => !hyprConfigComponents.Contains(c.Name)).ToList();

        int total = Config.Components.Count;
        int current = 0;

        foreach (var component in hyprFirst)
            LoadComponent(component, path, ++current, total);

        Hyprland.Reload();

        Parallel.ForEach(
            hyprRest,
            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(hyprRest.Count, 1) },
            component =>
            {
                int componentNumber = Interlocked.Increment(ref current);
                LoadComponent(component, path, componentNumber, total);
            }
        );

        Hyprland.Reload();

        Cli.Finish($"Applied style: {name}");
    }

    public static void DeleteStyle(string name)
    {
        string path = Path.Join(Config.StyleDir, name);

        if (!Directory.Exists(path))
        {
            Console.WriteLine($"Style '{name}' does not exist.");
            return;
        }

        if (IsCurrent(path))
            DeleteCurrentLink();

        bool isStyle = File.Exists(Path.Join(path, Marker));

        Directory.Delete(path, true);
        Console.WriteLine($"Deleted {(isStyle ? "style" : "folder")}: {name}");
    }

    public static void CurrentStyle()
    {
        string? path = ResolveCurrent();

        Console.WriteLine(path == null ? "No style selected." : Path.GetFileName(path));
    }

    public static void CurrentThemeName()
    {
        string? path = ResolveCurrent();

        Console.WriteLine(path == null ? "" : Path.GetFileName(path));
    }

    private static void SetCurrentLink(string target)
    {
        DeleteCurrentLink();
        Directory.CreateSymbolicLink(CurrentLink, target);
    }

    private static void DeleteCurrentLink()
    {
        try
        {
            if (File.Exists(CurrentLink))
                File.Delete(CurrentLink);

            if (Directory.Exists(CurrentLink))
                Directory.Delete(CurrentLink, recursive: false);
        }
        catch
        {
        }
    }

    private static string? ResolveCurrent()
    {
        try
        {
            string? target = new DirectoryInfo(CurrentLink).LinkTarget;

            if (target == null)
                return null;

            return Path.GetFullPath(
                Path.IsPathRooted(target)
                    ? target
                    : Path.Join(Path.GetDirectoryName(CurrentLink)!, target)
            );
        }
        catch
        {
            return null;
        }
    }

    private static bool IsCurrent(string path)
    {
        string? current = ResolveCurrent();

        return current != null && Path.GetFullPath(current) == Path.GetFullPath(path);
    }
}

static class Cli
{
    private static readonly object OutputLock = new();

    public static void Progress(string action, string component, int current, int total)
    {
        lock (OutputLock)
        {
            Console.Write($"\r  {action,-8} {component,-28} [{current,2}/{total,2}]");
        }
    }

    public static void Finish(string message)
    {
        lock (OutputLock)
        {
            Console.WriteLine($"\r  ✓ {message,-48}");
        }
    }

    public static void Error(string message)
    {
        lock (OutputLock)
        {
            Console.WriteLine();
            Console.WriteLine($"  ✗ {message}");
        }
    }

    public static void Usage()
    {
        Console.WriteLine(
            """
            Usage:
              hyprstyle list -- List all saved styles.
              hyprstyle current -- Get the current style name, or "No style selected." if there's none.
              hyprstyle current name -- Get the current style name, or "" if there's none. Most useful for other programs to read the current style name.
              hyprstyle save <name> -- Save a new style or replace an existing one.
              hyprstyle set <name> -- Load a saved style.
              hyprstyle delete <name> -- Delete a saved style.

            Config: ~/.config/hyprstyle/config.json
            """
        );
    }
}