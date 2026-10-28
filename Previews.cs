using System.ComponentModel;

static class Previews
{
    public static void Capture(string stylePath)
    {
        try
        {
            Processes.Run("grim", ["-s", "0.4", Path.Join(stylePath, Styles.PreviewFile)], suppressOutput: true);
        }
        catch
        {
        }
    }

    public static void Set(string[] args)
    {
        string? name = null;
        string? image = null;

        if (args.Length == 2)
        {
            name = args[0];
            image = args[1];
        }
        else if (args.Length == 1)
        {
            if (Styles.FindStyle(args[0]) != null)
                name = args[0];
            else
                image = args[0];
        }

        name ??= Styles.CurrentName();

        if (name == null)
        {
            Cli.Fail("No style selected.");
            return;
        }

        string? path = Styles.FindStyle(name);

        if (path == null)
        {
            Cli.Fail($"Style '{name}' does not exist.");
            return;
        }

        string target = Path.Join(path, Styles.PreviewFile);

        if (image == null)
        {
            TakeScreenshot(path, target);
            return;
        }

        string source = image.StartsWith("file://")
            ? Uri.UnescapeDataString(image[7..])
            : Config.Expand(image);

        if (!File.Exists(source))
        {
            Cli.Fail($"Couldn't find {image}.");
            return;
        }

        Files.CopyFile(source, target);
        Console.WriteLine($"Updated preview for {Styles.NameOf(path)}");
    }

    public static void Delete(string? name)
    {
        name ??= Styles.CurrentName();

        if (name == null)
        {
            Cli.Fail("No style selected.");
            return;
        }

        string? path = Styles.FindStyle(name);

        if (path == null)
        {
            Cli.Fail($"Style '{name}' does not exist.");
            return;
        }

        string file = Path.Join(path, Styles.PreviewFile);

        if (!File.Exists(file))
        {
            Cli.Fail($"'{Styles.NameOf(path)}' has no preview.");
            return;
        }

        File.Delete(file);
        Console.WriteLine($"Removed preview from {Styles.NameOf(path)}");
    }

    private static void TakeScreenshot(string stylePath, string target)
    {
        try
        {
            if (Processes.Run("grim", ["-s", "0.4", target], suppressOutput: true) != 0)
                Cli.Fail("grim couldn't take a screenshot.");
            else
                Console.WriteLine($"Updated preview for {Styles.NameOf(stylePath)}");
        }
        catch (Win32Exception)
        {
            Cli.Fail("grim isn't installed or isn't on your PATH.");
            Cli.Notify("grim isn't installed or isn't on your PATH.");
        }
    }
}
