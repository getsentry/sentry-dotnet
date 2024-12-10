using System.Diagnostics;

internal static class Program
{
    private static async Task Main()
    {
        // Enable the SDK
        using (SentrySdk.Init(options =>
        {
            // You can set here in code, or you can set it in the SENTRY_DSN environment variable.
            // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
            options.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";

            options.Debug = false;
            // options.AutoSessionTracking = true;
            options.IsGlobalModeEnabled = true;
            options.TracesSampleRate = 1.0;

            // Make sure to reduce the sampling rate in production.
            options.ProfilesSampleRate = 1.0;

            // Debugging
            options.ShutdownTimeout = TimeSpan.FromMinutes(5);

            options.AddProfilingIntegration(TimeSpan.FromMilliseconds(500));
        }))
        {
            var count = 10;

            var sw = Stopwatch.StartNew();
            var tx = SentrySdk.StartTransaction("FindPrimeNumber", "Sequential");
            for (var i = 0; i < count; i++)
            {
                FindPrimeNumber(100000);
            }
            tx.Finish();
            Console.WriteLine("Sequential computation finished in " + sw.Elapsed);
            SentrySdk.Flush(TimeSpan.FromMinutes(5));
            Console.WriteLine("Flushed in " + sw.Elapsed);
            await Task.Delay(500);

            sw.Restart();
            tx = SentrySdk.StartTransaction("FindPrimeNumber", "Parallel");
            var tasks = Enumerable.Range(1, count).ToList().Select(_ => Task.Run(async () =>
            {
                FindPrimeNumber(100000);
                await Task.Delay(500);
                FindPrimeNumber(100000);
            }));
            await Task.WhenAll(tasks).ConfigureAwait(false);
            tx.Finish();
            Console.WriteLine("Parallel computation finished in " + sw.Elapsed);
            SentrySdk.Flush(TimeSpan.FromMinutes(5));
            Console.WriteLine("Flushed in " + sw.Elapsed);
        }  // On Dispose: SDK closed, events queued are flushed/sent to Sentry
    }

    private static long FindPrimeNumber(int n)
    {
        int count = 0;
        long a = 2;
        while (count < n)
        {
            long b = 2;
            int prime = 1;// to check if found a prime
            while (b * b <= a)
            {
                if (a % b == 0)
                {
                    prime = 0;
                    break;
                }
                b++;
            }
            if (prime > 0)
            {
                count++;
            }
            a++;
        }
        return (--a);
    }
}
