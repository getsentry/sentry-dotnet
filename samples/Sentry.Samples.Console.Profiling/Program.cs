using Sentry;
using Sentry.Profiling;

internal static class Program
{
    private static void Main()
    {
        // Enable the SDK
        using (SentrySdk.Init(options =>
        {
            options.Dsn =
                // NOTE: ADD YOUR OWN DSN BELOW so you can see the events in your own Sentry account
                "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";

            options.Debug = true;
            options.AutoSessionTracking = true;
            options.IsGlobalModeEnabled = true;
            options.EnableTracing = true;

            options.AddIntegration(new ProfilingIntegration());
        }))
        {
            var tx = SentrySdk.StartTransaction("app", "run");
            var count = 10;
            for (var i = 0; i < count; i++)
            {
                FindPrimeNumber(100000);
            }
            tx.Finish();
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
