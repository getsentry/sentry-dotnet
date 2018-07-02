using Sentry;

static class Program
{
    static void Main()
    {
        using (SentrySdk.Init("https://5fd7a6cda8444965bade9ccfd3df9882@sentry.io/1188141"))
        {
            // The following exception is captured and sent to Sentry
            throw null;
        }
    }
}
