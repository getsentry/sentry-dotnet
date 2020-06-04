using Sentry;

static class Program
{
    static void Main()
    {
        using (SentrySdk.Init("https://9f271c100c3248a4b074a0bead837061@o19635.ingest.sentry.io/5264714"))
        {
            // The following exception is captured and sent to Sentry
            throw null;
        }
    }
}
