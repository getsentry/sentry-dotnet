using System;
using Xunit;

namespace Sentry.Tests
{
    public class SentryIdTests
    {
        [Fact]
        public void ToString_Equal_GuidToStringN()
        {
            var expected = Guid.NewGuid();
            SentryId actual = expected;
            Assert.Equal(expected.ToString("N"), actual.ToString());
        }
    }
}
