using Sentry;

static class Program
{
    static void Main()
    {
        using (SentrySdk.Init("https://key@sentry.io/id"))
        {

            // The following exception is captured and sent to Sentry
            throw null;
        }
    }
}
