using System;
using NSubstitute;
using Sentry.Extensibility;
using Xunit;

namespace Sentry.Tests.Extensibility
{
    public class SentryEventExceptionProcessorTests
    {
        [Fact]
        public void Process_IncompatibleType_ProcessExceptionNotInvoked()
        {
            var sut = Substitute.For<SentryEventExceptionProcessor<InvalidOperationException>>();

            sut.Process(new Exception(), new SentryEvent());

            sut.DidNotReceive().ProcessException(Arg.Any<InvalidOperationException>(), Arg.Any<SentryEvent>());
        }

        [Fact]
        public void Process_ExactType_ProcessExceptionInvoked()
        {
            var sut = Substitute.For<SentryEventExceptionProcessor<Exception>>();

            sut.Process(new Exception(), new SentryEvent());

            sut.Received(1).ProcessException(Arg.Any<Exception>(), Arg.Any<SentryEvent>());
        }

        [Fact]
        public void Process_BaseType_ProcessExceptionInvoked()
        {
            var sut = Substitute.For<SentryEventExceptionProcessor<ArgumentException>>();

            sut.Process(new ArgumentNullException(), new SentryEvent());

            sut.Received(1).ProcessException(Arg.Any<ArgumentException>(), Arg.Any<SentryEvent>());
        }
    }
}
