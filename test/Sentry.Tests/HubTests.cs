using System;
using System.Linq;
using NSubstitute;
using Sentry;
using Sentry.Extensibility;
using Sentry.Internal;
using Xunit;

// ReSharper disable once CheckNamespace
// Tests code path which excludes frames with namespace Sentry
namespace NotSentry.Tests
{
    public class HubTests
    {
        private class Fixture
        {
            public SentryOptions SentryOptions { get; set; } = new SentryOptions();
            public IBackgroundWorker Worker { get; set; } = Substitute.For<IBackgroundWorker>();

            public Fixture()
            {
                SentryOptions.Dsn = DsnSamples.Valid;
                SentryOptions.BackgroundWorker = Worker;
            }

            public Hub GetSut() => new Hub(SentryOptions);
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void PushScope_BreadcrumbWithinScope_NotVisibleOutside()
        {
            var sut = _fixture.GetSut();

            using (sut.PushScope())
            {
                sut.ConfigureScope(s => s.AddBreadcrumb("test"));
                Assert.Single(sut.ScopeManager.GetCurrent().Key.Breadcrumbs);
            }

            Assert.Empty(sut.ScopeManager.GetCurrent().Key.Breadcrumbs);
        }

        [Fact]
        public void PushAndLockScope_DoesNotAffectOuterScope()
        {
            var sut = _fixture.GetSut();

            sut.ConfigureScope(s => Assert.False(s.Locked));
            using (sut.PushAndLockScope())
            {
                sut.ConfigureScope(s => Assert.True(s.Locked));
            }
            sut.ConfigureScope(s => Assert.False(s.Locked));
        }

        [Fact]
        public void CaptureMessage_AttachStacktraceTrue_IncludesStackTrace()
        {
            _fixture.SentryOptions.AttachStacktrace = true;

            var sut = _fixture.GetSut();

            sut.CaptureMessage("test");

            _fixture.Worker.Received(1)
                .EnqueueEvent(Arg.Do<SentryEvent>(
                    e => Assert.DoesNotContain(e.SentryExceptions.Single().Stacktrace.Frames,
                        p => p.Function == nameof(CaptureMessage_AttachStacktraceTrue_IncludesStackTrace))));
        }

        [Fact]
        public void CaptureMessage_AttachStacktraceFalse_IncludesStackTrace()
        {
            _fixture.SentryOptions.AttachStacktrace = false;

            var sut = _fixture.GetSut();

            sut.CaptureMessage("test");

            _fixture.Worker.Received(1)
                .EnqueueEvent(Arg.Is<SentryEvent>(e => e.SentryExceptionValues == null));
        }

        [Fact]
        public void CaptureMessage_FailedQueue_LastEventIdSetToEmpty()
        {
            var expectedId = Guid.Empty;
            _fixture.Worker.EnqueueEvent(Arg.Any<SentryEvent>()).Returns(false);
            var sut = _fixture.GetSut();

            var actualId = sut.CaptureMessage("test");

            Assert.Equal(expectedId, (Guid)actualId);
            Assert.Equal(expectedId, (Guid)sut.LastEventId);
        }

        [Fact]
        public void CaptureMessage_SuccessQueued_LastEventIdSetToReturnedId()
        {
            _fixture.Worker.EnqueueEvent(Arg.Any<SentryEvent>()).Returns(true);
            var sut = _fixture.GetSut();

            var actualId = sut.CaptureMessage("test");

            Assert.NotEqual(default, actualId);
            Assert.Equal(actualId, sut.LastEventId);
        }
    }
}
