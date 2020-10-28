using System;
using Sentry;

namespace Sentry.Samples.Console.ManualUserFeedback
{
    public static class Program
    {
        static void Main()
        {
            using (SentrySdk.Init("https://80aed643f81249d4bed3e30687b310ab@o447951.ingest.sentry.io/5428537"))
            {
                SentrySdk.FlushAsync(new TimeSpan(0, 0, 30)).Wait();
            }
        }
    }
}
