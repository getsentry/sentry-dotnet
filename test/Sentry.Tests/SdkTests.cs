using Xunit;

namespace Sentry.Tests
{
    public class SdkTests
    {
        [Fact]
        public void T()
        {
            // TODO: test SDK instance
            SentryCore.CaptureEvent(new SentryEvent());
        }
    }
}
