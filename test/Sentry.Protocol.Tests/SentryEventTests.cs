using Xunit;

namespace Sentry.Protocol.Tests
{
    public class SentryEventTests
    {
        [Fact]
        public void Ctor_Platform_CSharp()
        {
            var evt = new SentryEvent();

            Assert.Equal(Constants.Platform, evt.Platform);
        }
    }
}
