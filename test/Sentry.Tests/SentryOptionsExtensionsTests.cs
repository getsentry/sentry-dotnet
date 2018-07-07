using System.Linq;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Internal;
using Xunit;

namespace Sentry.Tests
{
    public class SentryOptionsExtensionsTests
    {
        public SentryOptions Sut { get; set; } = new SentryOptions();

        [Fact]
        public void AddExceptionProcessor_StoredInOptions()
        {
            var expected = Substitute.For<ISentryEventExceptionProcessor>();
            Sut.AddExceptionProcessor(expected);
            Assert.Contains(Sut.ExceptionProcessors, actual => actual == expected);
        }

        [Fact]
        public void AddExceptionProcessors_StoredInOptions()
        {
            var first = Substitute.For<ISentryEventExceptionProcessor>();
            var second = Substitute.For<ISentryEventExceptionProcessor>();
            Sut.AddExceptionProcessors(new[] { first, second });
            Assert.Contains(Sut.ExceptionProcessors, actual => actual == first);
            Assert.Contains(Sut.ExceptionProcessors, actual => actual == second);
        }

        [Fact]
        public void AddExceptionProcessorProvider_StoredInOptions()
        {
            var first = Substitute.For<ISentryEventExceptionProcessor>();
            var second = Substitute.For<ISentryEventExceptionProcessor>();
            Sut.AddExceptionProcessorProvider(() => new[] { first, second });
            Assert.Contains(Sut.GetAllExceptionProcessors(), actual => actual == first);
            Assert.Contains(Sut.GetAllExceptionProcessors(), actual => actual == second);
        }

        [Fact]
        public void AddExceptionProcessor_DoesNotExcludeMainProcessor()
        {
            Sut.AddExceptionProcessor(Substitute.For<ISentryEventExceptionProcessor>());
            Assert.Contains(Sut.ExceptionProcessors, actual => actual.GetType() == typeof(MainExceptionProcessor));
        }

        [Fact]
        public void AddExceptionProcessors_DoesNotExcludeMainProcessor()
        {
            Sut.AddExceptionProcessors(new[] { Substitute.For<ISentryEventExceptionProcessor>() });
            Assert.Contains(Sut.ExceptionProcessors, actual => actual.GetType() == typeof(MainExceptionProcessor));
        }

        [Fact]
        public void AddExceptionProcessorProvider_DoesNotExcludeMainProcessor()
        {
            Sut.AddExceptionProcessorProvider(() => new[] { Substitute.For<ISentryEventExceptionProcessor>() });
            Assert.Contains(Sut.ExceptionProcessors, actual => actual.GetType() == typeof(MainExceptionProcessor));
        }

        [Fact]
        public void GetAllExceptionProcessors_ReturnsMainSentryEventProcessor()
        {
            Assert.Contains(Sut.GetAllExceptionProcessors(), actual => actual.GetType() == typeof(MainExceptionProcessor));
        }

        [Fact]
        public void GetAllExceptionProcessors_FirstReturned_MainExceptionProcessor()
        {
            Sut.AddExceptionProcessorProvider(() => new[] { Substitute.For<ISentryEventExceptionProcessor>() });
            Sut.AddExceptionProcessors(new[] { Substitute.For<ISentryEventExceptionProcessor>() });
            Sut.AddExceptionProcessor(Substitute.For<ISentryEventExceptionProcessor>());

            Assert.IsType<MainExceptionProcessor>(Sut.GetAllExceptionProcessors().First());
        }

        [Fact]
        public void AddEventProcessor_StoredInOptions()
        {
            var expected = Substitute.For<ISentryEventProcessor>();
            Sut.AddEventProcessor(expected);
            Assert.Contains(Sut.EventProcessors, actual => actual == expected);
        }

        [Fact]
        public void AddEventProcessors_StoredInOptions()
        {
            var first = Substitute.For<ISentryEventProcessor>();
            var second = Substitute.For<ISentryEventProcessor>();
            Sut.AddEventProcessors(new[] { first, second });
            Assert.Contains(Sut.EventProcessors, actual => actual == first);
            Assert.Contains(Sut.EventProcessors, actual => actual == second);
        }

        [Fact]
        public void AddEventProcessorProvider_StoredInOptions()
        {
            var first = Substitute.For<ISentryEventProcessor>();
            var second = Substitute.For<ISentryEventProcessor>();
            Sut.AddEventProcessorProvider(() => new[] { first, second });
            Assert.Contains(Sut.GetAllEventProcessors(), actual => actual == first);
            Assert.Contains(Sut.GetAllEventProcessors(), actual => actual == second);
        }

        [Fact]
        public void AddEventProcessor_DoesNotExcludeMainProcessor()
        {
            Sut.AddEventProcessor(Substitute.For<ISentryEventProcessor>());
            Assert.Contains(Sut.EventProcessors, actual => actual.GetType() == typeof(MainSentryEventProcessor));
        }

        [Fact]
        public void AddEventProcessors_DoesNotExcludeMainProcessor()
        {
            Sut.AddEventProcessors(new[] { Substitute.For<ISentryEventProcessor>() });
            Assert.Contains(Sut.EventProcessors, actual => actual.GetType() == typeof(MainSentryEventProcessor));
        }

        [Fact]
        public void AddEventProcessorProvider_DoesNotExcludeMainProcessor()
        {
            Sut.AddEventProcessorProvider(() => new[] { Substitute.For<ISentryEventProcessor>() });
            Assert.Contains(Sut.EventProcessors, actual => actual.GetType() == typeof(MainSentryEventProcessor));
        }

        [Fact]
        public void GetAllEventProcessors_ReturnsMainSentryEventProcessor()
        {
            Assert.Contains(Sut.GetAllEventProcessors(), actual => actual.GetType() == typeof(MainSentryEventProcessor));
        }


        [Fact]
        public void GetAllEventProcessors_FirstReturned_MainSentryEventProcessor()
        {
            Sut.AddEventProcessorProvider(() => new[] { Substitute.For<ISentryEventProcessor>() });
            Sut.AddEventProcessors(new[] { Substitute.For<ISentryEventProcessor>() });
            Sut.AddEventProcessor(Substitute.For<ISentryEventProcessor>());

            Assert.IsType<MainSentryEventProcessor>(Sut.GetAllEventProcessors().First());
        }
    }
}
