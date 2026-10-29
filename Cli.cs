using System.ComponentModel;

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

    public static void Fail(string message)
    {
        Console.WriteLine(message);
        Environment.ExitCode = 1;
    }

    public static void Notify(string message)
    {
        try
        {
            Processes.Run("notify-send", ["hyprstyle", message], suppressOutput: true);
        }
        catch (Win32Exception)
        {
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
              hyprstyle setpreview [name] [image] -- Set a style's preview from an image, or from a screenshot if no image is given. Defaults to the current style.
              hyprstyle deletepreview [name] -- Remove a style's preview. Defaults to the current style.
              hyprstyle delete <name> -- Delete a saved style.
              hyprstyle gui -- Open the quickshell GUI.
              hyprstyle json -- Print all style data as JSON.
              hyprstyle update -- Update hyprstyle to the latest release.
              hyprstyle changelogs -- Show what changed in every version up to the one you have.
              hyprstyle version -- Print the installed version.

            Config: ~/.config/hyprstyle/config.json
            """
        );
    }
}
