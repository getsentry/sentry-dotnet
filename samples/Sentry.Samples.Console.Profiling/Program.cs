using System.Reflection;
using System.Xml.Xsl;
using Sentry;
using Sentry.Profiling;

internal static class Program
{
    private static void Main()
    {
        // Enable the SDK
        using (SentrySdk.Init(options =>
        {
            // A Sentry Data Source Name (DSN) is required.
            // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
            // You can set it in the SENTRY_DSN environment variable, or you can set it in code here.
            // options.Dsn = "... Your DSN ...";

            // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
            // This might be helpful, or might interfere with the normal operation of your application.
            // We enable it here for demonstration purposes.
            // You should not do this in your applications unless you are troubleshooting issues with Sentry.
            options.Debug = true;

            // This option is recommended, which enables Sentry's "Release Health" feature.
            options.AutoSessionTracking = true;

            // This option is recommended for client applications only. It ensures all threads use the same global scope.
            // If you are writing a background service of any kind, you should remove this.
            options.IsGlobalModeEnabled = true;

            // This option will enable Sentry's tracing features. You still need to start transactions and spans.
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
