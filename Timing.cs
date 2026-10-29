using System.Diagnostics;

// Run hyprstyle with --timing (or set HYPRSTYLE_TIMING=1) to print a per-phase / per-component timing breakdown to stderr.
static class Timing
{
    public static bool Enabled = Environment.GetEnvironmentVariable("HYPRSTYLE_TIMING") == "1";
    private static readonly Stopwatch Clock = Stopwatch.StartNew();
    private static readonly object Lock = new();

    public static void Mark(string label)
    {
        if (!Enabled)
            return;

        lock (Lock)
            Console.Error.WriteLine($"[timing] {Clock.ElapsedMilliseconds,6} ms  {label}");
    }

    public static T Measure<T>(string label, Func<T> work)
    {
        if (!Enabled)
            return work();

        long start = Clock.ElapsedMilliseconds;
        try { return work(); }
        finally { Mark($"{label} (took {Clock.ElapsedMilliseconds - start} ms)"); }
    }

    public static void Measure(string label, Action work) =>
        Measure<int>(label, () => { work(); return 0; });
}

static class Concurrent
{
    // Dedicated threads instead of Parallel.ForEach: components block on child processes, and the
    // thread pool only injects ~1-2 extra threads per second when its workers are blocked, which
    // would stall any component beyond the core count.
    public static void ForEach<T>(IReadOnlyList<T> items, Action<T> work)
    {
        if (items.Count == 0)
            return;

        if (items.Count == 1)
        {
            work(items[0]);
            return;
        }

        var threads = new Thread[items.Count - 1];

        for (int i = 0; i < threads.Length; i++)
        {
            var item = items[i + 1];
            threads[i] = new Thread(() => work(item)) { IsBackground = true };
            threads[i].Start();
        }

        work(items[0]);

        foreach (var thread in threads)
            thread.Join();
    }
}
