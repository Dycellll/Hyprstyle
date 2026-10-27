using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
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
    public static readonly string ConfigDir = $"{Home}/.config/hyprstyle";
    public static readonly string ConfigPath = $"{ConfigDir}/config.json";

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

static class Program
{
    static void Main(string[] args)
    {
        if (args.Length >= 1)
        {
            switch (args[0])
            {
                case "current":
                    StyleManager.CurrentStyle();
                    return;

                case "version" or "--version":
                    Console.WriteLine(Updater.CurrentVersion);
                    return;

                case "--refresh-update-cache":
                    Updater.RefreshCache();
                    return;
            }
        }

        if (args.Length < 1)
        {
            Config.EnsureExists();
            Cli.Usage();
            return;
        }

        Directory.CreateDirectory(Config.StyleDir);

        bool notify = true;

        switch (args[0])
        {
            case "list":
                StyleManager.ListStyles();
                break;

            case "save" when args.Length == 2:
                StyleManager.SaveStyle(args[1]);
                break;

            case "set" when args.Length == 2:
                StyleManager.SetStyle(args[1]);
                break;

            case "reload" when args.Length == 1:
                StyleManager.ReloadStyle();
                break;

            case "rename" when args.Length == 3:
                StyleManager.RenameStyle(args[1], args[2]);
                break;

            case "alias" when args.Length == 3:
                StyleManager.AddAlias(args[1], args[2]);
                break;

            case "deletealias" when args.Length == 3:
                StyleManager.RemoveAlias(args[1], args[2]);
                break;

            case "delete" when args.Length == 2:
                StyleManager.DeleteStyle(args[1]);
                break;

            case "json" when args.Length == 1:
                notify = false;
                StyleManager.PrintJson();
                break;

            case "gui" when args.Length == 1:
                notify = false;
                Gui.Open();
                break;

            case "update" when args.Length == 1:
                notify = false;
                Updater.Update();
                break;

            default:
                Cli.Usage();
                Environment.ExitCode = 1;
                break;
        }

        if (notify)
            Updater.Notify();
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
    private const string AliasesFile = ".aliases";
    private const string PreviewFile = ".preview.png";
    private static readonly string StateFile = $"{Config.ConfigDir}/current_style";

    private static string CurrentLink => Path.Join(Config.StyleDir, "current");

    private static string PathOf(string name) => Path.Join(Config.StyleDir, name);

    private static string NameOf(string path) => Path.GetRelativePath(Config.StyleDir, path);

    private static void Fail(string message)
    {
        Console.WriteLine(message);
        Environment.ExitCode = 1;
    }

    private static bool IsValidName(string name) =>
        !string.IsNullOrWhiteSpace(name) &&
        !Path.IsPathRooted(name) &&
        name != "current" &&
        !name.Split('/').Any(part => part == "" || part.StartsWith('.'));

    private static bool IsValidAlias(string alias) =>
        !string.IsNullOrWhiteSpace(alias) &&
        !alias.StartsWith('.') &&
        !alias.Any(c => char.IsWhiteSpace(c) || c == '/');

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

            if (File.Exists(Path.Join(sub, Marker)))
                found.Add(sub);
            else
                Walk(sub, skip, found);
        }
    }

    private static string? FindStyle(string nameOrAlias)
    {
        string direct = PathOf(nameOrAlias);

        if (File.Exists(Path.Join(direct, Marker)))
            return direct;

        return AliasOwner(nameOrAlias);
    }

    private static string? AliasOwner(string alias) =>
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

    private static void WriteAliases(string stylePath, List<string> aliases)
    {
        string file = Path.Join(stylePath, AliasesFile);

        if (aliases.Count == 0)
            File.Delete(file);
        else
            File.WriteAllLines(file, aliases);
    }

    private static string DisplayName(string style)
    {
        var aliases = ReadAliases(style);
        string name = Path.GetFileName(style);

        return aliases.Count == 0 ? name : $"{name} ({string.Join(", ", aliases)})";
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

                Console.WriteLine($"│  {branch} {marker} {DisplayName(entries[i])}");
            }
        }

        Console.WriteLine("│");

        if (grouped.TryGetValue("", out var rootStyles))
        {
            foreach (var style in rootStyles)
            {
                string marker = current != null && Path.GetFullPath(style) == current ? "★" : " ";
                Console.WriteLine($"│ {marker} {DisplayName(style)}");
            }
        }

        Console.WriteLine("│");
        Console.WriteLine("╰────────────────");
    }

    public static void SaveStyle(string name)
    {
        if (!IsValidName(name))
        {
            Fail($"'{name}' isn't a valid style name.");
            return;
        }

        if (AliasOwner(name) is string owner)
        {
            Fail($"'{name}' is already an alias of '{NameOf(owner)}'.");
            return;
        }

        string path = PathOf(name);
        var aliases = ReadAliases(path);

        if (File.Exists(path) || Directory.Exists(path))
        {
            if (File.Exists(path))
                File.Delete(path);
            else
                Directory.Delete(path, true);
        }

        Directory.CreateDirectory(path);
        File.Create(Path.Join(path, Marker)).Dispose();
        WriteAliases(path, aliases);

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

        CapturePreview(path);
        SetCurrentLink(path);
        Cli.Finish($"Saved style: {name}");
    }

    private static void CapturePreview(string path)
    {
        try
        {
            Processes.Run("grim", ["-s", "0.4", Path.Join(path, PreviewFile)], suppressOutput: true);
        }
        catch
        {
        }
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
        string? path = FindStyle(name);

        if (path == null)
        {
            Fail($"Style '{name}' does not exist.");
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

        Cli.Finish($"Applied style: {NameOf(path)}");
    }

    public static void ReloadStyle()
    {
        string? name = CurrentRelative();

        if (name == null)
        {
            Fail("No style selected.");
            return;
        }

        SetStyle(name);
    }

    public static void RenameStyle(string name, string newName)
    {
        string? path = FindStyle(name);

        if (path == null)
        {
            Fail($"Style '{name}' does not exist.");
            return;
        }

        if (!IsValidName(newName))
        {
            Fail($"'{newName}' isn't a valid style name.");
            return;
        }

        string target = PathOf(newName);

        if (File.Exists(target) || Directory.Exists(target))
        {
            Fail($"'{newName}' already exists.");
            return;
        }

        if (AliasOwner(newName) is string owner)
        {
            Fail($"'{newName}' is already an alias of '{NameOf(owner)}'.");
            return;
        }

        if (target.StartsWith(path + Path.DirectorySeparatorChar))
        {
            Fail($"Can't move '{NameOf(path)}' inside itself.");
            return;
        }

        bool wasCurrent = IsCurrent(path);

        Files.EnsureParent(target);
        Directory.Move(path, target);

        if (wasCurrent)
            SetCurrentLink(target);

        Console.WriteLine($"Renamed {NameOf(path)} to {newName}");
    }

    public static void AddAlias(string name, string alias)
    {
        string? path = FindStyle(name);

        if (path == null)
        {
            Fail($"Style '{name}' does not exist.");
            return;
        }

        if (!IsValidAlias(alias))
        {
            Fail("Aliases can't be empty, start with a dot, or contain spaces or slashes.");
            return;
        }

        if (AliasOwner(alias) is string owner)
        {
            Fail(Path.GetFullPath(owner) == Path.GetFullPath(path)
                ? $"'{NameOf(path)}' already has the alias '{alias}'."
                : $"'{alias}' is already an alias of '{NameOf(owner)}'.");
            return;
        }

        if (File.Exists(Path.Join(PathOf(alias), Marker)))
        {
            Fail($"'{alias}' is already the name of a style.");
            return;
        }

        var aliases = ReadAliases(path);
        aliases.Add(alias);
        WriteAliases(path, aliases);

        Console.WriteLine($"Added alias '{alias}' to {NameOf(path)}");
    }

    public static void RemoveAlias(string name, string alias)
    {
        string? path = FindStyle(name);

        if (path == null)
        {
            Fail($"Style '{name}' does not exist.");
            return;
        }

        var aliases = ReadAliases(path);

        if (!aliases.Remove(alias))
        {
            Fail($"'{NameOf(path)}' has no alias '{alias}'.");
            return;
        }

        WriteAliases(path, aliases);
        Console.WriteLine($"Removed alias '{alias}' from {NameOf(path)}");
    }

    public static void DeleteStyle(string name)
    {
        string path = PathOf(name);

        if (!Directory.Exists(path))
        {
            Fail($"Style '{name}' does not exist.");
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
        string? name = CurrentRelative();

        Console.WriteLine(name == null ? "" : Path.GetFileName(name));
    }

    public static string? CurrentRelative()
    {
        try
        {
            if (File.Exists(StateFile))
            {
                string saved = File.ReadAllText(StateFile).Trim();
                return saved == "" ? null : saved;
            }
        }
        catch
        {
        }

        string? path = ResolveCurrent();
        string? name = path == null ? null : NameOf(path);

        WriteState(name ?? "");
        return name;
    }

    public static void PrintJson()
    {
        string? current = CurrentRelative();
        var styles = FindStyles();
        string? latest = Updater.CachedLatest(out _);
        bool updateAvailable = latest != null && Updater.IsNewer(latest, Updater.CurrentVersion);

        using var stream = new MemoryStream();

        using (var json = new Utf8JsonWriter(stream))
        {
            json.WriteStartObject();
            json.WriteString("version", Updater.CurrentVersion);

            if (latest != null)
                json.WriteString("latestVersion", Updater.Clean(latest));
            else
                json.WriteNull("latestVersion");

            json.WriteBoolean("updateAvailable", updateAvailable);
            json.WriteString("styleDir", Config.StyleDir);
            json.WriteString("configPath", Config.ConfigPath);

            if (current != null)
                json.WriteString("current", current);
            else
                json.WriteNull("current");

            json.WriteStartArray("folders");

            foreach (var folder in styles
                .Select(s => Path.GetDirectoryName(NameOf(s)) ?? "")
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
                string name = NameOf(style);
                string previewPath = Path.Join(style, PreviewFile);

                json.WriteStartObject();
                json.WriteString("name", name);
                json.WriteString("displayName", Path.GetFileName(style));
                json.WriteString("folder", Path.GetDirectoryName(name) ?? "");
                json.WriteString("path", style);
                json.WriteBoolean("current", name == current);

                json.WriteStartArray("aliases");

                foreach (var alias in ReadAliases(style))
                    json.WriteStringValue(alias);

                json.WriteEndArray();

                if (File.Exists(previewPath))
                    json.WriteString("preview", previewPath);
                else
                    json.WriteNull("preview");

                json.WriteString("savedAt", File.GetLastWriteTimeUtc(Path.Join(style, Marker)));
                json.WriteEndObject();
            }

            json.WriteEndArray();

            json.WriteStartArray("components");

            foreach (var component in Config.Components)
                json.WriteStringValue(component.Name);

            json.WriteEndArray();
            json.WriteEndObject();
        }

        Console.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));
    }

    private static void SetCurrentLink(string target)
    {
        DeleteCurrentLink();
        Directory.CreateSymbolicLink(CurrentLink, target);
        WriteState(NameOf(target));
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

        WriteState("");
    }

    private static void WriteState(string name)
    {
        try
        {
            Files.EnsureParent(StateFile);
            File.WriteAllText(StateFile, name);
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

static class Updater
{
    private const string Repo = "Dycellll/hyprstyle";
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(12);
    private static readonly string CachePath = $"{Config.ConfigDir}/update_cache";

    public static string CurrentVersion { get; } = ReadVersion();

    private static string ReadVersion()
    {
        string raw = typeof(Updater).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "0.0.0";

        int plus = raw.IndexOf('+');

        return plus >= 0 ? raw[..plus] : raw;
    }

    public static string Clean(string version) => version.Trim().TrimStart('v', 'V');

    private static Version? Parse(string text)
    {
        text = Clean(text);

        int cut = text.IndexOfAny(['-', '+']);

        if (cut >= 0)
            text = text[..cut];

        if (!text.Contains('.'))
            text += ".0";

        return Version.TryParse(text, out var version) ? version : null;
    }

    public static bool IsNewer(string latest, string current)
    {
        var a = Parse(latest);
        var b = Parse(current);

        return a != null && b != null && a > b;
    }

    public static string? CachedLatest(out bool stale)
    {
        stale = true;

        try
        {
            string[] lines = File.ReadAllLines(CachePath);

            if (lines.Length >= 1 && long.TryParse(lines[0], out long stamp))
            {
                stale = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(stamp) > CacheLifetime;

                if (lines.Length >= 2 && lines[1].Trim() != "")
                    return lines[1].Trim();
            }
        }
        catch
        {
        }

        return null;
    }

    private static void WriteCache(string latest)
    {
        try
        {
            Files.EnsureParent(CachePath);
            File.WriteAllLines(CachePath, [DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), latest]);
        }
        catch
        {
        }
    }

    private static string? FetchLatest()
    {
        try
        {
            using var handler = new HttpClientHandler { AllowAutoRedirect = false };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("hyprstyle");

            using var request = new HttpRequestMessage(HttpMethod.Head, $"https://github.com/{Repo}/releases/latest");
            using var response = client.Send(request);

            string? location = response.Headers.Location?.ToString();

            if (location == null || !location.Contains("/tag/"))
                return null;

            return location[(location.LastIndexOf('/') + 1)..];
        }
        catch
        {
            return null;
        }
    }

    public static void RefreshCache()
    {
        string? previous = CachedLatest(out _);

        WriteCache(FetchLatest() ?? previous ?? "");
    }

    public static void Notify()
    {
        string? latest = CachedLatest(out bool stale);

        if (latest != null && IsNewer(latest, CurrentVersion))
            Console.Error.WriteLine($"Update available: {CurrentVersion} -> {Clean(latest)}. Run 'hyprstyle update' to install it.");

        if (!stale)
            return;

        try
        {
            Processes.Start(Environment.ProcessPath ?? "hyprstyle", "--refresh-update-cache");
        }
        catch
        {
        }
    }

    public static void Update()
    {
        Console.WriteLine("Checking for updates...");

        string? latest = FetchLatest();

        if (latest == null)
        {
            Console.WriteLine("Couldn't reach GitHub to check for updates.");
            Environment.ExitCode = 1;
            return;
        }

        WriteCache(latest);

        if (!IsNewer(latest, CurrentVersion))
        {
            Console.WriteLine($"Already up to date ({CurrentVersion}).");
            return;
        }

        Console.WriteLine($"Updating {CurrentVersion} -> {Clean(latest)}...");

        string script = $"f=$(mktemp) && curl -fsSL https://raw.githubusercontent.com/{Repo}/HEAD/install.sh -o \"$f\" && sh \"$f\" --update; code=$?; rm -f \"$f\"; exit $code";

        int exitCode = Processes.Run("sh", ["-c", script]);

        if (exitCode != 0)
            Environment.ExitCode = exitCode;
    }
}

static class Gui
{
    public static void Open()
    {
        string directory = Path.Join(Config.ConfigDir, "gui");
        string file = Path.Join(directory, "shell.qml");

        Directory.CreateDirectory(directory);
        File.WriteAllText(file, Source.Replace("@BIN@", Environment.ProcessPath ?? "hyprstyle"));

        try
        {
            Processes.Run("quickshell", ["-p", file]);
        }
        catch (Win32Exception)
        {
            Console.WriteLine("quickshell isn't installed or isn't on your PATH.");
            Environment.ExitCode = 1;
        }
    }

    private const string Source = """"
        import QtQuick
        import QtQuick.Controls
        import QtQuick.Layouts
        import Quickshell
        import Quickshell.Io

        FloatingWindow {
            id: win
            title: "hyprstyle"
            color: "#15151b"
            implicitWidth: 960
            implicitHeight: 640

            property var info: ({ styles: [], current: null })
            property string selected: ""
            property string status: ""
            property bool saving: false

            readonly property var selectedStyle: {
                const list = info.styles || []
                for (let i = 0; i < list.length; i++) {
                    if (list[i].name === selected)
                        return list[i]
                }
                return null
            }

            Process {
                id: loader
                command: ["@BIN@", "json"]
                stdout: StdioCollector {
                    onStreamFinished: {
                        try {
                            win.info = JSON.parse(text)
                        } catch (e) {
                            win.status = "Couldn't read hyprstyle json"
                        }
                    }
                }
            }

            Process {
                id: runner
                stdout: StdioCollector {
                    onStreamFinished: {
                        const parts = text.split(/[\r\n]+/).filter(s => s.trim() !== "")
                        win.status = parts.length ? parts[parts.length - 1].trim() : ""
                    }
                }
                onExited: {
                    win.saving = false
                    win.visible = true
                    loader.running = true
                }
            }

            Timer {
                id: saveTimer
                interval: 450
                property var args: []
                onTriggered: win.run(args)
            }

            Connections {
                target: Quickshell
                function onLastWindowClosed() {
                    if (!win.saving)
                        Qt.quit()
                }
            }

            Component.onCompleted: loader.running = true

            function run(args) {
                runner.command = ["@BIN@"].concat(args)
                runner.running = true
            }

            function saveStyle(name) {
                saving = true
                visible = false
                saveTimer.args = ["save", name]
                saveTimer.start()
            }

            ColumnLayout {
                anchors.fill: parent
                anchors.margins: 16
                spacing: 12

                RowLayout {
                    Layout.fillWidth: true
                    spacing: 8

                    TextField {
                        id: saveName
                        Layout.fillWidth: true
                        placeholderText: "Save the current setup as..."
                        onAccepted: saveButton.clicked()
                    }

                    Button {
                        id: saveButton
                        text: "Save"
                        enabled: saveName.text.trim() !== ""
                        onClicked: {
                            win.saveStyle(saveName.text.trim())
                            saveName.text = ""
                        }
                    }

                    Button {
                        text: "Reload"
                        onClicked: win.run(["reload"])
                    }
                }

                Rectangle {
                    visible: win.info.updateAvailable === true
                    Layout.fillWidth: true
                    implicitHeight: 40
                    radius: 6
                    color: "#2b2540"

                    RowLayout {
                        anchors.fill: parent
                        anchors.margins: 8

                        Text {
                            Layout.fillWidth: true
                            color: "#d8d0f0"
                            text: "Update available: " + win.info.latestVersion
                        }

                        Button {
                            text: "Update"
                            onClicked: win.run(["update"])
                        }
                    }
                }

                RowLayout {
                    Layout.fillWidth: true
                    Layout.fillHeight: true
                    spacing: 16

                    GridView {
                        id: grid
                        Layout.fillWidth: true
                        Layout.fillHeight: true
                        clip: true
                        cellWidth: 220
                        cellHeight: 190
                        model: win.info.styles

                        delegate: Item {
                            id: card
                            required property var modelData
                            width: grid.cellWidth
                            height: grid.cellHeight

                            Rectangle {
                                anchors.fill: parent
                                anchors.margins: 6
                                radius: 8
                                color: card.modelData.name === win.selected ? "#2a2540" : "#1e1e26"
                                border.width: card.modelData.current ? 2 : 0
                                border.color: "#8b7cf6"

                                Rectangle {
                                    id: frame
                                    anchors.left: parent.left
                                    anchors.right: parent.right
                                    anchors.top: parent.top
                                    anchors.margins: 8
                                    height: 116
                                    radius: 4
                                    color: "#101015"
                                    clip: true

                                    Text {
                                        anchors.centerIn: parent
                                        text: "No preview"
                                        color: "#5d5b6b"
                                        visible: shot.status !== Image.Ready
                                    }

                                    Image {
                                        id: shot
                                        anchors.fill: parent
                                        fillMode: Image.PreserveAspectCrop
                                        asynchronous: true
                                        cache: false
                                        source: card.modelData.preview ? "file://" + card.modelData.preview : ""
                                    }
                                }

                                Text {
                                    id: nameText
                                    anchors.left: parent.left
                                    anchors.right: parent.right
                                    anchors.top: frame.bottom
                                    anchors.margins: 8
                                    text: card.modelData.name
                                    color: "white"
                                    elide: Text.ElideRight
                                }

                                Text {
                                    anchors.left: parent.left
                                    anchors.right: parent.right
                                    anchors.top: nameText.bottom
                                    anchors.leftMargin: 8
                                    anchors.rightMargin: 8
                                    text: card.modelData.aliases.join(", ")
                                    color: "#8a879a"
                                    font.pixelSize: 11
                                    elide: Text.ElideRight
                                }

                                MouseArea {
                                    anchors.fill: parent
                                    onClicked: {
                                        win.selected = card.modelData.name
                                        win.run(["set", card.modelData.name])
                                    }
                                }
                            }
                        }
                    }

                    ColumnLayout {
                        Layout.preferredWidth: 260
                        Layout.fillHeight: true
                        visible: win.selectedStyle !== null
                        spacing: 10

                        Text {
                            Layout.fillWidth: true
                            text: win.selectedStyle ? win.selectedStyle.name : ""
                            color: "white"
                            font.pixelSize: 16
                            elide: Text.ElideRight
                        }

                        RowLayout {
                            Layout.fillWidth: true

                            TextField {
                                id: renameField
                                Layout.fillWidth: true
                                placeholderText: "New name"
                            }

                            Button {
                                text: "Rename"
                                enabled: renameField.text.trim() !== ""
                                onClicked: {
                                    win.run(["rename", win.selected, renameField.text.trim()])
                                    renameField.text = ""
                                    win.selected = ""
                                }
                            }
                        }

                        Text {
                            text: "Aliases"
                            color: "#8a879a"
                        }

                        Flow {
                            Layout.fillWidth: true
                            spacing: 6

                            Repeater {
                                model: win.selectedStyle ? win.selectedStyle.aliases : []

                                delegate: Button {
                                    required property string modelData
                                    text: modelData + "  ×"
                                    onClicked: win.run(["deletealias", win.selected, modelData])
                                }
                            }
                        }

                        RowLayout {
                            Layout.fillWidth: true

                            TextField {
                                id: aliasField
                                Layout.fillWidth: true
                                placeholderText: "New alias"
                            }

                            Button {
                                text: "Add"
                                enabled: aliasField.text.trim() !== ""
                                onClicked: {
                                    win.run(["alias", win.selected, aliasField.text.trim()])
                                    aliasField.text = ""
                                }
                            }
                        }

                        Item { Layout.fillHeight: true }

                        Button {
                            Layout.fillWidth: true
                            text: "Delete"
                            onClicked: {
                                win.run(["delete", win.selected])
                                win.selected = ""
                            }
                        }
                    }
                }

                Text {
                    Layout.fillWidth: true
                    text: win.status
                    color: "#8a879a"
                    elide: Text.ElideRight
                }
            }
        }
        """";
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
              hyprstyle current -- Get the current style name, or "" if there's none. Most useful for other programs to read the current style name.
              hyprstyle save <name> -- Save a new style or replace an existing one.
              hyprstyle set <name|alias> -- Load a saved style.
              hyprstyle reload -- Load the current style again.
              hyprstyle rename <name> <new_name> -- Rename a saved style.
              hyprstyle alias <name> <alias> -- Give a style an alias.
              hyprstyle deletealias <name> <alias> -- Remove an alias from a style.
              hyprstyle delete <name> -- Delete a saved style.
              hyprstyle gui -- Open the quickshell GUI.
              hyprstyle json -- Print all style data as JSON.
              hyprstyle update -- Update hyprstyle to the latest release.
              hyprstyle version -- Print the installed version.

            Config: ~/.config/hyprstyle/config.json
            """
        );
    }
}
