#if __IOS__
using Foundation;
using Sentry.Cocoa;
using Sentry.Cocoa.Extensions;

namespace Sentry.Tests.Platforms.iOS;

public class SentrySdkTests
{
    private readonly IDiagnosticLogger _logger;

    public SentrySdkTests(ITestOutputHelper testOutputHelper)
    {
        _logger = Substitute.ForPartsOf<TestOutputDiagnosticLogger>(testOutputHelper);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ApplyDataCollection_NotConfigured_ForwardsSendDefaultPii(bool sendDefaultPii)
    {
        var options = new SentryOptions { SendDefaultPii = sendDefaultPii };
        var nativeOptions = new CocoaSdk.SentryObjCOptions();

        SentrySdk.ApplyDataCollection(options, nativeOptions);

        nativeOptions.SendDefaultPii.Should().Be(sendDefaultPii);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void ApplyDataCollection_Configured_ForwardsUserInfoAsSendDefaultPii(bool userInfo, bool sendDefaultPii)
    {
        var options = new SentryOptions { SendDefaultPii = sendDefaultPii };
        options.ConfigureDataCollection(dataCollection => dataCollection.UserInfo = userInfo);
        var nativeOptions = new CocoaSdk.SentryObjCOptions();

        SentrySdk.ApplyDataCollection(options, nativeOptions);

        nativeOptions.SendDefaultPii.Should().Be(userInfo);
    }

    [Fact]
    public void ApplyDataCollection_Configured_LeavesSessionReplayUntouched()
    {
        var options = new SentryOptions();
        options.ConfigureDataCollection(dataCollection =>
        {
            dataCollection.UserInfo = false;
            dataCollection.Cookies = KeyValueCollectionBehavior.Off;
            dataCollection.UrlQueryParams = KeyValueCollectionBehavior.Off;
            dataCollection.HttpHeaders.Request = KeyValueCollectionBehavior.Off;
            dataCollection.HttpHeaders.Response = KeyValueCollectionBehavior.Off;
            dataCollection.HttpBodies = HttpBodyTypes.None;
            dataCollection.GraphQL.Document = false;
            dataCollection.GraphQL.Variables = false;
        });
        var nativeOptions = new CocoaSdk.SentryObjCOptions
        {
            SessionReplay = new CocoaSdk.SentryObjCReplayOptions
            {
                SessionSampleRate = 1,
                OnErrorSampleRate = 1,
                MaskAllText = false,
                MaskAllImages = false,
                NetworkCaptureBodies = true,
                NetworkRequestHeaders = ["Authorization"],
                NetworkResponseHeaders = ["Set-Cookie"]
            }
        };
        var expected = ReplaySnapshot(nativeOptions.SessionReplay);

        SentrySdk.ApplyDataCollection(options, nativeOptions);

        ReplaySnapshot(nativeOptions.SessionReplay).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ProcessOnBeforeSend_NativeErrorSuppression(bool suppressNativeErrors)
    {
        // Arrange
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            IsGlobalModeEnabled = true,
            Debug = true,
            AutoSessionTracking = false,
            BackgroundWorker = Substitute.For<IBackgroundWorker>(),
            InitNativeSdks = false,
        };
        options.Native.SuppressExcBadAccess = suppressNativeErrors;

        var called = false;
        options.SetBeforeSend(e =>
        {
            called = true;
            return e;
        });

        var scope = new Scope(options);
        var hub = Substitute.For<IHub>();
        hub.When(h => hub.ConfigureScope(Arg.Any<Action<Scope>>()))
            .Do(callback => callback.Arg<Action<Scope>>().Invoke(scope));

        var evt = new Sentry.CocoaSdk.SentryObjCEvent();
        var ex = new Sentry.CocoaSdk.SentryObjCException("Not checked", "EXC_BAD_ACCESS");
        evt.Exceptions = [ex];

        // Act
        var result = SentrySdk.ProcessOnBeforeSend(options, evt, hub);

        // Assert
        if (suppressNativeErrors)
        {
            called.Should().BeFalse();
            result.Should().BeNull();
        }
        else
        {
            called.Should().BeTrue();
            result.Exceptions.First().Type.Should().Be("EXC_BAD_ACCESS");
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ProcessOnBeforeSend_NativeErrorSuppressionBeforeHubInit(bool suppressNativeErrors)
    {
        // Arrange
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            IsGlobalModeEnabled = true,
            Debug = true,
            AutoSessionTracking = false,
            BackgroundWorker = Substitute.For<IBackgroundWorker>(),
            InitNativeSdks = false,
        };
        options.Native.SuppressExcBadAccess = suppressNativeErrors;

        var scope = new Scope(options);
        // `SIGABRT` must be filtered out early on startup during
        // the Cocoa SDK init before the Hub instance has been created
        var hub = DisabledHub.Instance;

        var evt = new Sentry.CocoaSdk.SentryObjCEvent();
        var ex = new Sentry.CocoaSdk.SentryObjCException("Not checked", "EXC_BAD_ACCESS");
        evt.Exceptions = [ex];

        // Act
        var result = SentrySdk.ProcessOnBeforeSend(options, evt, hub);

        // Assert
        if (suppressNativeErrors)
        {
            result.Should().BeNull();
        }
        else
        {
            result.Exceptions.First().Type.Should().Be("EXC_BAD_ACCESS");
        }
    }

    [Fact]
    public void ProcessOnBeforeSend_OptionsBeforeOnSendRuns()
    {
        // Arrange
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            IsGlobalModeEnabled = true,
            Debug = true,
            AutoSessionTracking = false,
            BackgroundWorker = Substitute.For<IBackgroundWorker>(),
            InitNativeSdks = false
        };

        var native = new Sentry.CocoaSdk.SentryObjCEvent();
        native.ServerName = "server name";
        native.Dist = "dist";
        native.Logger = "logger";
        native.ReleaseName = "release name";
        native.Environment = "environment";
        native.Transaction = "transaction name";
        options.SetBeforeSend(e =>
        {
            e.TransactionName = "dotnet";
            return e;
        });

        var scope = new Scope(options);
        var hub = Substitute.For<IHub>();
        hub.When(h => hub.ConfigureScope(Arg.Any<Action<Scope>>()))
            .Do(callback => callback.Arg<Action<Scope>>().Invoke(scope));

        // Act
        var result = SentrySdk.ProcessOnBeforeSend(options, native, hub);

        // Assert
        result.Should().NotBeNull();
        result.Transaction.Should().Be("dotnet");
    }

    [Fact]
    public void ProcessOnBeforeSend_EventProcessorsInvoked()
    {
        // Arrange
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            IsGlobalModeEnabled = true,
            Debug = true,
            AutoSessionTracking = false,
            BackgroundWorker = Substitute.For<IBackgroundWorker>(),
            InitNativeSdks = false,
        };
        var eventProcessor = new TestEventProcessor();
        options.AddEventProcessor(eventProcessor);

        var scope = new Scope(options);
        var hub = Substitute.For<IHub>();
        hub.When(h => hub.ConfigureScope(Arg.Any<Action<Scope>>()))
            .Do(callback => callback.Arg<Action<Scope>>().Invoke(scope));

        var native = new Sentry.CocoaSdk.SentryObjCEvent();

        // Act
        SentrySdk.ProcessOnBeforeSend(options, native, hub);

        // Assert
        eventProcessor.Invoked.Should().BeTrue();
    }

    [Fact]
    public void ProcessOnBeforeSend_BeforeSendThrows_DropsNativeEvent()
    {
        // Arrange
        var exception = new InvalidOperationException("callback failed");
        var logger = new InMemoryDiagnosticLogger();
        var options = new SentryOptions { Debug = true, DiagnosticLogger = logger };
        options.SetBeforeSend((SentryEvent _, SentryHint _) => throw exception);

        var native = new Sentry.CocoaSdk.SentryObjCEvent();

        // Act
        var result = SentrySdk.ProcessOnBeforeSend(options, native, Substitute.For<IHub>());

        // Assert
        result.Should().BeNull();
        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == SentryLevel.Error &&
            entry.Exception == exception &&
            entry.Message == "The BeforeSend callback threw an exception. The event will be dropped.");
    }

    [Fact]
    public void ProcessOnBeforeSend_BeforeSendReturnsEvent_KeepsNativeEvent()
    {
        // Arrange
        var options = new SentryOptions();
        options.SetBeforeSend((SentryEvent @event, SentryHint _) => @event);

        var native = new Sentry.CocoaSdk.SentryObjCEvent();

        // Act
        var result = SentrySdk.ProcessOnBeforeSend(options, native, Substitute.For<IHub>());

        // Assert
        result.Should().BeSameAs(native);
    }

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
    public void ProcessOnBeforeSend_CopyBackFails_DropsEvent()
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

    private static object ReplaySnapshot(CocoaSdk.SentryObjCReplayOptions replay) => new
    {
        replay.SessionSampleRate,
        replay.OnErrorSampleRate,
        replay.MaskAllText,
        replay.MaskAllImages,
        replay.NetworkCaptureBodies,
        NetworkRequestHeaders = replay.NetworkRequestHeaders.ToList(),
        NetworkResponseHeaders = replay.NetworkResponseHeaders.ToList()
    };

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

file class TestEventProcessor : ISentryEventProcessor
{
    public bool Invoked { get; private set; }
    public SentryEvent Process(SentryEvent @event)
    {
        Invoked = true;
        return @event;
    }
}
#endif
