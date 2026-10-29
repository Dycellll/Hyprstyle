static partial class Changelogs
{
    public sealed record Entry(string Version, string Log);

    private static readonly List<Entry> entries = [];

    public static List<Entry> Released
    {
        get
        {
            if (entries.Count == 0)
                Register();

            return entries
                .Where(entry => !Updater.IsNewer(entry.Version, Updater.CurrentVersion))
                .ToList();
        }
    }

    public static void AddChangelog(string version, string log)
    {
        var parsed = Updater.Parse(version)
            ?? throw new ArgumentException($"'{version}' isn't a valid version.");

        if (entries.Any(entry => Updater.Parse(entry.Version) == parsed))
            throw new ArgumentException($"There's already a changelog for {version}.");

        entries.Add(new Entry(Updater.Clean(version), log.Trim()));
        entries.Sort((a, b) => Updater.Parse(a.Version)!.CompareTo(Updater.Parse(b.Version)!));
    }

    public static void Print()
    {
        var released = Released;

        if (released.Count == 0)
        {
            Console.WriteLine("No changelogs yet.");
            return;
        }

        foreach (var entry in released)
        {
            Console.WriteLine();
            Console.WriteLine($"v{entry.Version}");

            foreach (var line in entry.Log.Split('\n'))
                Console.WriteLine($"  {line.TrimEnd()}");
        }

        Console.WriteLine();
    }
}
