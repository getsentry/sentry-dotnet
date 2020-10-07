using System;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Sentry;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;
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
                SentryOptions.Dsn = DsnSamples.ValidDsnWithSecret;
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
                _ = Assert.Single(sut.ScopeManager.GetCurrent().Key.Breadcrumbs);
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

            _ = sut.CaptureMessage("test");

            _ = _fixture.Worker.Received(1).EnqueueEnvelope(Arg.Do<Envelope>(e =>
            {
                var functions = JToken.Parse(e.Items.Items.Single().Payload.Serialize())
                    .SelectTokens("..exception.stacktrace.frames[*].function")
                    .Select(j => j.Value<string>())
                    .ToArray();

                functions.Should().NotBeEmpty();
                functions.Should().NotContain(nameof(CaptureMessage_AttachStacktraceTrue_IncludesStackTrace));
            }));
        }

        [Fact]
        public void CaptureMessage_AttachStacktraceFalse_IncludesStackTrace()
        {
            _fixture.SentryOptions.AttachStacktrace = false;

            var sut = _fixture.GetSut();

            _ = sut.CaptureMessage("test");

            _ = _fixture.Worker.Received(1).EnqueueEnvelope(Arg.Do<Envelope>(e =>
            {
                var exceptions = JToken.Parse(e.Items.Items.Single().Payload.Serialize())
                    .SelectTokens("..exception")
                    .Select(j => j.Value<string>())
                    .ToArray();

                exceptions.Should().BeEmpty();
            }));
        }

        [Fact]
        public void CaptureMessage_FailedQueue_LastEventIdSetToEmpty()
        {
            var expectedId = Guid.Empty;
            _ = _fixture.Worker.EnqueueEnvelope(Arg.Any<Envelope>()).Returns(false);
            var sut = _fixture.GetSut();

            var actualId = sut.CaptureMessage("test");

            Assert.Equal(expectedId, (Guid)actualId);
            Assert.Equal(expectedId, (Guid)sut.LastEventId);
        }

        [Fact]
        public void CaptureMessage_SuccessQueued_LastEventIdSetToReturnedId()
        {
            _ = _fixture.Worker.EnqueueEnvelope(Arg.Any<Envelope>()).Returns(true);
            var sut = _fixture.GetSut();

            var actualId = sut.CaptureMessage("test");

            Assert.NotEqual(default, actualId);
            Assert.Equal(actualId, sut.LastEventId);
        }
    }
}
