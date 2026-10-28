using System.ComponentModel;

static partial class Gui
{
    public static void Open()
    {
        string directory = Path.Join(Config.ConfigDir, "gui");
        string file = Path.Join(directory, "shell.qml");

        Directory.CreateDirectory(directory);
        File.WriteAllText(file, Source.Replace("@BIN@", Environment.ProcessPath ?? "hyprstyle"));

        Updater.CheckInBackground();

        try
        {
            Processes.Run("quickshell", ["-p", file]);
        }
        catch (Win32Exception)
        {
            Console.WriteLine("quickshell isn't installed or isn't on your PATH.");
            Cli.Notify("quickshell isn't installed or isn't on your PATH.");
            Environment.ExitCode = 1;
        }
    }
}
