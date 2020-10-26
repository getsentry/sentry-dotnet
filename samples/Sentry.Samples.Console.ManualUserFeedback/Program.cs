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
                // The Message is captured and sent to Sentry
                var eventId = SentrySdk.CaptureMessage("Sample Manual User Feedback");

                SentrySdk.FlushAsync(new TimeSpan(0, 0, 30)).Wait();

                var timestamp = DateTime.Now.Ticks;
                var user = $"user{timestamp}";
                var email = $"user{timestamp}@user{timestamp}.com";
                SentrySdk.CaptureUserFeedback(new SentryUserFeedback(eventId, email, "this is a sample user feedback", user));
                SentrySdk.FlushAsync(new TimeSpan(0, 0, 30)).Wait();
            }
        }
    }
}
