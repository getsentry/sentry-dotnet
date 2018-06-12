using System;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Extensions.Logging.Tests
{
    public class SentryLoggerTests
    {
        private class Fixture
        {
            public string CategoryName { get; set; } = nameof(SentryLoggerTests);
            public ISystemClock Clock { get; set; } = Substitute.For<ISystemClock>();
            public IHub Hub { get; set; } = Substitute.For<IHub>();
            public SentryLoggingOptions Options { get; set; } = new SentryLoggingOptions();
            public Scope Scope { get; } = new Scope(new SentryOptions());

            public Fixture()
            {
                Hub.IsEnabled.Returns(true);
                Clock.GetUtcNow().Returns(DateTimeOffset.MaxValue);
                Hub.ConfigureScope(Arg.Invoke(Scope));
            }

            public SentryLogger GetSut() => new SentryLogger(CategoryName, Options, Clock, Hub);
        }

        private readonly Fixture _fixture = new Fixture();
        private const string BreadcrumbType = "default";

        [Fact]
        public void Log_InvokesSdkIsEnabled()
        {
            var sut = _fixture.GetSut();
            sut.Log(LogLevel.Critical, 1, "info", null, null);

            _ = _fixture.Hub.Received(1).IsEnabled;
        }

        [Fact]
        public void Log_WithException_EventMessageFromException()
        {
            var expectedException = new Exception("expected message");
            var sut = _fixture.GetSut();

            sut.Log<object>(LogLevel.Critical, default, null, expectedException, null);

            _fixture.Hub.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(
                    e => e.Message == expectedException.Message));
        }

        [Fact]
        public void Log_WithException_BreadcrumbFromException()
        {
            var expectedException = new Exception("expected message");
            const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Critical;

            var sut = _fixture.GetSut();

            sut.Log<object>(LogLevel.Critical, default, null, expectedException, null);

            var b = _fixture.Scope.Breadcrumbs[0];
            Assert.Equal(b.Message, expectedException.Message);
            Assert.Equal(b.Timestamp, _fixture.Clock.GetUtcNow());
            Assert.Equal(b.Category, _fixture.CategoryName);
            Assert.Equal(b.Level, expectedLevel);
            Assert.Equal(b.Type, BreadcrumbType);
            Assert.Null(b.Data);
        }

        [Fact]
        public void Log_WithExceptionAndMessage_ExceptionMessageAsTag()
        {
            var expectedMessage = "explicit message";
            var expectedException = new Exception("expected message");

            var sut = _fixture.GetSut();

            sut.Log<object>(
                LogLevel.Critical,
                default,
                expectedMessage,
                expectedException,
                (o, exception) => o.ToString());

            _fixture.Hub.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(
                    e => e.Message == expectedMessage
                         && e.Extra["original_message"] == expectedException.Message));
        }

        [Fact]
        public void Log_WithEventId_EventIdAsTagOnEvent()
        {
            var expectedEventId = new EventId(10, "EventId-!@#$%^&*(");

            var sut = _fixture.GetSut();

            sut.Log<object>(LogLevel.Critical, expectedEventId, null, null, null);

            _fixture.Hub.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(
                    e => e.Tags[EventIdExtensions.DataKey] == expectedEventId.ToString()));
        }

        [Fact]
        public void Log_WithEventId_EventIdAsBreadcrumbData()
        {
            var expectedEventId = new EventId(10, "EventId-!@#$%^&*(");
            const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Critical;

            var sut = _fixture.GetSut();

            sut.Log<object>(LogLevel.Critical, expectedEventId, null, null, null);

            var b = _fixture.Scope.Breadcrumbs[0];
            Assert.Equal(b.Data[EventIdExtensions.DataKey], expectedEventId.ToString());
            Assert.Equal(b.Timestamp, _fixture.Clock.GetUtcNow());
            Assert.Equal(b.Category, _fixture.CategoryName);
            Assert.Equal(b.Level, expectedLevel);
            Assert.Equal(b.Type, BreadcrumbType);
            Assert.Null(b.Message);
        }

        [Fact]
        public void LogCritical_DefaultOptions_CapturesEvent()
        {
            const string expected = "message";
            var sut = _fixture.GetSut();

            sut.LogCritical(expected);

            _fixture.Hub.Received(1)
                .CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void LogError_DefaultOptions_CapturesEvent()
        {
            const string expected = "message";
            var sut = _fixture.GetSut();

            sut.LogError(expected);

            _fixture.Hub.Received(1)
                .CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void LogCritical_DefaultOptions_RecordsBreadcrumbs()
        {
            const string expectedMessage = "message";
            const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Critical;

            var sut = _fixture.GetSut();

            sut.LogCritical(expectedMessage);

            var b = _fixture.Scope.Breadcrumbs[0];
            Assert.Equal(b.Message, expectedMessage);
            Assert.Equal(b.Timestamp, _fixture.Clock.GetUtcNow());
            Assert.Equal(b.Category, _fixture.CategoryName);
            Assert.Equal(b.Level, expectedLevel);
            Assert.Equal(b.Type, BreadcrumbType);
            Assert.Null(b.Data);
        }

        [Fact]
        public void LogError_DefaultOptions_RecordsBreadcrumbs()
        {
            const string expectedMessage = "message";
            const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Error;

            var sut = _fixture.GetSut();

            sut.LogError(expectedMessage);

            var b = _fixture.Scope.Breadcrumbs[0];
            Assert.Equal(b.Message, expectedMessage);
            Assert.Equal(b.Timestamp, _fixture.Clock.GetUtcNow());
            Assert.Equal(b.Category, _fixture.CategoryName);
            Assert.Equal(b.Level, expectedLevel);
            Assert.Equal(b.Type, BreadcrumbType);
            Assert.Null(b.Data);
        }

        [Fact]
        public void LogWarning_DefaultOptions_RecordsBreadcrumbs()
        {
            const string expectedMessage = "message";
            const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Warning;

            var sut = _fixture.GetSut();

            sut.LogWarning(expectedMessage);

            var b = _fixture.Scope.Breadcrumbs[0];
            Assert.Equal(b.Message, expectedMessage);
            Assert.Equal(b.Timestamp, _fixture.Clock.GetUtcNow());
            Assert.Equal(b.Category, _fixture.CategoryName);
            Assert.Equal(b.Level, expectedLevel);
            Assert.Equal(b.Type, BreadcrumbType);
            Assert.Null(b.Data);
        }

        [Fact]
        public void LogInformation_DefaultOptions_RecordsBreadcrumbs()
        {
            const string expectedMessage = "message";
            const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Info;

            var sut = _fixture.GetSut();

            sut.LogInformation(expectedMessage);

            var b = _fixture.Scope.Breadcrumbs[0];
            Assert.Equal(b.Message, expectedMessage);
            Assert.Equal(b.Timestamp, _fixture.Clock.GetUtcNow());
            Assert.Equal(b.Category, _fixture.CategoryName);
            Assert.Equal(b.Level, expectedLevel);
            Assert.Equal(b.Type, BreadcrumbType);
            Assert.Null(b.Data);
        }

        [Fact]
        public void LogDebug_DefaultOptions_DoesNotRecordsBreadcrumbs()
        {
            var sut = _fixture.GetSut();

            sut.LogDebug("antyhing");

            _fixture.Hub.DidNotReceive()
                .AddBreadcrumb(
                    _fixture.Clock,
                    Arg.Any<string>(),
                    BreadcrumbType,
                    _fixture.CategoryName,
                    null,
                    Arg.Any<BreadcrumbLevel>());
        }

        [Fact]
        public void LogTrace_DefaultOptions_DoesNotRecordsBreadcrumbs()
        {
            var sut = _fixture.GetSut();

            sut.LogTrace("antyhing");

            _fixture.Hub.DidNotReceive()
                .AddBreadcrumb(
                    _fixture.Clock,
                    Arg.Any<string>(),
                    BreadcrumbType,
                    _fixture.CategoryName,
                    null,
                    Arg.Any<BreadcrumbLevel>());
        }

        [Fact]
        public void IsEnabled_DisabledSdk_ReturnsFalse()
        {
            _fixture.Hub.IsEnabled.Returns(false);

            var sut = _fixture.GetSut();

            Assert.False(sut.IsEnabled(LogLevel.Critical));
        }

        [Fact]
        public void IsEnabled_EnabledSdk_ReturnsTrue()
        {
            _fixture.Hub.IsEnabled.Returns(true);

            var sut = _fixture.GetSut();

            Assert.True(sut.IsEnabled(LogLevel.Critical));
        }

        [Fact]
        public void IsEnabled_EnabledSdkLogLevelNone_ReturnsFalse()
        {
            _fixture.Hub.IsEnabled.Returns(true);

            var sut = _fixture.GetSut();

            Assert.False(sut.IsEnabled(LogLevel.None));
        }

        [Fact]
        public void IsEnabled_EnabledSdkLogLevelLower_ReturnsFalse()
        {
            _fixture.Options.MinimumBreadcrumbLevel = LogLevel.Critical;
            _fixture.Options.MinimumEventLevel = LogLevel.Critical;
            _fixture.Hub.IsEnabled.Returns(true);

            var sut = _fixture.GetSut();

            Assert.False(sut.IsEnabled(LogLevel.Error));
        }

        [Fact]
        public void IsEnabled_EnabledSdkLogLevelBreadcrumbLower_ReturnsTrue()
        {
            _fixture.Options.MinimumBreadcrumbLevel = LogLevel.Critical;
            _fixture.Options.MinimumEventLevel = LogLevel.Trace;
            _fixture.Hub.IsEnabled.Returns(true);

            var sut = _fixture.GetSut();

            Assert.True(sut.IsEnabled(LogLevel.Error));
        }

        [Fact]
        public void IsEnabled_EnabledSdkLogLevelEventLower_ReturnsTrue()
        {
            _fixture.Options.MinimumBreadcrumbLevel = LogLevel.Trace;
            _fixture.Options.MinimumEventLevel = LogLevel.Critical;
            _fixture.Hub.IsEnabled.Returns(true);

            var sut = _fixture.GetSut();

            Assert.True(sut.IsEnabled(LogLevel.Error));
        }

        [Fact]
        public void BeginScope_NullState_PushesScope()
        {
            var sut = _fixture.GetSut();
            sut.BeginScope<object>(null);
            _fixture.Hub.Received(1).PushScope<object>(null);
        }

        [Fact]
        public void BeginScope_StringState_PushesScope()
        {
            const string expected = "state";
            var sut = _fixture.GetSut();
            sut.BeginScope(expected);
            _fixture.Hub.Received(1).PushScope(expected);
        }

        [Fact]
        public void BeginScope_Disposable_Scope()
        {
            var expected = Substitute.For<IDisposable>();
            _fixture.Hub.PushScope(Arg.Any<string>()).Returns(expected);

            var sut = _fixture.GetSut();
            var actual = sut.BeginScope("state");

            Assert.Same(actual, expected);
        }
    }
}
