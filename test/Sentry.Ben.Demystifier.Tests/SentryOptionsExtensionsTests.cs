using NSubstitute;
using Sentry.Extensibility;
using Xunit;

namespace Sentry.Ben.Demystifier.Tests
{
    public class SentryOptionsExtensionsTests
    {
        public SentryOptions Sut { get; set; } = new SentryOptions();

        [Fact]
        public void UseEnhancedStackTrace_AddEventProcessors()
        {
            var first = Sut.EventProcessors[0];
            var second =Sut.EventProcessors[1];
            var third = Substitute.For<ISentryEventProcessor>();
            Sut.AddEventProcessors(new[] { third });

            Sut.UseEnhancedStackTrace();

            Assert.Contains(Sut.EventProcessors, actual => actual == first);
            Assert.DoesNotContain(Sut.EventProcessors, actual => actual == second);
            Assert.Contains(Sut.EventProcessors, actual => actual == third);
        }

        [Fact]
        public void UseEnhancedStackTrace_AddExceptionProcessors()
        {
            var first = Sut.ExceptionProcessors[0];
            var second = Substitute.For<ISentryEventExceptionProcessor>();
            Sut.AddExceptionProcessors(new[] { second });

            Sut.UseEnhancedStackTrace();

            Assert.DoesNotContain(Sut.ExceptionProcessors, actual => actual == first);
            Assert.Contains(Sut.ExceptionProcessors, actual => actual == second);
        }
    }
}
