using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

sealed class Transition : IDisposable
{
    public static bool Disabled;

    private readonly Socket socket;

    private Transition(Socket socket)
    {
        this.socket = socket;
    }

    public static Transition? Begin()
    {
        if (Disabled || Config.Transition == "none")
            return null;

        string? runtime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");

        if (string.IsNullOrEmpty(runtime) || !Processes.OnPath("grim") || !Processes.OnPath("quickshell"))
            return null;

        string dir = $"{runtime}/hyprstyle-{Environment.ProcessId}";

        try
        {
            var transition = Open(dir);

            if (transition == null)
                Directory.Delete(dir, true);

            return transition;
        }
        catch
        {
            try { Directory.Delete(dir, true); } catch { }
            return null;
        }
    }

    public void Progress(int done, int total) =>
        Send($"progress {(double)done / total:0.###}\n");

    public void Reveal() =>
        Send("go\n");

    public void Dispose() =>
        socket.Dispose();

    private void Send(string message)
    {
        try
        {
            lock (socket)
                socket.Send(Encoding.ASCII.GetBytes(message));
        }
        catch
        {
        }
    }

    private static Transition? Open(string dir)
    {
        Directory.CreateDirectory(dir);

        File.WriteAllBytes($"{dir}/shell.qml", Gui.ReadResource(Assembly.GetExecutingAssembly(), "transition.qml"));

        Timing.Measure("  overlay spawn", () => Processes.StartDetached(
            "sh",
            ["-c", "exec >/dev/null 2>&1 </dev/null; quickshell -p \"$0/shell.qml\"; rm -rf \"$0\"", dir],
            "HYPRSTYLE_TRANSITION_DIR",
            dir));

        Timing.Measure("  screenshots", () => Concurrent.ForEach(Hyprland.GetMonitorNames(), name =>
            Processes.Run("grim", ["-t", "ppm", "-o", name, $"{dir}/{name}.ppm"], suppressOutput: true, check: true)));

        var socket = Timing.Measure("  overlay connect", () => Connect($"{dir}/sock"));

        if (socket == null)
            return null;

        try
        {
            socket.ReceiveTimeout = 1500;
            socket.Send(Encoding.ASCII.GetBytes($"show {Config.Transition} {Config.TransitionMs}\n"));

            var reply = new byte[16];
            int length = socket.Receive(reply);

            if (Encoding.ASCII.GetString(reply, 0, length).Trim() != "ready")
            {
                socket.Dispose();
                return null;
            }
        }
        catch
        {
            socket.Dispose();
            return null;
        }

        Timing.Mark("  overlay ready");

        foreach (var file in Directory.GetFiles(dir, "*.ppm"))
            File.Delete(file);

        return new Transition(socket);
    }

    private static Socket? Connect(string path)
    {
        var endpoint = new UnixDomainSocketEndPoint(path);
        var clock = Stopwatch.StartNew();

        while (clock.ElapsedMilliseconds < 1500)
        {
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

            try
            {
                socket.Connect(endpoint);
                return socket;
            }
            catch (SocketException)
            {
                socket.Dispose();
                Thread.Sleep(5);
            }
        }

        return null;
    }
}
