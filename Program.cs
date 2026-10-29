static class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Config.EnsureExists();
            Cli.Usage();
            return;
        }

        if (RunQuickCommand(args))
            return;

        Directory.CreateDirectory(Config.StyleDir);

        bool showNotice = true;

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

            case "description" when args.Length >= 2:
                StyleManager.SetDescription(args[1], string.Join(' ', args[2..]));
                break;

            case "undo" when args.Length == 1:
                StyleManager.UndoLast();
                break;

            case "delete" when args.Length == 2:
                StyleManager.DeleteStyle(args[1]);
                break;

            case "json" when args.Length == 1:
                showNotice = false;
                StyleManager.PrintJson();
                break;

            case "gui" when args.Length == 1:
                showNotice = false;
                Gui.Open();
                break;

            case "update" when args.Length == 1:
                showNotice = false;
                Updater.Update();
                break;

            default:
                Cli.Usage();
                Environment.ExitCode = 1;
                break;
        }

        if (showNotice)
            Updater.ShowNotice();
    }

    private static bool RunQuickCommand(string[] args)
    {
        switch (args[0])
        {
            case "current":
                StyleManager.CurrentStyle();
                return true;

            case "version" or "--version":
                Console.WriteLine(Updater.CurrentVersion);
                return true;

            case "changelogs" when args.Length == 1:
                Changelogs.Print();
                return true;

            case "--save-components" when args.Length == 2:
                Config.SaveComponents(args[1]);
                return true;

            case "--refresh-update-cache":
                Updater.RefreshCache();
                return true;

            case "setpreview" when args.Length <= 3:
                Previews.Set(args[1..]);
                Updater.ShowNotice();
                return true;

            case "deletepreview" when args.Length <= 2:
                Previews.Delete(args.Length == 2 ? args[1] : null);
                Updater.ShowNotice();
                return true;

            default:
                return false;
        }
    }
}
