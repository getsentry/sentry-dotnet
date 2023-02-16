namespace ConsoleApp1
{
    using System;
    using Sentry;

    internal class Program
    {
        static void Main(string[] args)
        {

            using (var s = SentrySdk.Init(o =>
            {
               // The DSN is required, but for these samples, we recommend using the SENTRY_DSN environment variable.
               // If you prefer, paste your DSN in the code instead (uncomment the below line).
               // To learn more about DSN, see the https://docs.sentry.io/product/sentry-basics/dsn-explainer/ article.

               // o.Dsn = "";

               // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
               // Turn off in production. To learn more, see the https://docs.sentry.io/platforms/dotnet/configuration/options/#debug article.

#if DEBUG
               o.Debug = true;
#endif
            }))
            {
                // The following unhandled exception will be captured and sent to Sentry.
                throw new Exception("test");
            }
        }
    }
}
