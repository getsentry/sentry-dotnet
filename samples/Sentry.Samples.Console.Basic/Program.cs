// One of the ways to set your DSN is via attribute:
[assembly: Sentry.Dsn("https://key@sentry.io/id")]

namespace Sentry.Samples.Console.Basic
{
    static class Program
    {
        static void Main()
        {
            SentryCore.Init();

            // The following exception is captured and sent to Sentry
            throw null;
        }
    }
}
