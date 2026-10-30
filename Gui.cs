using System.ComponentModel;
using System.Reflection;
using System.Security.Cryptography;

static class Gui
{
    private const string ResourcePrefix = "gui/";
    private const string StampName = ".installed";

    public static void Open()
    {
        string directory = Path.Join(Config.ConfigDir, "gui");
        string file = Path.Join(directory, "shell.qml");

        Install(directory);

        Environment.SetEnvironmentVariable("HYPRSTYLE_BIN", Environment.ProcessPath ?? "hyprstyle");

        Updater.CheckInBackground();

        try
        {
            // this prevents the GUI from being killed by `pkill quickshell` in users' load hooks
            // as i also said in the transition code, i really do hope there's no edge case where this breaks
            string? shell = (Environment.GetEnvironmentVariable("PATH") ?? "")
                .Split(':', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => Path.Join(d, "quickshell"))
                .FirstOrDefault(File.Exists);

            if (shell == null)
                throw new Win32Exception();

            string link = Path.Join(directory, "hyprstyle-gui");
            File.Delete(link);
            File.CreateSymbolicLink(link, shell);

            Processes.Run(link, ["-p", file]);
        }
        catch (Win32Exception)
        {
            Console.WriteLine("quickshell isn't installed or isn't on your PATH.");
            Cli.Notify("quickshell isn't installed or isn't on your PATH.");
            Environment.ExitCode = 1;
        }
    }

    private static void Install(string directory)
    {
        Directory.CreateDirectory(directory);

        string stampPath = Path.Join(directory, StampName);
        var installed = ReadStamp(stampPath);
        var assembly = Assembly.GetExecutingAssembly();

        foreach (var resource in assembly.GetManifestResourceNames().Where(n => n.StartsWith(ResourcePrefix)))
        {
            string name = resource[ResourcePrefix.Length..];
            string target = Path.Join(directory, name);
            byte[] bundled = ReadResource(assembly, resource);
            string bundledHash = Convert.ToHexString(SHA256.HashData(bundled));

            installed.TryGetValue(name, out string? recordedHash);

            if (File.Exists(target) && recordedHash == bundledHash)
                continue;

            if (File.Exists(target) && recordedHash != null && HashOf(target) != recordedHash)
                File.Move(target, target + ".bak", overwrite: true);

            File.WriteAllBytes(target, bundled);
            installed[name] = bundledHash;
        }

        File.WriteAllLines(stampPath, installed.Select(pair => $"{pair.Key}\t{pair.Value}"));
    }

    public static byte[] ReadResource(Assembly assembly, string resource)
    {
        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new Exception($"Missing bundled file {resource}");

        using var memory = new MemoryStream();
        stream.CopyTo(memory);

        return memory.ToArray();
    }

    private static string HashOf(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static Dictionary<string, string> ReadStamp(string path)
    {
        var installed = new Dictionary<string, string>();

        if (!File.Exists(path))
            return installed;

        foreach (var line in File.ReadAllLines(path))
        {
            string[] parts = line.Split('\t');

            if (parts.Length == 2)
                installed[parts[0]] = parts[1];
        }

        return installed;
    }
}