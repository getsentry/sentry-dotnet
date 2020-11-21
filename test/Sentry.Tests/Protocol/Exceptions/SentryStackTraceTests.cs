using System.Collections.Generic;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Protocol.Exceptions
{
    public class SentryStackTraceTests
    {
        [Fact]
        public void Frames_Getter_NotNull()
        {
            var sut = new SentryStackTrace();
            Assert.NotNull(sut.Frames);
        }

        [Fact]
        public void Frames_Setter_ReplacesList()
        {
            var sut = new SentryStackTrace();
            var original = sut.Frames;
            var replacement = new List<SentryStackFrame>();
            sut.Frames = replacement;
            Assert.NotSame(original, replacement);
            Assert.Same(replacement, sut.Frames);
        }
    }
}
