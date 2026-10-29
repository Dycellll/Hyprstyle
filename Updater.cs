using System.Reflection;

static class Updater
{
    private const string Repo = "Dycellll/hyprstyle";
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
    private static readonly string CachePath = $"{Config.ConfigDir}/update_cache";

    private sealed record Cache(DateTimeOffset CheckedAt, string? Latest, string? Notified);

    public static string CurrentVersion { get; } = ReadVersion();

    public static string? LatestKnown => ReadCache().Latest;

    private static string ReadVersion()
    {
        string raw = typeof(Updater).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "0.0.0";

        int plus = raw.IndexOf('+');

        return plus >= 0 ? raw[..plus] : raw;
    }

    public static string Clean(string version) => version.Trim().TrimStart('v', 'V');

    public static Version? Parse(string text)
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

    private static string? Line(string[] lines, int index) =>
        index < lines.Length && lines[index].Trim() != "" ? lines[index].Trim() : null;

    private static Cache ReadCache()
    {
        try
        {
            string[] lines = File.ReadAllLines(CachePath);

            if (lines.Length > 0 && long.TryParse(lines[0], out long stamp))
                return new Cache(DateTimeOffset.FromUnixTimeSeconds(stamp), Line(lines, 1), Line(lines, 2));
        }
        catch
        {
        }

        return new Cache(DateTimeOffset.MinValue, null, null);
    }

    private static void WriteCache(Cache cache)
    {
        try
        {
            Files.EnsureParent(CachePath);

            File.WriteAllLines(CachePath,
            [
                cache.CheckedAt.ToUnixTimeSeconds().ToString(),
                cache.Latest ?? "",
                cache.Notified ?? ""
            ]);
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

    private static string NoticeText(string latest) =>
        $"Update available: {CurrentVersion} -> {Clean(latest)}. Run 'hyprstyle update' to install it.";

    public static void ShowNotice()
    {
        var cache = ReadCache();

        if (cache.Latest != null && IsNewer(cache.Latest, CurrentVersion))
            Console.Error.WriteLine(NoticeText(cache.Latest));

        CheckInBackground();
    }

    public static void CheckInBackground()
    {
        var cache = ReadCache();

        if (DateTimeOffset.UtcNow - cache.CheckedAt < CheckInterval)
            return;

        WriteCache(cache with { CheckedAt = DateTimeOffset.UtcNow });

        try
        {
            Processes.Start(Environment.ProcessPath ?? "hyprstyle", "--refresh-update-cache");
        }
        catch
        {
        }
    }

    public static void RefreshCache()
    {
        var cache = ReadCache();
        string? fetched = FetchLatest();
        string? notified = cache.Notified;

        if (fetched != null && IsNewer(fetched, CurrentVersion) && Clean(fetched) != notified)
        {
            Cli.Notify(NoticeText(fetched));
            notified = Clean(fetched);
        }

        WriteCache(new Cache(DateTimeOffset.UtcNow, fetched ?? cache.Latest, notified));
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

        WriteCache(new Cache(DateTimeOffset.UtcNow, latest, ReadCache().Notified));

        if (!IsNewer(latest, CurrentVersion))
        {
            Console.WriteLine($"Already up to date ({CurrentVersion}).");
            return;
        }

        Console.WriteLine($"Updating {CurrentVersion} -> {Clean(latest)}...");

        int exitCode = UpdateFromGitHub();

        if (exitCode != 0)
            Environment.ExitCode = exitCode;
    }

    private static int UpdateFromGitHub()
    {
        string script = $"f=$(mktemp) && curl -fsSL https://raw.githubusercontent.com/{Repo}/HEAD/install.sh -o \"$f\" && sh \"$f\" --update; code=$?; rm -f \"$f\"; exit $code";

        return Processes.Run("sh", ["-c", script]);
    }
}
