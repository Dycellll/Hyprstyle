using System.Diagnostics;

static class Processes
{
    private static ProcessStartInfo Prepare(string command, IEnumerable<string> args, bool redirect)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            UseShellExecute = false,
            RedirectStandardOutput = redirect,
            RedirectStandardError = redirect
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        return psi;
    }

    public static int Run(string command, string[] args, bool suppressOutput = false, bool check = false)
    {
        using var process = Process.Start(Prepare(command, args, suppressOutput))
            ?? throw new Exception($"Failed to start {command}");

        process.WaitForExit();

        if (check && process.ExitCode != 0)
            throw new Exception($"{command} exited with code {process.ExitCode}");

        return process.ExitCode;
    }

    public static string RunCapture(string command, params string[] args)
    {
        using var process = Process.Start(Prepare(command, args, true))
            ?? throw new Exception($"Failed to start {command}");

        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception($"{command} exited with code {process.ExitCode}");

        return output;
    }

    public static Process Start(string command, params string[] args) =>
        Process.Start(Prepare(command, args, true)) ?? throw new Exception($"Failed to start {command}");

    public static void StartDetached(string command, string[] args, string envKey, string envValue)
    {
        var psi = Prepare(command, args, false);
        psi.Environment[envKey] = envValue;

        Process.Start(psi)?.Dispose();
    }

    public static bool OnPath(string command) =>
        (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(':', StringSplitOptions.RemoveEmptyEntries)
            .Any(dir => File.Exists(Path.Join(dir, command)));

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

        var psi = Prepare("sh", ["-c", command], true);
        psi.Environment["STYLE_PATH"] = stylePath;
        psi.Environment["COMPONENT"] = componentName;
        psi.Environment["HOME"] = Config.Home;

        using var process = Process.Start(psi)
            ?? throw new Exception("Failed to start hook shell");

        // Drain both pipes concurrently so chatty hooks can't deadlock on a full pipe buffer.
        // We never wait on the pipes after the shell exits: a hook that backgrounds a long-lived
        // program (e.g. "waybar &") leaves it holding them open.
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            stderr.Wait(100);
            string text = stderr.IsCompletedSuccessfully ? stderr.Result.Trim() : "";
            throw new Exception($"hook exited with code {process.ExitCode}{(text == "" ? "" : $": {text}")}");
        }
    }
}
