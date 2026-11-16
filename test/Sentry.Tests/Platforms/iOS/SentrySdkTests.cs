using System;
using System.Collections.Generic;
using FluentAssertions;
using Foundation;
using NSubstitute;
using Sentry.Cocoa;
using Sentry.Cocoa.Extensions;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests.Platforms.iOS;

public class SentrySdkTests
{
    [Theory]
    [InlineData("BeforeSend")]
    [InlineData("Options")]
    [InlineData("Provider")]
    [InlineData("Scope")]
    public void ProcessOnBeforeSend_InputConversionFails_DropsEventWithoutInvokingFilter(string registration)
    {
        var logger = new InMemoryDiagnosticLogger();
        var options = new SentryOptions { Debug = true, DiagnosticLogger = logger };
        var recorder = Substitute.For<IClientReportRecorder>();
        options.ClientReportRecorder = recorder;
        var scope = new Scope(options);
        var called = false;
        RegisterFilter(options, scope, registration, evt =>
        {
            called = true;
            return evt;
        });
        using var native = CreateUnconvertibleEvent();
        Action convert = () => native.ToSentryEvent();
        convert.Should().Throw<InvalidOperationException>();

        var result = SentrySdk.ProcessOnBeforeSend(options, native, CreateHub(scope));

        result.Should().BeNull();
        called.Should().BeFalse();
        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == SentryLevel.Error &&
            entry.Exception is InvalidOperationException &&
            entry.Message == "Error running managed event processors for native event");
        recorder.DidNotReceive().RecordDiscardedEvent(
            Arg.Any<DiscardReason>(), Arg.Any<DataCategory>(), Arg.Any<int>());
    }

    [Fact]
    public void ProcessOnBeforeSend_OnlySdkProcessors_SkipsConversion()
    {
        var logger = new InMemoryDiagnosticLogger();
        var options = new SentryOptions { Debug = true, DiagnosticLogger = logger };
        options.AddEventProcessor(new CocoaEventProcessor());
        var scope = new Scope(options);
        scope.GetAllEventProcessors().Should().Contain(processor => processor is DuplicateEventDetectionEventProcessor);
        scope.GetAllEventProcessors().Should().Contain(processor => processor is CocoaEventProcessor);
        scope.GetAllEventProcessors().Should().OnlyContain(processor => processor is ISdkProcessor);
        using var native = CreateUnconvertibleEvent();
        Action convert = () => native.ToSentryEvent();
        convert.Should().Throw<InvalidOperationException>();

        var result = SentrySdk.ProcessOnBeforeSend(options, native, CreateHub(scope));

        result.Should().BeSameAs(native);
        logger.Entries.Should().NotContain(entry => entry.Level == SentryLevel.Error);
    }

    [Fact]
    public void ProcessOnBeforeSend_CopyBackFails_DropsPartiallyMutatedEvent()
    {
        var exception = new InvalidOperationException("Cannot stringify message parameter");
        var logger = new InMemoryDiagnosticLogger();
        var options = new SentryOptions { Debug = true, DiagnosticLogger = logger };
        var recorder = Substitute.For<IClientReportRecorder>();
        options.ClientReportRecorder = recorder;
        var called = false;
        options.SetBeforeSend(evt =>
        {
            called = true;
            evt.ServerName = "filtered";
            evt.User.Email = null;
            evt.Message = new SentryMessage { Params = [new ThrowingMessageParameter(exception)] };
            return evt;
        });
        using var user = new CocoaSdk.SentryObjCUser { Email = "private@example.com" };
        using var native = new CocoaSdk.SentryObjCEvent { ServerName = "private-host", User = user };
        native.ToSentryEvent().Should().NotBeNull();

        var result = SentrySdk.ProcessOnBeforeSend(options, native, CreateHub(new Scope(options)));

        called.Should().BeTrue();
        result.Should().BeNull();
        native.ServerName.Should().Be("filtered");
        native.User.Email.Should().Be("private@example.com");
        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == SentryLevel.Error &&
            entry.Exception == exception &&
            entry.Message == "Error running managed event processors for native event");
        recorder.DidNotReceive().RecordDiscardedEvent(
            Arg.Any<DiscardReason>(), Arg.Any<DataCategory>(), Arg.Any<int>());
    }

    [Theory]
    [InlineData("BeforeSend")]
    [InlineData("Options")]
    [InlineData("Provider")]
    [InlineData("Scope")]
    public void ProcessOnBeforeSend_ApplicationFilterRedactsUser_CopiesRedactionBack(string registration)
    {
        var options = new SentryOptions();
        var scope = new Scope(options);
        var called = false;
        RegisterFilter(options, scope, registration, evt =>
        {
            called = true;
            evt.User.Email = null;
            return evt;
        });
        using var user = new CocoaSdk.SentryObjCUser { Email = "private@example.com", UserId = "user-id" };
        using var native = new CocoaSdk.SentryObjCEvent { User = user };
        var eventId = native.EventId.SentryIdString;

        var result = SentrySdk.ProcessOnBeforeSend(options, native, CreateHub(scope));

        called.Should().BeTrue();
        result.Should().BeSameAs(native);
        native.EventId.SentryIdString.Should().Be(eventId);
        native.User.Email.Should().BeNull();
        native.User.UserId.Should().Be("user-id");
    }

    [Fact]
    public void ProcessOnBeforeSend_ApplicationProcessors_RunBeforeBeforeSendInOrder()
    {
        var options = new SentryOptions();
        var scope = new Scope(options);
        var calls = new List<string>();
        foreach (var registration in new[] { "Options", "Provider", "Scope", "BeforeSend" })
        {
            RegisterFilter(options, scope, registration, evt =>
            {
                calls.Add(registration);
                return evt;
            });
        }
        using var native = new CocoaSdk.SentryObjCEvent();

        SentrySdk.ProcessOnBeforeSend(options, native, CreateHub(scope)).Should().BeSameAs(native);

        calls.Should().Equal("Options", "Provider", "Scope", "BeforeSend");
    }

    [Fact]
    public void ProcessOnBeforeSend_ProcessorDiscoveryThrows_DropsEventWithoutManagedDiscard()
    {
        var exception = new InvalidOperationException("Cannot discover processors");
        var logger = new InMemoryDiagnosticLogger();
        var options = new SentryOptions { Debug = true, DiagnosticLogger = logger };
        var recorder = Substitute.For<IClientReportRecorder>();
        options.ClientReportRecorder = recorder;
        var calls = 0;
        options.AddEventProcessorProvider(() =>
        {
            calls++;
            throw exception;
        });
        using var native = new CocoaSdk.SentryObjCEvent();

        var result = SentrySdk.ProcessOnBeforeSend(options, native, CreateHub(new Scope(options)));

        result.Should().BeNull();
        calls.Should().Be(1);
        logger.Entries.Should().ContainSingle(entry => entry.Exception == exception);
        recorder.DidNotReceive().RecordDiscardedEvent(
            Arg.Any<DiscardReason>(), Arg.Any<DataCategory>(), Arg.Any<int>());
    }

    [Theory]
    [InlineData("SIGABRT", false, false)]
    [InlineData("SIGABRT", false, true)]
    [InlineData("SIGABRT", true, false)]
    [InlineData("EXC_BAD_ACCESS", false, false)]
    public void ProcessOnBeforeSend_SuppressedCrash_DropsBeforeFilteringOrSdkOnlyBypass(
        string type, bool disabledHub, bool userFilter)
    {
        var logger = new InMemoryDiagnosticLogger();
        var options = new SentryOptions { Debug = true, DiagnosticLogger = logger };
        options.Native.SuppressSignalAborts = true;
        options.Native.SuppressExcBadAccess = true;
        var called = false;
        if (userFilter)
        {
            options.SetBeforeSend(evt =>
            {
                called = true;
                return evt;
            });
        }
        using var frame = new CocoaSdk.SentryObjCFrame { Function = "xamarin_unhandled_exception_handler" };
        using var registers = new NSDictionary<NSString, NSString>();
        using var stacktrace = new CocoaSdk.SentryObjCStacktrace([frame], registers);
        using var exception = new CocoaSdk.SentryObjCException("Signal 6, Code 0", type) { Stacktrace = stacktrace };
        using var native = userFilter ? new CocoaSdk.SentryObjCEvent() : CreateUnconvertibleEvent();
        native.Exceptions = [exception];
        if (userFilter)
        {
            native.ToSentryEvent().Should().NotBeNull();
        }
        var scope = new Scope(options);
        IHub hub = disabledHub ? DisabledHub.Instance : CreateHub(scope);

        var result = SentrySdk.ProcessOnBeforeSend(options, native, hub);

        result.Should().BeNull();
        called.Should().BeFalse();
        logger.Entries.Should().NotContain(entry => entry.Level == SentryLevel.Error);
    }

    [Theory]
    [InlineData("SIGABRT", "Other", "xamarin_unhandled_exception_handler")]
    [InlineData("SIGABRT", "Signal 6, Code 0", "other_handler")]
    [InlineData("Other", "Signal 6, Code 0", "xamarin_unhandled_exception_handler")]
    public void ProcessOnBeforeSend_NonmatchingCrash_ReachesApplicationFilter(string type, string value, string function)
    {
        var options = new SentryOptions();
        options.Native.SuppressSignalAborts = true;
        options.Native.SuppressExcBadAccess = true;
        var called = false;
        options.SetBeforeSend(evt =>
        {
            called = true;
            evt.ServerName = "filtered";
            return evt;
        });
        using var frame = new CocoaSdk.SentryObjCFrame { Function = function };
        using var registers = new NSDictionary<NSString, NSString>();
        using var stacktrace = new CocoaSdk.SentryObjCStacktrace([frame], registers);
        using var exception = new CocoaSdk.SentryObjCException(value, type) { Stacktrace = stacktrace };
        using var native = new CocoaSdk.SentryObjCEvent { Exceptions = [exception] };

        var result = SentrySdk.ProcessOnBeforeSend(options, native, CreateHub(new Scope(options)));

        called.Should().BeTrue();
        result.Should().BeSameAs(native);
        native.ServerName.Should().Be("filtered");
    }

    private static CocoaSdk.SentryObjCEvent CreateUnconvertibleEvent()
    {
        using var key = new NSString("device");
        using var device = new Dictionary<string, string> { ["memory_size"] = "not-a-number" }.ToNSDictionary();
        using var contexts = new NSDictionary<NSString, NSDictionary<NSString, NSObject>>(key, device);
        return new CocoaSdk.SentryObjCEvent { Context = contexts };
    }

    private static IHub CreateHub(Scope scope)
    {
        var hub = Substitute.For<IHub>();
        hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
            .Do(callback => callback.Arg<Action<Scope>>().Invoke(scope));
        return hub;
    }

    private static void RegisterFilter(SentryOptions options, Scope scope, string registration,
        Func<SentryEvent, SentryEvent> filter)
    {
        switch (registration)
        {
            case "BeforeSend":
                options.SetBeforeSend(filter);
                break;
            case "Options":
                options.AddEventProcessor(new ApplicationEventProcessor(filter));
                break;
            case "Provider":
                options.AddEventProcessorProvider(() => [new ApplicationEventProcessor(filter)]);
                break;
            case "Scope":
                scope.AddEventProcessor(filter);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(registration));
        }
    }

    private sealed class ApplicationEventProcessor(Func<SentryEvent, SentryEvent> filter) : ISentryEventProcessor
    {
        public SentryEvent Process(SentryEvent @event) => filter(@event);
    }

    private sealed class ThrowingMessageParameter(Exception exception)
    {
        public override string ToString() => throw exception;
    }
}
