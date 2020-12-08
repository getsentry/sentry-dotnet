using System;
using Xunit;

namespace Sentry.Tests.Protocol
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

        [Fact]
        public void Implicit_ToGuid()
        {
            var expected = SentryId.Create();
            Guid actual = expected;
            Assert.Equal(expected.ToString(), actual.ToString("N"));
        }

        [Fact]
        public void Empty_Equal_GuidEmpty()
        {
            Assert.Equal(SentryId.Empty.ToString(), Guid.Empty.ToString("N"));
        }
    }
}
