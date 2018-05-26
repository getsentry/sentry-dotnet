namespace Sentry.Samples.Console.Basic
{
    static class Program
    {
        static void Main()
        {
            SentryCore.Init();

            // assuming it can find the DSN, the following exception is captured and sent to Sentry
            throw null;
        }
    }
}
