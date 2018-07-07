using Sentry.Internal;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class SentryOptionsExtensionsTests
    {
        [Fact]
        public void Apply_ReleaseOnOptions_SetToEvent()
        {
            const string expectedVersion = "1.0 - f4d6b23";

            var options = new SentryOptions
            {
                Release = expectedVersion
            };

            var evt = new SentryEvent();
            options.Apply(evt);

            Assert.Equal(expectedVersion, evt.Release);
        }

        [Fact]
        public void Apply_NoReleaseOnOptions_SameAsCachedVersion()
        {
            var options = new SentryOptions();

            var evt = new SentryEvent();

            options.Apply(evt);
            Assert.Equal(SentryOptionsExtensions.Release.Value, evt.Release);
        }
    }
}
