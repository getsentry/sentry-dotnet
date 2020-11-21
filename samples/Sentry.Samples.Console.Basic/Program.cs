using Sentry;

static class Program
{
    static void Main()
    {
        using (SentrySdk.Init("https://80aed643f81249d4bed3e30687b310ab@o447951.ingest.sentry.io/5428537"))
        {
            // The following exception is captured and sent to Sentry
            throw null;
        }
    }
}
