using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sentry.Infrastructure;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Extensions.Logging.Tests
{
    public class SentryLoggerTests
    {
        private class Fixture
        {
            public string CategoryName { get; set; } = "SomeApp";
            public ISystemClock Clock { get; set; } = Substitute.For<ISystemClock>();
            public IHub Hub { get; set; } = Substitute.For<IHub>();
            public SentryLoggingOptions Options { get; set; } = new();
            public Scope Scope { get; } = new(new SentryOptions());

            public Fixture()
            {
                _ = Hub.IsEnabled.Returns(true);
                _ = Clock.GetUtcNow().Returns(DateTimeOffset.MaxValue);
                Hub.ConfigureScope(Arg.Invoke(Scope));
            }

            public SentryLogger GetSut() => new(CategoryName, Options, Clock, Hub);
        }

        private readonly Fixture _fixture = new();
        private const string BreadcrumbType = null;

        [Fact]
        public void Log_InvokesSdkIsEnabled()
        {
            var sut = _fixture.GetSut();
            sut.Log(LogLevel.Critical, 1, "info", null, null);

            _ = _fixture.Hub.Received(1).IsEnabled;
        }

        [Fact]
        public void Log_WithException_BreadcrumbFromException()
        {
            var expectedException = new Exception("expected message");
            const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Critical;

            var sut = _fixture.GetSut();

            sut.Log<object>(LogLevel.Critical, default, null, expectedException, null);

            var b = _fixture.Scope.Breadcrumbs.First();
            Assert.Equal(b.Message, expectedException.Message);
            Assert.Equal(b.Timestamp, _fixture.Clock.GetUtcNow());
            Assert.Equal(b.Category, _fixture.CategoryName);
            Assert.Equal(b.Level, expectedLevel);
            Assert.Equal(b.Type, BreadcrumbType);
            Assert.Null(b.Data);
        }

        [Fact]
        public void Log_WithEventId_EventIdAsTagOnEvent()
        {
            var expectedEventId = new EventId(10, "EventId-!@#$%^&*(");

            var sut = _fixture.GetSut();

            sut.Log<object>(LogLevel.Critical, expectedEventId, null, null, null);

            _ = _fixture.Hub.Received(1)
                    .CaptureEvent(Arg.Is<SentryEvent>(
                        e => e.Tags[EventIdExtensions.DataKey] == expectedEventId.ToString()));
        }

        [Fact]
        public void Log_WithProperties_SetsTagsInEvent()
        {
            var props = new List<KeyValuePair<string, object>>
            {
                new("foo", "bar")
            };
            var sut = _fixture.GetSut();

            sut.Log<object>(LogLevel.Critical, default, props, null, null);

            _ = _fixture.Hub.Received(1)
                    .CaptureEvent(Arg.Is<SentryEvent>(
                        e => e.Tags["foo"] == "bar"));
        }

        [Fact]
        public void Log_WithEventId_EventIdAsBreadcrumbData()
        {
            var expectedEventId = new EventId(10, "EventId-!@#$%^&*(");
            const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Critical;

            var sut = _fixture.GetSut();

            sut.Log<object>(LogLevel.Critical, expectedEventId, null, null, null);

            var b = _fixture.Scope.Breadcrumbs.First();
            Assert.Equal(b.Data![EventIdExtensions.DataKey], expectedEventId.ToString());
            Assert.Equal(b.Timestamp, _fixture.Clock.GetUtcNow());
            Assert.Equal(b.Category, _fixture.CategoryName);
            Assert.Equal(b.Level, expectedLevel);
            Assert.Equal(b.Type, BreadcrumbType);
            Assert.Null(b.Message);
        }

        [Fact]
        public void LogCritical_MatchingFilter_DoesNotCapturesEvent()
        {
            const string expected = "message";
            _fixture.Options.AddLogEntryFilter((_, _, _, _) => false);
            _fixture.Options.AddLogEntryFilter((_, _, _, _) => true);

            var sut = _fixture.GetSut();

            sut.LogCritical(expected);

            _ = _fixture.Hub.DidNotReceive()
                    .CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void LogCritical_MatchingFilter_DoesNotAddBreadcrumb()
        {
            const string expected = "message";
            _fixture.Options.AddLogEntryFilter((_, _, _, _) => false);
            _fixture.Options.AddLogEntryFilter((_, _, _, _) => true);

            var sut = _fixture.GetSut();

            sut.LogCritical(expected);

            _fixture.Hub.DidNotReceive()
                // Breadcrumbs live in the scope
                .ConfigureScope(Arg.Any<Action<Scope>>());
        }

        [Fact]
        public void LogCritical_NotMatchingFilter_CapturesEvent()
        {
            const string expected = "message";
            _fixture.Options.AddLogEntryFilter((_, _, _, _) => false);
            _fixture.Options.AddLogEntryFilter((_, _, _, _) => false);

            var sut = _fixture.GetSut();

            sut.LogCritical(expected);

            _ = _fixture.Hub.Received(1)
                    .CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void LogCritical_NotMatchingFilter_AddsBreadcrumb()
        {
            var scope = new Scope();
            _fixture.Hub.ConfigureScope(Arg.Invoke(scope));

            const string expected = "message";
            _fixture.Options.AddLogEntryFilter((_, _, _, _) => false);
            _fixture.Options.AddLogEntryFilter((_, _, _, _) => false);

            var sut = _fixture.GetSut();

            sut.LogCritical(expected);

            _fixture.Hub.Received(1)
                .ConfigureScope(Arg.Any<Action<Scope>>());

            _ = Assert.Single(scope.Breadcrumbs);
        }

        [Fact]
        public void LogCritical_DefaultOptions_CapturesEvent()
        {
            const string expected = "message";
            var sut = _fixture.GetSut();

            sut.LogCritical(expected);

            _ = _fixture.Hub.Received(1)
                    .CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void LogError_DefaultOptions_CapturesEvent()
        {
            const string expected = "message";
            var sut = _fixture.GetSut();

            sut.LogError(expected);

            _ = _fixture.Hub.Received(1)
                    .CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void Log_SentryCategory_DoesNotSendEvent()
        {
            var expectedException = new Exception("expected message");
            _fixture.CategoryName = "Sentry.Some.Class";
            var sut = _fixture.GetSut();

            sut.Log<object>(LogLevel.Critical, default, null, expectedException, null);

            _ = _fixture.Hub.DidNotReceive()
                    .CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void Log_SentryRootCategory_DoesNotSendEvent()
        {
            var expectedException = new Exception("expected message");
            _fixture.CategoryName = "Sentry";
            var sut = _fixture.GetSut();

            sut.Log<object>(LogLevel.Critical, default, null, expectedException, null);

            _ = _fixture.Hub.DidNotReceive()
                    .CaptureEvent(Arg.Any<SentryEvent>());
        }

        // https://github.com/getsentry/sentry-dotnet/issues/132
        [Fact]
        public void Log_SentrySomethingCategory_SendEvent()
        {
            var expectedException = new Exception("expected message");
            _fixture.CategoryName = "SentrySomething";
            var sut = _fixture.GetSut();

            sut.Log<object>(LogLevel.Critical, default, null, expectedException, null);

            _ = _fixture.Hub.Received(1)
                    .CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void LogCritical_DefaultOptions_RecordsBreadcrumbs()
        {
            const string expectedMessage = "message";
            const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Critical;

            var sut = _fixture.GetSut();

            sut.LogCritical(expectedMessage);

            var b = _fixture.Scope.Breadcrumbs.First();
            Assert.Equal(b.Message, expectedMessage);
            Assert.Equal(b.Timestamp, _fixture.Clock.GetUtcNow());
            Assert.Equal(b.Category, _fixture.CategoryName);
            Assert.Equal(b.Level, expectedLevel);
            Assert.Equal(b.Type, BreadcrumbType);
            Assert.Null(b.Data);
        }

        [Fact]
        public void LogCritical_ExceptionAndMessage_ExceptionMessageAsBreadcrumbData()
        {
            const string expectedMessage = "message";
            var exception = new Exception("exception message");

            var sut = _fixture.GetSut();

            sut.LogCritical(exception, expectedMessage);

            var b = _fixture.Scope.Breadcrumbs.First();
            Assert.Contains(b.Data,
                pair => pair.Key == "exception_message" && pair.Value == exception.Message);
            Assert.Equal(expectedMessage, b.Message);
        }

        [Fact]
        public void LogCritical_ExceptionAndMessageAndEventId_ExceptionMessageAndEventIdAsBreadcrumbData()
        {
            const string expectedMessage = "message";
            const int expectedEventId = 1;
            var exception = new Exception("exception message");

            var sut = _fixture.GetSut();

            sut.LogCritical(expectedEventId, exception, expectedMessage);

            var b = _fixture.Scope.Breadcrumbs.First();
            Assert.Contains(b.Data,
                pair => pair.Key == "exception_message"
                        && pair.Value == exception.Message);
            Assert.Contains(b.Data,
                pair => pair.Key == EventIdExtensions.DataKey
                         && pair.Value == expectedEventId.ToString());
            Assert.Equal(expectedMessage, b.Message);
        }

        [Fact]
        public void LogError_DefaultOptions_RecordsBreadcrumbs()
        {
            const string expectedMessage = "message";
            const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Error;

            var sut = _fixture.GetSut();

            sut.LogError(expectedMessage);

            var b = _fixture.Scope.Breadcrumbs.First();
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

            var b = _fixture.Scope.Breadcrumbs.First();
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

            var b = _fixture.Scope.Breadcrumbs.First();
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

            sut.LogDebug("anything");


            _fixture.Hub.DidNotReceiveWithAnyArgs()
                .AddBreadcrumb(
                    default,
                    string.Empty,
                    default,
                    default,
                    default,
                    default);

        }

        [Fact]
        public void LogTrace_DefaultOptions_DoesNotRecordsBreadcrumbs()
        {
            var sut = _fixture.GetSut();

            sut.LogTrace("anything");

            _fixture.Hub.DidNotReceiveWithAnyArgs()
                .AddBreadcrumb(
                    default,
                    string.Empty,
                    default,
                    default,
                    default,
                    default);

        }

        [Fact]
        public void IsEnabled_DisabledSdk_ReturnsFalse()
        {
            _ = _fixture.Hub.IsEnabled.Returns(false);

            var sut = _fixture.GetSut();

            Assert.False(sut.IsEnabled(LogLevel.Critical));
        }

        [Fact]
        public void IsEnabled_EnabledSdk_ReturnsTrue()
        {
            _ = _fixture.Hub.IsEnabled.Returns(true);

            var sut = _fixture.GetSut();

            Assert.True(sut.IsEnabled(LogLevel.Critical));
        }

        [Fact]
        public void IsEnabled_EnabledSdkLogLevelNone_ReturnsFalse()
        {
            _ = _fixture.Hub.IsEnabled.Returns(true);

            var sut = _fixture.GetSut();

            Assert.False(sut.IsEnabled(LogLevel.None));
        }

        [Fact]
        public void IsEnabled_EnabledSdkLogLevelLower_ReturnsFalse()
        {
            _fixture.Options.MinimumBreadcrumbLevel = LogLevel.Critical;
            _fixture.Options.MinimumEventLevel = LogLevel.Critical;
            _ = _fixture.Hub.IsEnabled.Returns(true);

            var sut = _fixture.GetSut();

            Assert.False(sut.IsEnabled(LogLevel.Error));
        }

        [Fact]
        public void IsEnabled_EnabledSdkLogLevelBreadcrumbLower_ReturnsTrue()
        {
            _fixture.Options.MinimumBreadcrumbLevel = LogLevel.Critical;
            _fixture.Options.MinimumEventLevel = LogLevel.Trace;
            _ = _fixture.Hub.IsEnabled.Returns(true);

            var sut = _fixture.GetSut();

            Assert.True(sut.IsEnabled(LogLevel.Error));
        }

        [Fact]
        public void IsEnabled_EnabledSdkLogLevelEventLower_ReturnsTrue()
        {
            _fixture.Options.MinimumBreadcrumbLevel = LogLevel.Trace;
            _fixture.Options.MinimumEventLevel = LogLevel.Critical;
            _ = _fixture.Hub.IsEnabled.Returns(true);

            var sut = _fixture.GetSut();

            Assert.True(sut.IsEnabled(LogLevel.Error));
        }

        [Fact]
        public void BeginScope_NullState_PushesScope()
        {
            var sut = _fixture.GetSut();
            _ = sut.BeginScope<object>(null);
            _ = _fixture.Hub.Received(1).PushScope<object>(null);
        }

        [Fact]
        public void BeginScope_StringState_PushesScope()
        {
            const string expected = "state";
            var sut = _fixture.GetSut();
            _ = sut.BeginScope(expected);
            _ = _fixture.Hub.Received(1).PushScope(expected);
        }

        [Fact]
        public void BeginScope_Disposable_Scope()
        {
            var expected = Substitute.For<IDisposable>();
            _ = _fixture.Hub.PushScope(Arg.Any<string>()).Returns(expected);

            var sut = _fixture.GetSut();
            var actual = sut.BeginScope("state");

            Assert.Same(actual, expected);
        }
    }
}
