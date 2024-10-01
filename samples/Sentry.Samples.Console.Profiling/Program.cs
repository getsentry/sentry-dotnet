using System.Diagnostics;
using Sentry.Profiling;

internal static class Program
{
    private static void Main()
    {
        // Enable the SDK
        using (SentrySdk.Init(options =>
        {
            // You can set here in code, or you can set it in the SENTRY_DSN environment variable.
            // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
            options.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";

            options.Debug = true;
            // options.AutoSessionTracking = true;
            options.IsGlobalModeEnabled = true;
            options.TracesSampleRate = 1.0;

            // Make sure to reduce the sampling rate in production.
            options.ProfilesSampleRate = 1.0;

            // Debugging
            options.ShutdownTimeout = TimeSpan.FromMinutes(5);

            options.AddIntegration(new ProfilingIntegration(TimeSpan.FromMilliseconds(500)));
        }))
        {
            var tx = SentrySdk.StartTransaction("app", "run");
            var count = 10;
            for (var i = 0; i < count; i++)
            {
                FindPrimeNumber(100000);
            }

            tx.Finish();
            var sw = Stopwatch.StartNew();

            // Flushing takes 10 seconds consistently?
            SentrySdk.Flush(TimeSpan.FromMinutes(5));
            Console.WriteLine("Flushed in " + sw.Elapsed);

            // is the second profile faster?
            tx = SentrySdk.StartTransaction("app", "run");
            count = 10;
            for (var i = 0; i < count; i++)
            {
                FindPrimeNumber(100000);
            }

            tx.Finish();
            sw = Stopwatch.StartNew();

            // Flushing takes 10 seconds consistently?
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
