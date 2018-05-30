using System;
using System.Collections.Generic;
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
            public ISdk Sdk { get; set; } = Substitute.For<ISdk>();
            public SentryLoggingOptions Options { get; set; } = new SentryLoggingOptions();

            public Fixture()
            {
                Sdk.IsEnabled.Returns(true);
            }

            public SentryLogger GetSut() => new SentryLogger(CategoryName, Options, Clock, Sdk);
        }

        private readonly Fixture _fixture = new Fixture();
        private const string BreadcrumbType = "logger";

        [Fact]
        public void Log_InvokesSdkIsEnabled()
        {
            var sut = _fixture.GetSut();
            sut.Log(LogLevel.Critical, 1, "info", null, null);

            _ = _fixture.Sdk.Received(1).IsEnabled;
        }

        [Fact]
        public void Log_WithException_EventMessageFromException()
        {
            var expectedException = new Exception("expected message");
            var sut = _fixture.GetSut();

            sut.Log<object>(LogLevel.Critical, default, null, expectedException, null);

            _fixture.Sdk.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(
                    e => e.Message == expectedException.Message));
        }

        [Fact]
        public void Log_WithException_BreadcrumbFromException()
        {
            var expectedException = new Exception("expected message");

            var sut = _fixture.GetSut();

            sut.Log<object>(LogLevel.Critical, default, null, expectedException, null);

            _fixture.Sdk.Received(1)
                .AddBreadcrumb(
                    _fixture.Clock,
                    expectedException.Message,
                    BreadcrumbType,
                    _fixture.CategoryName,
                    Arg.Is<Dictionary<string, string>>(
                        e => e["exception.message"] == expectedException.Message
                             && e["exception.stacktrace"] == expectedException.StackTrace),
                    BreadcrumbLevel.Critical);
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

            _fixture.Sdk.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(
                    e => e.Message == expectedMessage
                         && e.Tags["message"] == expectedException.Message));
        }

        [Fact]
        public void Log_WithEventId_EventIdAsTagOnEvent()
        {
            var expectedEventId = new EventId(10, "EventId-!@#$%^&*(");

            var sut = _fixture.GetSut();

            sut.Log<object>(LogLevel.Critical, expectedEventId, null, null, null);

            _fixture.Sdk.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(
                    e => e.Tags[EventIdExtensions.DataKey] == expectedEventId.ToString()));
        }

        [Fact]
        public void Log_WithEventId_EventIdAsBreadcrumbData()
        {
            var expectedEventId = new EventId(10, "EventId-!@#$%^&*(");

            var sut = _fixture.GetSut();

            sut.Log<object>(LogLevel.Critical, expectedEventId, null, null, null);

            _fixture.Sdk.Received(1)
                .AddBreadcrumb(
                    _fixture.Clock,
                    null,
                    BreadcrumbType,
                    _fixture.CategoryName,
                    Arg.Is<IDictionary<string, string>>(
                        e => e[EventIdExtensions.DataKey] == expectedEventId.ToString()),
                    BreadcrumbLevel.Critical);
        }

        [Fact]
        public void LogCritical_DefaultOptions_CapturesEvent()
        {
            const string expected = "message";
            var sut = _fixture.GetSut();

            sut.LogCritical(expected);

            _fixture.Sdk.Received(1)
                .CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void LogError_DefaultOptions_CapturesEvent()
        {
            const string expected = "message";
            var sut = _fixture.GetSut();

            sut.LogError(expected);

            _fixture.Sdk.Received(1)
                .CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void LogCritical_DefaultOptions_RecordsBreadcrumbs()
        {
            const string expected = "message";
            var sut = _fixture.GetSut();

            sut.LogCritical(expected);

            _fixture.Sdk.Received(1)
                .AddBreadcrumb(
                    _fixture.Clock,
                    expected,
                    BreadcrumbType,
                    _fixture.CategoryName,
                    null,
                    BreadcrumbLevel.Critical);
        }

        [Fact]
        public void LogError_DefaultOptions_RecordsBreadcrumbs()
        {
            const string expected = "message";
            var sut = _fixture.GetSut();

            sut.LogError(expected);

            _fixture.Sdk.Received(1)
                .AddBreadcrumb(
                    _fixture.Clock,
                    expected,
                    BreadcrumbType,
                    _fixture.CategoryName,
                    null,
                    BreadcrumbLevel.Error);
        }

        [Fact]
        public void LogWarning_DefaultOptions_RecordsBreadcrumbs()
        {
            const string expected = "message";
            var sut = _fixture.GetSut();

            sut.LogWarning(expected);

            _fixture.Sdk.Received(1)
                .AddBreadcrumb(
                    _fixture.Clock,
                    expected,
                    BreadcrumbType,
                    _fixture.CategoryName,
                    null,
                    BreadcrumbLevel.Warning);
        }

        [Fact]
        public void LogInformation_DefaultOptions_RecordsBreadcrumbs()
        {
            const string expected = "message";
            var sut = _fixture.GetSut();

            sut.LogInformation(expected);

            _fixture.Sdk.Received(1)
                .AddBreadcrumb(
                    _fixture.Clock,
                    expected,
                    BreadcrumbType,
                    _fixture.CategoryName,
                    null,
                    BreadcrumbLevel.Info);
        }

        [Fact]
        public void LogDebug_DefaultOptions_DoesNotRecordsBreadcrumbs()
        {
            var sut = _fixture.GetSut();

            sut.LogDebug("antyhing");

            _fixture.Sdk.DidNotReceive()
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

            _fixture.Sdk.DidNotReceive()
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
            _fixture.Sdk.IsEnabled.Returns(false);

            var sut = _fixture.GetSut();

            Assert.False(sut.IsEnabled(LogLevel.Critical));
        }

        [Fact]
        public void IsEnabled_EnabledSdk_ReturnsTrue()
        {
            _fixture.Sdk.IsEnabled.Returns(true);

            var sut = _fixture.GetSut();

            Assert.True(sut.IsEnabled(LogLevel.Critical));
        }

        [Fact]
        public void IsEnabled_EnabledSdkLogLevelNone_ReturnsFalse()
        {
            _fixture.Sdk.IsEnabled.Returns(true);

            var sut = _fixture.GetSut();

            Assert.False(sut.IsEnabled(LogLevel.None));
        }

        [Fact]
        public void IsEnabled_EnabledSdkLogLevelLower_ReturnsFalse()
        {
            _fixture.Options.MinimumBreadcrumbLevel = LogLevel.Critical;
            _fixture.Options.MinimumEventLevel = LogLevel.Critical;
            _fixture.Sdk.IsEnabled.Returns(true);

            var sut = _fixture.GetSut();

            Assert.False(sut.IsEnabled(LogLevel.Error));
        }

        [Fact]
        public void IsEnabled_EnabledSdkLogLevelBreadcrumbLower_ReturnsTrue()
        {
            _fixture.Options.MinimumBreadcrumbLevel = LogLevel.Critical;
            _fixture.Options.MinimumEventLevel = LogLevel.Trace;
            _fixture.Sdk.IsEnabled.Returns(true);

            var sut = _fixture.GetSut();

            Assert.True(sut.IsEnabled(LogLevel.Error));
        }

        [Fact]
        public void IsEnabled_EnabledSdkLogLevelEventLower_ReturnsTrue()
        {
            _fixture.Options.MinimumBreadcrumbLevel = LogLevel.Trace;
            _fixture.Options.MinimumEventLevel = LogLevel.Critical;
            _fixture.Sdk.IsEnabled.Returns(true);

            var sut = _fixture.GetSut();

            Assert.True(sut.IsEnabled(LogLevel.Error));
        }

        [Fact]
        public void BeginScope_NullState_PushesScope()
        {
            var sut = _fixture.GetSut();
            sut.BeginScope<object>(null);
            _fixture.Sdk.Received(1).PushScope<object>(null);
        }

        [Fact]
        public void BeginScope_StringState_PushesScope()
        {
            const string expected = "state";
            var sut = _fixture.GetSut();
            sut.BeginScope(expected);
            _fixture.Sdk.Received(1).PushScope(expected);
        }

        [Fact]
        public void BeginScope_Disposable_Scope()
        {
            var expected = Substitute.For<IDisposable>();
            _fixture.Sdk.PushScope(Arg.Any<string>()).Returns(expected);

            var sut = _fixture.GetSut();
            var actual = sut.BeginScope("state");

            Assert.Same(actual, expected);
        }
    }
}
