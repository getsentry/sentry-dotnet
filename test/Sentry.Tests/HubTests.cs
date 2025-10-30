using System.IO.Abstractions.TestingHelpers;
using Sentry.Internal.Http;
using Sentry.Protocol;
using Sentry.Tests.Internals;

namespace Sentry.Tests;

public partial class HubTests : IDisposable
{
    private readonly ITestOutputHelper _output;

    private class Fixture : IDisposable
    {
        public SentryOptions Options { get; }
        public ISentryClient Client { get; set; }
        public ISessionManager SessionManager { get; set; }
        public IInternalScopeManager ScopeManager { get; set; }
        public ISystemClock Clock { get; set; }
        public IReplaySession ReplaySession { get; }
        public ISampleRandHelper SampleRandHelper { get; set; }
        public BackpressureMonitor BackpressureMonitor { get; set; }

        public Fixture()
        {
            Options = new SentryOptions
            {
                Dsn = ValidDsn,
                TracesSampleRate = 1.0,
                AutoSessionTracking = false
            };
            Client = Substitute.For<ISentryClient>();
            ReplaySession = Substitute.For<IReplaySession>();
        }

        public void Dispose()
        {
            BackpressureMonitor?.Dispose();
        }

        public Hub GetSut() => new(Options, Client, SessionManager, Clock, ScopeManager, replaySession: ReplaySession,
            sampleRandHelper: SampleRandHelper, backpressureMonitor: BackpressureMonitor);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    private readonly Fixture _fixture = new();

    public HubTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void PushScope_BreadcrumbWithinScope_NotVisibleOutside()
    {
        // Arrange
        _fixture.Options.IsGlobalModeEnabled = false;
        var hub = _fixture.GetSut();

        // Act & assert
        using (hub.PushScope())
        {
            hub.ConfigureScope(s => s.AddBreadcrumb("test"));
            Assert.Single(hub.ScopeManager.GetCurrent().Key.Breadcrumbs);
        }

        Assert.Empty(hub.ScopeManager.GetCurrent().Key.Breadcrumbs);
    }

    [Fact]
    public void PushAndLockScope_DoesNotAffectOuterScope()
    {
        // Arrange
        var hub = _fixture.GetSut();

        // Act & Assert
        hub.ScopeManager.ConfigureScope(s => Assert.False(s.Locked));
        using (hub.PushAndLockScope())
        {
            hub.ScopeManager.ConfigureScope(s => Assert.True(s.Locked));
        }

        if (_fixture.Options.IsGlobalModeEnabled)
        {
            hub.ScopeManager.ConfigureScope(s => Assert.True(s.Locked));
        }
        else
        {
            hub.ScopeManager.ConfigureScope(s => Assert.False(s.Locked));
        }
    }

    [Fact]
    public void CaptureMessage_FailedQueue_LastEventIdSetToEmpty()
    {
        // Arrange
        var hub = _fixture.GetSut();

        // Act
        var actualId = hub.CaptureMessage("test");

        // Assert
        Assert.Equal(Guid.Empty, (Guid)actualId);
        Assert.Equal(Guid.Empty, (Guid)hub.LastEventId);
    }

    [Fact]
    public void CaptureMessage_SuccessQueued_LastEventIdSetToReturnedId()
    {
        // Arrange
        var worker = Substitute.For<IBackgroundWorker>();
        worker.EnqueueEnvelope(Arg.Any<Envelope>()).Returns(true);

        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            BackgroundWorker = worker
        });

        // Act
        var actualId = hub.CaptureMessage("test");

        // Assert
        Assert.NotEqual(default, actualId);
        Assert.Equal(actualId, hub.LastEventId);
    }

    [Fact]
    public void CaptureException_FinishedSpanBoundToSameExceptionExists_EventIsLinkedToSpan()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 1.0;
        var hub = _fixture.GetSut();
        var exception = new Exception("error");

        var transaction = hub.StartTransaction("foo", "bar");
        transaction.Finish(exception);

        // Act
        hub.CaptureException(exception);

        // Assert
        _fixture.Client.Received(1).CaptureEvent(
            Arg.Is<SentryEvent>(evt =>
                evt.Contexts.Trace.TraceId == transaction.TraceId &&
                evt.Contexts.Trace.SpanId == transaction.SpanId),
            Arg.Any<Scope>(), Arg.Any<SentryHint>());
    }

    [Fact]
    public void CaptureException_ActiveSpanExistsOnScope_EventIsLinkedToSpan()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 1.0;
        var hub = _fixture.GetSut();
        var exception = new Exception("error");

        var transaction = hub.StartTransaction("foo", "bar");
        hub.ConfigureScope(scope => scope.Transaction = transaction);

        // Act
        hub.CaptureException(exception);

        // Assert
        _fixture.Client.Received(1).CaptureEvent(
            Arg.Is<SentryEvent>(evt =>
                evt.Contexts.Trace.TraceId == transaction.TraceId &&
                evt.Contexts.Trace.SpanId == transaction.SpanId),
            Arg.Any<Scope>(), Arg.Any<SentryHint>());
    }

    [Fact]
    public void CaptureException_TransactionFinished_Gets_DSC_From_LinkedSpan()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 1.0;
        var hub = _fixture.GetSut();
        var exception = new Exception("error");

        var traceHeader = new SentryTraceHeader(
            SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"),
            SpanId.Parse("2000000000000000"),
            true);
        var transactionContext = new TransactionContext("foo", "bar", traceHeader);

        var dsc = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-sample_rate", "1.0"},
            {"sentry-trace_id", "75302ac48a024bde9a3b3734a82e36c8"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-replay_id", "bfd31b89a59d41c99d96dc2baf840ecd"}
        }).CreateDynamicSamplingContext(_fixture.ReplaySession);

        var transaction = hub.StartTransaction(
            transactionContext,
            new Dictionary<string, object>(),
            dsc);
        transaction.Finish(exception);

        // Act
        hub.CaptureException(exception);

        // Assert
        _fixture.Client.Received(1).CaptureEvent(
            Arg.Is<SentryEvent>(evt =>
                evt.DynamicSamplingContext == dsc),
            Arg.Any<Scope>(), Arg.Any<SentryHint>());
    }

    [Fact]
    public void CaptureException_ActiveSpanExistsOnScopeButIsSampledOut_EventIsLinkedToSpan()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 0.0;
        var hub = _fixture.GetSut();
        var exception = new Exception("error");

        var transaction = hub.StartTransaction("foo", "bar");
        hub.ConfigureScope(scope => scope.Transaction = transaction);

        // Act
        hub.CaptureException(exception);

        // Assert
        _fixture.Client.Received(1).CaptureEvent(
            Arg.Is<SentryEvent>(evt =>
                evt.Contexts.Trace.TraceId == transaction.TraceId &&
                evt.Contexts.Trace.SpanId == transaction.SpanId),
            Arg.Any<Scope>(), Arg.Any<SentryHint>());
    }

    [Fact]
    public void CaptureException_NoActiveSpanAndNoSpanBoundToSameException_EventContainsPropagationContext()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 1.0;
        var hub = _fixture.GetSut();
        var scope = hub.ScopeManager.GetCurrent().Key;

        // Act
        hub.CaptureException(new Exception("error"));

        // Assert
        _fixture.Client.Received(1).CaptureEvent(
            Arg.Is<SentryEvent>(evt =>
                evt.Contexts.Trace.TraceId == scope.PropagationContext.TraceId &&
                evt.Contexts.Trace.SpanId == scope.PropagationContext.SpanId),
            Arg.Any<Scope>(), Arg.Any<SentryHint>());
    }

    [Fact]
    public void CaptureEvent_SessionActive_NoExceptionDoesNotReportError()
    {
        // Arrange
        _fixture.Options.Release = "release";
        var hub = _fixture.GetSut();
        hub.StartSession();

        // Act
        hub.CaptureEvent(new SentryEvent());
        hub.EndSession();

        // Assert
        _fixture.Client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => s.ErrorCount == 0));
    }

    [Fact]
    public void CaptureEvent_ExceptionWithOpenSpan_SpanLinkedToEventContext()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 1.0;
        var hub = _fixture.GetSut();

        var scope = new Scope();
        var evt = new SentryEvent(new Exception());
        scope.Transaction = hub.StartTransaction("transaction", "operation");
        var child = scope.Transaction.StartChild("child", "child");

        // Act
        hub.CaptureEvent(evt, scope);

        // Assert
        Assert.Equal(child.SpanId, evt.Contexts.Trace.SpanId);
        Assert.Equal(child.TraceId, evt.Contexts.Trace.TraceId);
        Assert.Equal(child.ParentSpanId, evt.Contexts.Trace.ParentSpanId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CaptureEvent_Exception_LeavesBreadcrumb(bool withScopeCallback)
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 1.0;
        using var hub = _fixture.GetSut();
        var evt = new SentryEvent(new Exception());
        var scope = hub.ScopeManager.GetCurrent().Key;

        // Act
        _ = withScopeCallback
            ? hub.CaptureEvent(evt, s => s.ClearBreadcrumbs())
            : hub.CaptureEvent(evt);

        // Assert
        scope.Breadcrumbs.Should().NotBeEmpty();
        using var assertionScope = new AssertionScope();
        var breadcrumb = scope.Breadcrumbs.Last();
        breadcrumb.Message.Should().Be(evt.Exception!.Message);
        breadcrumb.Level.Should().Be(BreadcrumbLevel.Fatal);
        breadcrumb.Category.Should().Be("Exception");
    }

    [Fact]
    public void CaptureEvent_WithMessageAndException_StoresExceptionMessageAsData()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 1.0;
        var hub = _fixture.GetSut();
        var evt = new SentryEvent(new Exception())
        {
            Message = new SentryMessage
            {
                Formatted = "formatted",
                Message = "message"
            }
        };
        var scope = hub.ScopeManager.GetCurrent().Key;

        // Act
        hub.CaptureEvent(evt);

        // Assert
        scope.Breadcrumbs.Should().NotBeEmpty();
        using var assertionScope = new AssertionScope();
        var breadcrumb = scope.Breadcrumbs.Last();
        breadcrumb.Message.Should().Be(evt.Message.Formatted);
        breadcrumb.Data.Should().BeEquivalentTo(
            new Dictionary<string, string>
            {
                ["exception_message"] = evt.Exception!.Message
            });
        breadcrumb.Level.Should().Be(BreadcrumbLevel.Fatal);
        breadcrumb.Category.Should().Be("Exception");
    }

    internal class EvilContext
    {
        // This property will throw an exception during serialization.
        // ReSharper disable once UnusedMember.Local
        public string Thrower => throw new InvalidDataException();
    }

#if !__MOBILE__
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CaptureEvent_NonSerializableContextAndOfflineCaching_CapturesEventWithContextKey(bool offlineCaching)
    {
        // This test has proven to be flaky, so we'll skip it on mobile targets.
        // We'll also retry it a few times when we run it for non-mobile targets.
        // As long as it doesn't consistently fail, that should be good enough.
        // TODO: The retry and/or #if can be removed if we can confidently figure out the source of the flakiness.
        await TestHelpers.RetryTestAsync(
            maxAttempts: 3,
            _output,
            () => CapturesEventWithContextKey_Implementation(offlineCaching));
    }

    private async Task CapturesEventWithContextKey_Implementation(bool offlineCaching)
    {
#if NET6_0_OR_GREATER
        JsonExtensions.AddJsonSerializerContext(o => new HubTestsJsonContext(o));
#endif
        var tcs = new TaskCompletionSource<bool>();
        var expectedMessage = Guid.NewGuid().ToString();

        var requests = new List<string>();
        async Task Verify(HttpRequestMessage message)
        {
            var payload = await message.Content!.ReadAsStringAsync();
            requests.Add(payload);
            if (payload.Contains(expectedMessage))
            {
                tcs.TrySetResult(true);
            }
        }

        var cts = new CancellationTokenSource();
        cts.Token.Register(() => tcs.TrySetCanceled());

        using var tempDirectory = offlineCaching ? new TempDirectory() : null;

        var logger = Substitute.ForPartsOf<TestOutputDiagnosticLogger>(_output);

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            // To go through a round trip serialization of cached envelope
            CacheDirectoryPath = tempDirectory?.Path,
            // So we don't need to deal with gzip payloads
            RequestBodyCompressionLevel = CompressionLevel.NoCompression,
            CreateHttpMessageHandler = () => new CallbackHttpClientHandler(Verify),
            // Not to send some session envelope
            AutoSessionTracking = false,
            Debug = true,
            DiagnosticLogger = logger,
            // This keeps all writing-to-file operations in memory instead of actually writing to disk
            FileSystem = new FakeFileSystem()
        };

        // Disable process exit flush to resolve "There is no currently active test." errors.
        options.DisableAppDomainProcessExitFlush();

        try
        {
            using var hub = new Hub(options);

            var expectedContextKey = Guid.NewGuid().ToString();
            var evt = new SentryEvent
            {
                Contexts = { [expectedContextKey] = new EvilContext() },
                Message = new()
                {
                    Formatted = expectedMessage
                }
            };

            hub.CaptureEvent(evt);
            await hub.FlushAsync();

            // Synchronizing in the tests to go through the caching and http transports

            cts.CancelAfter(TimeSpan.FromSeconds(3));
            var ex = await Record.ExceptionAsync(() => tcs.Task);
            Assert.False(ex is OperationCanceledException || !tcs.Task.Result, "Event not captured");
            Assert.Null(ex);

            Assert.True(requests.All(p => p.Contains(expectedContextKey)),
                "Un-serializable context key should exist");

            logger.Received().Log(SentryLevel.Error,
                "Failed to serialize object for property '{0}'. Original depth: {1}, current depth: {2}",
                Arg.Any<InvalidDataException>(),
                Arg.Any<object[]>());
        }
        finally
        {
            // ensure the task is complete before leaving the test
            tcs.TrySetResult(false);
            await tcs.Task;

            if (options.Transport is CachingTransport cachingTransport)
            {
                // Disposing the caching transport will ensure its worker
                // is shut down before we try to dispose and delete the temp folder
                await cachingTransport.DisposeAsync();
            }
        }
    }
#endif

    [Fact]
    public void CaptureEvent_ActiveSession_UnhandledExceptionSessionEndedAsCrashed()
    {
        // Arrange
        var worker = Substitute.For<IBackgroundWorker>();

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            Release = "release"
        };
        var sessionManager = new GlobalSessionManager(options);
        var client = new SentryClient(options, worker, sessionManager: sessionManager);
        var hub = new Hub(options, client, sessionManager);

        hub.StartSession();

        // Act
        hub.CaptureEvent(new()
        {
            SentryExceptions = new[]
            {
                new SentryException
                {
                    Mechanism = new()
                    {
                        Handled = false
                    }
                }
            }
        });

        // Assert
        worker.Received().EnqueueEnvelope(
            Arg.Is<Envelope>(e =>
                e.Items
                    .Select(i => i.Payload)
                    .OfType<JsonSerializable>()
                    .Select(i => i.Source)
                    .OfType<SessionUpdate>()
                    .Single()
                    .EndStatus == SessionEndStatus.Crashed
            ));
    }

    [Fact]
    public void CaptureEvent_Client_GetsHint()
    {
        // Arrange
        var @event = new SentryEvent();
        var hint = new SentryHint();
        var hub = _fixture.GetSut();

        // Act
        hub.CaptureEvent(@event, hint: hint);

        // Assert
        _fixture.Client.Received(1).CaptureEvent(
            Arg.Any<SentryEvent>(),
            Arg.Any<Scope>(), Arg.Is<SentryHint>(h => h == hint));
    }

    [Fact]
    public void CaptureEvent_TerminalUnhandledException_AbortsActiveTransaction()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 1.0;
        var hub = _fixture.GetSut();

        var transaction = hub.StartTransaction("test", "operation");
        hub.ConfigureScope(scope => scope.Transaction = transaction);

        var exception = new Exception("test");
        exception.SetSentryMechanism("test", handled: false, terminal: true);

        // Act
        hub.CaptureEvent(new SentryEvent(exception));

        // Assert
        transaction.Status.Should().Be(SpanStatus.Aborted);
        transaction.IsFinished.Should().BeTrue();
    }

    [Fact]
    public void CaptureEvent_NonTerminalUnhandledException_DoesNotAbortActiveTransaction()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 1.0;
        var hub = _fixture.GetSut();

        var transaction = hub.StartTransaction("test", "operation");
        hub.ConfigureScope(scope => scope.Transaction = transaction);

        var exception = new Exception("test");
        exception.SetSentryMechanism("TestException", handled: false, terminal: false);

        // Act
        hub.CaptureEvent(new SentryEvent(exception));

        // Assert
        transaction.IsFinished.Should().BeFalse();
    }

    [Fact]
    public void CaptureEvent_HandledException_DoesNotAbortActiveTransaction()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 1.0;
        var hub = _fixture.GetSut();

        var transaction = hub.StartTransaction("test", "operation");
        hub.ConfigureScope(scope => scope.Transaction = transaction);

        var exception = new Exception("test");
        exception.SetSentryMechanism("test", handled: true);

        // Act
        hub.CaptureEvent(new SentryEvent(exception));

        // Assert
        transaction.IsFinished.Should().BeFalse();
    }

    [Fact]
    public void CaptureEvent_EventWithoutException_DoesNotAbortActiveTransaction()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 1.0;
        var hub = _fixture.GetSut();

        var transaction = hub.StartTransaction("test", "operation");
        hub.ConfigureScope(scope => scope.Transaction = transaction);

        // Act
        hub.CaptureEvent(new SentryEvent { Message = "test message" });

        // Assert
        transaction.IsFinished.Should().BeFalse();
    }

    [Fact]
    public void AppDomainUnhandledExceptionIntegration_ActiveSession_UnhandledExceptionSessionEndedAsCrashed()
    {
        // Arrange
        var worker = Substitute.For<IBackgroundWorker>();

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            Release = "release"
        };
        var sessionManager = new GlobalSessionManager(options);
        var client = new SentryClient(options, worker, sessionManager: sessionManager);
        var hub = new Hub(options, client, sessionManager);

        var integration = new AppDomainUnhandledExceptionIntegration(Substitute.For<IAppDomain>());
        integration.Register(hub, options);

        hub.StartSession();

        // Act
        // Simulate a terminating exception
        integration.Handle(this, new UnhandledExceptionEventArgs(new Exception("test"), true));

        // Assert
        worker.Received().EnqueueEnvelope(
            Arg.Is<Envelope>(e =>
                e.Items
                    .Select(i => i.Payload)
                    .OfType<JsonSerializable>()
                    .Select(i => i.Source)
                    .OfType<SessionUpdate>()
                    .Single()
                    .EndStatus == SessionEndStatus.Crashed
            ));
    }

    [Fact]
    public void StartTransaction_NameOpDescription_Works()
    {
        // Arrange
        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction("name", "operation", "description");

        // Assert
        transaction.Name.Should().Be("name");
        transaction.Operation.Should().Be("operation");
        transaction.Description.Should().Be("description");
    }

    [Fact]
    public void StartTransaction_FromTraceHeader_CopiesContext()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 1.0;
        var hub = _fixture.GetSut();

        var traceHeader = new SentryTraceHeader(
            SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"),
            SpanId.Parse("2000000000000000"),
            true);

        // Act
        var transaction = hub.StartTransaction("name", "operation", traceHeader);

        // Assert
        transaction.TraceId.Should().Be(SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"));
        transaction.ParentSpanId.Should().Be(SpanId.Parse("2000000000000000"));
        transaction.IsSampled.Should().BeTrue();
    }

    [Fact]
    public void StartTransaction_FromTraceHeader_SampledInheritedFromParentRegardlessOfSampleRate()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 0.0;
        var hub = _fixture.GetSut();

        var traceHeader = new SentryTraceHeader(
            SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"),
            SpanId.Parse("2000000000000000"),
            true);

        // Act
        var transaction = hub.StartTransaction("name", "operation", traceHeader);

        // Assert
        transaction.IsSampled.Should().BeTrue();
    }

    [Fact]
    public void StartTransaction_FromTraceHeader_CustomSamplerCanSampleOutTransaction()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 1.0;
        _fixture.Options.TracesSampler = _ => 0.0;
        var hub = _fixture.GetSut();

        var traceHeader = new SentryTraceHeader(
            SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"),
            SpanId.Parse("2000000000000000"),
            true);

        // Act
        var transaction = hub.StartTransaction("foo", "bar", traceHeader);

        // Assert
        transaction.IsSampled.Should().BeFalse();
    }

    [Fact]
    public void StartTransaction_StaticSampling_SampledIn()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 1.0;
        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction("name", "operation");

        // Assert
        transaction.IsSampled.Should().BeTrue();
    }

    [Fact]
    public void StartTransaction_StaticSampling_SampledOut()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 0.0;
        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction("name", "operation");

        // Assert
        transaction.IsSampled.Should().BeFalse();
    }

    [Fact]
    public void StartTransaction_EnableTracing_SampledIn()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 1.0;
        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction("name", "operation");

        // Assert
        transaction.IsSampled.Should().BeTrue();
    }

    [Fact]
    public void StartTransaction_SameInstrumenter_SampledIn()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 1.0;
        _fixture.Options.Instrumenter = Instrumenter.Sentry; // The default... making it explicit for this test though
        var hub = _fixture.GetSut();

        var transactionContext = new TransactionContext("name", "operation")
        {
            Instrumenter = _fixture.Options.Instrumenter
        };

        // Act
        var transaction = hub.StartTransaction(transactionContext);

        // Assert
        transaction.IsSampled.Should().BeTrue();
    }

    [Fact]
    public void StartTransaction_DynamicSamplingContextWithSampleRate_UsesSampleRate()
    {
        // Arrange
        var transactionContext = new TransactionContext("name", "operation");
        var dsc = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sample_rate", "0.5"},
            {"sentry-sample_rand", "0.1234"},
        }).CreateDynamicSamplingContext();

        _fixture.Options.TracesSampler = _ => 0.5;
        _fixture.Options.TracesSampleRate = 0.5;

        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction(transactionContext, new Dictionary<string, object>(), dsc);

        // Assert
        var transactionTracer = transaction.Should().BeOfType<TransactionTracer>().Subject;
        transactionTracer.SampleRate.Should().Be(0.5);
        transactionTracer.DynamicSamplingContext.Should().BeSameAs(dsc);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StartTransaction_Backpressure_Downsamples(bool usesTracesSampler)
    {
        // Arrange
        var transactionContext = new TransactionContext("name", "operation");

        var clock = new MockClock(DateTimeOffset.UtcNow);
        _fixture.Options.EnableBackpressureHandling = true;
        _fixture.BackpressureMonitor = new BackpressureMonitor(null, clock, enablePeriodicHealthCheck: false);
        _fixture.BackpressureMonitor.SetDownsampleLevel(1);
        var sampleRate = 0.5f;
        var expectedDownsampledRate = sampleRate * _fixture.BackpressureMonitor.DownsampleFactor;
        if (usesTracesSampler)
        {
            _fixture.Options.TracesSampler = _ => sampleRate;
        }
        else
        {
            _fixture.Options.TracesSampleRate = sampleRate;
        }

        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction(transactionContext, new Dictionary<string, object>());

        switch (transaction)
        {
            // Assert
            case TransactionTracer tracer:
                tracer.SampleRate.Should().Be(expectedDownsampledRate);
                break;
            case UnsampledTransaction unsampledTransaction:
                unsampledTransaction.SampleRate.Should().Be(expectedDownsampledRate);
                break;
            default:
                throw new Exception("Unexpected transaction type.");
        }
    }

    [Theory]
    [InlineData(true, 0.4f, "backpressure")]
    [InlineData(true, 0.6f, "sample_rate")]
    [InlineData(false, 0.4f, "backpressure")]
    [InlineData(false, 0.6f, "sample_rate")]
    public void StartTransaction_Backpressure_SetsDiscardReason(bool usesTracesSampler, double sampleRand, string discardReason)
    {
        // Arrange
        var transactionContext = new TransactionContext("name", "operation");

        var clock = new MockClock(DateTimeOffset.UtcNow);
        _fixture.SampleRandHelper = Substitute.For<ISampleRandHelper>();
        _fixture.SampleRandHelper.GenerateSampleRand(Arg.Any<string>()).Returns(sampleRand);
        _fixture.Options.EnableBackpressureHandling = true;
        _fixture.BackpressureMonitor = new BackpressureMonitor(null, clock, enablePeriodicHealthCheck: false);
        _fixture.BackpressureMonitor.SetDownsampleLevel(1);
        var sampleRate = 0.5f;
        if (usesTracesSampler)
        {
            _fixture.Options.TracesSampler = _ => sampleRate;
        }
        else
        {
            _fixture.Options.TracesSampleRate = sampleRate;
        }

        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction(transactionContext, new Dictionary<string, object>());
        transaction.Should().BeOfType<UnsampledTransaction>();
        var unsampledTransaction = (UnsampledTransaction)transaction;
        var expectedReason = new DiscardReason(discardReason);
        unsampledTransaction.DiscardReason.Should().Be(expectedReason);
    }

    // overwrite the 'sample_rate' of the Dynamic Sampling Context (DSC) when a sampling decisions is made in the downstream SDK
    // 1. overwrite when 'TracesSampler' reaches a sampling decision
    // 2. keep when a sampling decision has been made upstream (via 'TransactionContext.IsSampled')
    // 3. overwrite when 'TracesSampleRate' reaches a sampling decision
    // 4. keep otherwise
    [SkippableTheory]
    [InlineData(null, 0.3, 0.4, true, 0.3, true)]
    [InlineData(null, 0.3, null, true, 0.3, true)]
    [InlineData(null, null, 0.4, true, 0.4, true)]
    [InlineData(null, null, null, false, 0.0, false)]
    [InlineData(true, 0.3, 0.4, true, 0.3, true)]
    [InlineData(true, 0.3, null, true, 0.3, true)]
    [InlineData(true, null, 0.4, true, 0.4, false)]
    [InlineData(true, null, null, true, 0.0, false)]
    [InlineData(false, 0.3, 0.4, true, 0.3, true)]
    [InlineData(false, 0.3, null, true, 0.3, true)]
    [InlineData(false, null, 0.4, false, 0.4, false)]
    [InlineData(false, null, null, false, 0.0, false)]
    public void StartTransaction_DynamicSamplingContextWithSampleRate_OverwritesSampleRate(bool? isSampled, double? tracesSampler, double? tracesSampleRate, bool expectedIsSampled, double expectedSampleRate, bool expectedDscOverwritten)
    {
        // Arrange
        var transactionContext = new TransactionContext("name", "operation", isSampled: isSampled);
        var dsc = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sample_rate", "0.5"},
            {"sentry-sample_rand", "0.1234"},
        }).CreateDynamicSamplingContext();
        var originalDsc = dsc?.Clone();

        _fixture.Options.TracesSampler = _ => tracesSampler;
        _fixture.Options.TracesSampleRate = tracesSampleRate;

        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction(transactionContext, new Dictionary<string, object>(), dsc);

        // Assert
        if (expectedIsSampled)
        {
            var transactionTracer = transaction.Should().BeOfType<TransactionTracer>().Subject;
            transactionTracer.SampleRate.Should().Be(expectedSampleRate);
            if (expectedDscOverwritten)
            {
                transactionTracer.DynamicSamplingContext.Should().BeEquivalentTo(originalDsc.CloneWithSampleRate(expectedSampleRate));
            }
            else
            {
                transactionTracer.DynamicSamplingContext.Should().BeEquivalentTo(originalDsc);
            }
        }
        else
        {
            var unsampledTransaction = transaction.Should().BeOfType<UnsampledTransaction>().Subject;
            unsampledTransaction.SampleRate.Should().Be(expectedSampleRate);
            if (expectedDscOverwritten)
            {
                unsampledTransaction.DynamicSamplingContext.Should().BeEquivalentTo(originalDsc.CloneWithSampleRate(expectedSampleRate));
            }
            else
            {
                unsampledTransaction.DynamicSamplingContext.Should().BeEquivalentTo(originalDsc);
            }
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StartTransaction_DynamicSamplingContextWithReplayId_UsesActiveReplaySessionId(bool replaySessionIsActive)
    {
        // Arrange
        var transactionContext = new TransactionContext("name", "operation");

        var dummyReplaySession = Substitute.For<IReplaySession>();
        dummyReplaySession.ActiveReplayId.Returns((SentryId?)null); // So the replay id in the baggage header is used
        var dsc = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sampled", "true"},
            {"sentry-sample_rate", "0.5"}, // Required in the baggage header, but ignored by sampling logic
            {"sentry-replay_id", "bfd31b89a59d41c99d96dc2baf840ecd"}
        }).CreateDynamicSamplingContext(dummyReplaySession);

        _fixture.Options.TracesSampleRate = 1.0;
        _fixture.ReplaySession.ActiveReplayId.Returns(replaySessionIsActive ? SentryId.Create() : null); // This one gets used by the SUT
        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction(transactionContext, new Dictionary<string, object>(), dsc);

        // Assert
        var transactionTracer = transaction.Should().BeOfType<TransactionTracer>().Subject;
        transactionTracer.IsSampled.Should().BeTrue();
        transactionTracer.DynamicSamplingContext.Should().NotBeNull();

        var expectedDsc = dsc.CloneWithSampleRate(_fixture.Options.TracesSampleRate.Value);
        if (replaySessionIsActive)
        {
            // We overwrite the replay_id when we have an active replay session
            // Otherwise we propagate whatever was in the baggage header
            expectedDsc = expectedDsc.CloneWithReplayId(_fixture.ReplaySession);
        }
        transactionTracer.DynamicSamplingContext.Should().BeEquivalentTo(expectedDsc);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StartTransaction_NoDynamicSamplingContext_UsesActiveReplaySessionId(bool replaySessionIsActive)
    {
        // Arrange
        var transactionContext = new TransactionContext("name", "operation");
        _fixture.ReplaySession.ActiveReplayId.Returns(replaySessionIsActive ? SentryId.Create() : null);
        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction(transactionContext, new Dictionary<string, object>());

        // Assert
        var transactionTracer = transaction.Should().BeOfType<TransactionTracer>().Subject;
        transactionTracer.SampleRand.Should().NotBeNull();
        transactionTracer.DynamicSamplingContext.Should().NotBeNull();
        if (replaySessionIsActive)
        {
            // We add the replay_id when we have an active replay session
            transactionTracer.DynamicSamplingContext!.Items["replay_id"].Should().Be(_fixture.ReplaySession.ActiveReplayId.ToString());
        }
        else
        {
            transactionTracer.DynamicSamplingContext!.Items.Should().NotContainKey("replay_id");
        }
    }

    [Fact]
    public void StartTransaction_NoDynamicSamplingContext_GeneratesSampleRand()
    {
        // Arrange
        var transactionContext = new TransactionContext("name", "operation");
        var customContext = new Dictionary<string, object>();

        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction(transactionContext, customContext);

        // Assert
        var transactionTracer = transaction.Should().BeOfType<TransactionTracer>().Subject;
        transactionTracer.SampleRand.Should().NotBeNull();
        transactionTracer.DynamicSamplingContext.Should().NotBeNull();
        transactionTracer.DynamicSamplingContext!.Items.Should().ContainKey("sample_rand");
        transactionTracer.DynamicSamplingContext.Items["sample_rand"].Should().Be(transactionTracer.SampleRand!.Value.ToString("N4", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void StartTransaction_DynamicSamplingContextWithoutSampleRand_SampleRandNotPropagated()
    {
        // Arrange
        var transactionContext = new TransactionContext("name", "operation");

        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction(transactionContext, new Dictionary<string, object>(), DynamicSamplingContext.Empty());

        // Assert
        var transactionTracer = transaction.Should().BeOfType<TransactionTracer>().Subject;
        transactionTracer.SampleRand.Should().NotBeNull();
        transactionTracer.DynamicSamplingContext.Should().NotBeNull();
        // See https://develop.sentry.dev/sdk/telemetry/traces/dynamic-sampling-context/#freezing-dynamic-sampling-context
        transactionTracer.DynamicSamplingContext!.Items.Should().NotContainKey("sample_rand");
    }

    [Fact]
    public void StartTransaction_DynamicSamplingContextWithSampleRand_InheritsSampleRand()
    {
        // Arrange
        var transactionContext = new TransactionContext("name", "operation");
        var dummyReplaySession = Substitute.For<IReplaySession>();
        var dsc = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sampled", "true"},
            {"sentry-sample_rate", "0.5"}, // Required in the baggage header, but ignored by sampling logic
            {"sentry-sample_rand", "0.1234"}
        }).CreateDynamicSamplingContext(dummyReplaySession);
        var originalDsc = dsc.Clone();

        _fixture.Options.TracesSampleRate = 0.4;
        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction(transactionContext, new Dictionary<string, object>(), dsc);

        // Assert
        var transactionTracer = transaction.Should().BeOfType<TransactionTracer>().Subject;
        transactionTracer.IsSampled.Should().BeTrue();
        transactionTracer.SampleRate.Should().Be(0.4);
        transactionTracer.SampleRand.Should().Be(0.1234);
        transactionTracer.DynamicSamplingContext.Should().BeEquivalentTo(originalDsc.CloneWithSampleRate(0.4));
    }

    [Theory]
    [InlineData(0.1, false)]
    [InlineData(0.2, true)]
    public void StartTransaction_TraceSampler_UsesSampleRand(double sampleRate, bool expectedIsSampled)
    {
        // Arrange
        var transactionContext = new TransactionContext("name", "operation");
        var customContext = new Dictionary<string, object>();
        var dsc = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sampled", "true"},
            {"sentry-sample_rate", "0.5"},
            {"sentry-sample_rand", "0.1234"}
        }).CreateDynamicSamplingContext(_fixture.ReplaySession);
        var originalDsc = dsc.Clone();

        _fixture.Options.TracesSampler = _ => sampleRate;
        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction(transactionContext, customContext, dsc);

        // Assert
        if (expectedIsSampled)
        {
            var transactionTracer = transaction.Should().BeOfType<TransactionTracer>().Subject;
            transactionTracer.IsSampled.Should().BeTrue();
            transactionTracer.SampleRate.Should().Be(sampleRate);
            transactionTracer.SampleRand.Should().Be(0.1234);
            transactionTracer.DynamicSamplingContext.Should().BeEquivalentTo(originalDsc.CloneWithSampleRate(sampleRate));
        }
        else
        {
            var unsampledTransaction = transaction.Should().BeOfType<UnsampledTransaction>().Subject;
            unsampledTransaction.IsSampled.Should().BeFalse();
            unsampledTransaction.SampleRate.Should().Be(sampleRate);
            unsampledTransaction.SampleRand.Should().Be(0.1234);
            unsampledTransaction.DynamicSamplingContext.Should().BeEquivalentTo(originalDsc.CloneWithSampleRate(sampleRate));
        }
    }

    [Theory]
    [InlineData(0.1, false)]
    [InlineData(0.2, true)]
    public void StartTransaction_StaticSampler_UsesSampleRand(double sampleRate, bool expectedIsSampled)
    {
        // Arrange
        var transactionContext = new TransactionContext("name", "operation");
        var customContext = new Dictionary<string, object>();
        var dummyReplaySession = Substitute.For<IReplaySession>();
        var dsc = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sample_rate", "0.5"}, // Static sampling ignores this and uses options.TracesSampleRate instead
            {"sentry-sample_rand", "0.1234"}
        }).CreateDynamicSamplingContext(dummyReplaySession);
        var originalDsc = dsc.Clone();

        _fixture.Options.TracesSampleRate = sampleRate;
        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction(transactionContext, customContext, dsc);

        // Assert
        if (expectedIsSampled)
        {
            var transactionTracer = transaction.Should().BeOfType<TransactionTracer>().Subject;
            transactionTracer.IsSampled.Should().BeTrue();
            transactionTracer.SampleRate.Should().Be(sampleRate);
            transactionTracer.SampleRand.Should().Be(0.1234);
            transactionTracer.DynamicSamplingContext.Should().BeEquivalentTo(originalDsc.CloneWithSampleRate(sampleRate));
        }
        else
        {
            var unsampledTransaction = transaction.Should().BeOfType<UnsampledTransaction>().Subject;
            unsampledTransaction.IsSampled.Should().BeFalse();
            unsampledTransaction.SampleRate.Should().Be(sampleRate);
            unsampledTransaction.SampleRand.Should().Be(0.1234);
            unsampledTransaction.DynamicSamplingContext.Should().BeEquivalentTo(originalDsc.CloneWithSampleRate(sampleRate));
        }
    }

    [Fact]
    public void StartTransaction_DifferentInstrumenter_SampledIn()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 1.0;
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;
        var hub = _fixture.GetSut();

        var transactionContext = new TransactionContext("name", "operation")
        {
            Instrumenter = Instrumenter.Sentry // The default... making it explicit for this test though
        };

        // Act
        var transaction = hub.StartTransaction(transactionContext);

        // Assert
        transaction.IsSampled.Should().BeTrue();
    }

    [Fact]
    public void StartTransaction_EnableTracing_Sampler_SampledIn()
    {
        // Arrange
        _fixture.Options.TracesSampler = _ => 1.0;
        _fixture.Options.TracesSampleRate = 1.0;
        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction("name", "operation");

        // Assert
        transaction.IsSampled.Should().BeTrue();
    }

    [Theory]
    [InlineData(0.25f)]
    [InlineData(0.50f)]
    [InlineData(0.75f)]
    public void StartTransaction_StaticSampling_AppropriateDistribution(float sampleRate)
    {
        // Arrange
        const int numEvents = 1000;
        const double allowedRelativeDeviation = 0.15;
        const uint allowedDeviation = (uint)(allowedRelativeDeviation * numEvents);
        var expectedSampled = (int)(sampleRate * numEvents);

        var worker = Substitute.For<IBackgroundWorker>();
        worker.EnqueueEnvelope(Arg.Any<Envelope>()).Returns(true);

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = sampleRate,
            AttachStacktrace = false,
            AutoSessionTracking = false,
            BackgroundWorker = worker
        };

        // This test expects an approximate uniform distribution of random numbers, so we'll retry a few times.
        TestHelpers.RetryTest(maxAttempts: 3, _output, () =>
        {
            var randomValuesFactory = new IsolatedRandomValuesFactory();
            var hub = new Hub(options, randomValuesFactory: randomValuesFactory);

            // Act
            var countSampled = 0;
            for (var i = 0; i < numEvents; i++)
            {
                var transaction = hub.StartTransaction($"name[{i}]", $"operation[{i}]");
                if (transaction.IsSampled == true)
                {
                    countSampled++;
                }
            }

            // Assert
            countSampled.Should().BeCloseTo(expectedSampled, allowedDeviation);
        });
    }

    [Fact]
    public void StartTransaction_TracesSampler_SampledIn()
    {
        // Arrange
        _fixture.Options.TracesSampler = ctx => ctx.TransactionContext.Name == "foo" ? 1 : 0;
        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction("foo", "op");

        // Assert
        transaction.IsSampled.Should().BeTrue();
    }

    [Fact]
    public void StartTransaction_TracesSampler_SampledOut()
    {
        // Arrange
        _fixture.Options.TracesSampler = ctx => ctx.TransactionContext.Name == "foo" ? 1 : 0;
        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction("bar", "op");

        // Assert
        transaction.IsSampled.Should().BeFalse();
    }

    [Fact]
    public void StartTransaction_TracesSampler_WithCustomContext_SampledIn()
    {
        // Arrange
        _fixture.Options.TracesSampler = ctx =>
            ctx.CustomSamplingContext.GetValueOrDefault("xxx") as string == "zzz" ? 1 : 0;
        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction(
            new TransactionContext("foo", "op"),
            new Dictionary<string, object> { ["xxx"] = "zzz" });

        // Assert
        transaction.IsSampled.Should().BeTrue();
    }

    [Fact]
    public void StartTransaction_TracesSampler_WithCustomContext_SampledOut()
    {
        // Arrange
        _fixture.Options.TracesSampler = ctx =>
            ctx.CustomSamplingContext.GetValueOrDefault("xxx") as string == "zzz" ? 1 : 0;
        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction(
            new TransactionContext("foo", "op"),
            new Dictionary<string, object> { ["xxx"] = "yyy" });

        // Assert
        transaction.IsSampled.Should().BeFalse();
    }

    [Fact]
    public void StartTransaction_TracesSampler_FallbackToStatic_SampledIn()
    {
        // Arrange
        _fixture.Options.TracesSampler = _ => null;
        _fixture.Options.TracesSampleRate = 1.0;
        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction("foo", "bar");

        // Assert
        transaction.IsSampled.Should().BeTrue();
    }

    [Fact]
    public void StartTransaction_TracesSampler_FallbackToStatic_SampledOut()
    {
        // Arrange
        _fixture.Options.TracesSampler = _ => null;
        _fixture.Options.TracesSampleRate = 0.0;
        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction("foo", "bar");

        // Assert
        transaction.IsSampled.Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetTraceHeader_ReturnsHeaderForActiveSpan(bool isSampled)
    {
        // Arrange
        _fixture.Options.TracesSampleRate = isSampled ? 1 : 0;
        var hub = _fixture.GetSut();
        var transaction = hub.StartTransaction("foo", "bar");
        hub.ConfigureScope(scope => scope.Transaction = transaction);

        // Act
        var header = hub.GetTraceHeader();

        // Assert
        header.Should().NotBeNull();
        header.SpanId.Should().Be(transaction.SpanId);
        header.TraceId.Should().Be(transaction.TraceId);
        header.IsSampled.Should().Be(transaction.IsSampled);
    }

    [Fact]
    public void GetTraceHeader_NoSpanActive_ReturnsHeaderFromPropagationContext()
    {
        // Arrange
        var hub = _fixture.GetSut();
        var propagationContext = new SentryPropagationContext(
            SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"),
            SpanId.Parse("2000000000000000"));
        hub.ConfigureScope(scope => scope.SetPropagationContext(propagationContext));

        // Act
        var header = hub.GetTraceHeader();

        // Assert
        header.Should().NotBeNull();
        header.SpanId.Should().Be(propagationContext.SpanId);
        header.TraceId.Should().Be(propagationContext.TraceId);
        header.IsSampled.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetBaggage_SpanActive_ReturnsBaggageFromSpan(bool isSampled)
    {
        // Arrange
        _fixture.Options.TracesSampleRate = isSampled ? 1 : 0;
        var hub = _fixture.GetSut();

        var expectedBaggage = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sample_rate", "0.0"}
        });
        var replaySession = Substitute.For<IReplaySession>();
        replaySession.ActiveReplayId.Returns((SentryId?)null);
        var dsc = expectedBaggage.CreateDynamicSamplingContext(replaySession);

        var transaction = hub.StartTransaction(new TransactionContext("name", "op"),
            new Dictionary<string, object>(), dsc);
        hub.ConfigureScope(scope => scope.Transaction = transaction);

        // Act
        var baggage = hub.GetBaggage();

        // Assert
        baggage.Should().NotBeNull();
        var sampleRand = isSampled ? ((TransactionTracer)transaction).SampleRand : ((UnsampledTransaction)transaction).SampleRand;
        baggage.Members.Should().Equal([
            new KeyValuePair<string, string>("sentry-trace_id", "43365712692146d08ee11a729dfbcaca"),
            new KeyValuePair<string, string>("sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"),
            new KeyValuePair<string, string>("sentry-sample_rate", isSampled ? "1" : "0"),
            new KeyValuePair<string, string>("sentry-sample_rand", sampleRand!.Value.ToString(CultureInfo.InvariantCulture)),
        ]);
    }

    [Fact]
    public void GetBaggage_NoSpanActive_ReturnsBaggageFromPropagationContext()
    {
        // Arrange
        var hub = _fixture.GetSut();
        var propagationContext = new SentryPropagationContext(
            SentryId.Parse("43365712692146d08ee11a729dfbcaca"), SpanId.Parse("1000000000000000"));
        hub.ConfigureScope(scope => scope.SetPropagationContext(propagationContext));

        // Act
        var baggage = hub.GetBaggage();

        // Assert
        baggage.Should().NotBeNull();
        Assert.Contains("sentry-trace_id=43365712692146d08ee11a729dfbcaca", baggage!.ToString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetTraceparentHeader_ReturnsHeaderForActiveSpan(bool isSampled)
    {
        // Arrange
        _fixture.Options.TracesSampleRate = isSampled ? 1 : 0;
        var hub = _fixture.GetSut();
        var transaction = hub.StartTransaction("foo", "bar");
        hub.ConfigureScope(scope => scope.Transaction = transaction);

        // Act
        var header = hub.GetTraceparentHeader();

        // Assert
        header.Should().NotBeNull();
        header.SpanId.Should().Be(transaction.SpanId);
        header.TraceId.Should().Be(transaction.TraceId);
        header.IsSampled.Should().Be(transaction.IsSampled);
    }

    [Fact]
    public void GetTraceparentHeader_NoSpanActive_ReturnsHeaderFromPropagationContext()
    {
        // Arrange
        var hub = _fixture.GetSut();
        var propagationContext = new SentryPropagationContext(
            SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"),
            SpanId.Parse("2000000000000000"));
        hub.ConfigureScope(scope => scope.SetPropagationContext(propagationContext));

        // Act
        var header = hub.GetTraceparentHeader();

        // Assert
        header.Should().NotBeNull();
        header.SpanId.Should().Be(propagationContext.SpanId);
        header.TraceId.Should().Be(propagationContext.TraceId);
        header.IsSampled.Should().BeNull();
    }

    [Fact]
    public void ContinueTrace_ReceivesHeaders_SetsPropagationContextAndReturnsTransactionContext()
    {
        // Arrange
        var hub = _fixture.GetSut();
        var propagationContext = new SentryPropagationContext(
            SentryId.Parse("43365712692146d08ee11a729dfbcaca"), SpanId.Parse("1000000000000000"));
        hub.ConfigureScope(scope => scope.SetPropagationContext(propagationContext));

        var traceHeader = new SentryTraceHeader(SentryId.Parse("5bd5f6d346b442dd9177dce9302fd737"),
            SpanId.Parse("2000000000000000"), null);
        var baggageHeader = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "5bd5f6d346b442dd9177dce9302fd737"},
            {"sentry-public_key", "49d0f7386ad645858ae85020e393bef3"},
            {"sentry-sample_rate", "1.0"}
        });

        hub.ScopeManager.ConfigureScope(scope => scope.PropagationContext.TraceId.Should().Be(SentryId.Parse("43365712692146d08ee11a729dfbcaca"))); // Sanity check

        // Act
        var transactionContext = hub.ContinueTrace(traceHeader, baggageHeader, "test-name");

        // Assert
        hub.ScopeManager.ConfigureScope(scope =>
        {
            scope.PropagationContext.TraceId.Should().Be(SentryId.Parse("5bd5f6d346b442dd9177dce9302fd737"));
            scope.PropagationContext.ParentSpanId.Should().Be(SpanId.Parse("2000000000000000"));
            Assert.NotNull(scope.PropagationContext._dynamicSamplingContext);
            scope.PropagationContext._dynamicSamplingContext.Items.Should().Contain(baggageHeader.GetSentryMembers());
        });

        transactionContext.TraceId.Should().Be(SentryId.Parse("5bd5f6d346b442dd9177dce9302fd737"));
        transactionContext.ParentSpanId.Should().Be(SpanId.Parse("2000000000000000"));
    }

    [Fact]
    public void ContinueTrace_DoesNotReceiveHeaders_CreatesRootTrace()
    {
        // Arrange
        var hub = _fixture.GetSut();

        // Act
        var transactionContext = hub.ContinueTrace((SentryTraceHeader)null, (BaggageHeader)null, "test-name", "test-operation");

        // Assert
        hub.ScopeManager.ConfigureScope(scope =>
        {
            Assert.Null(scope.PropagationContext.ParentSpanId);
            Assert.Null(scope.PropagationContext._dynamicSamplingContext);
        });

        transactionContext.Name.Should().Be("test-name");
        transactionContext.Operation.Should().Be("test-operation");
        transactionContext.SpanId.Should().NotBeNull();
        transactionContext.ParentSpanId.Should().BeNull();
        transactionContext.TraceId.Should().NotBeNull();
        transactionContext.IsSampled.Should().BeNull();
        transactionContext.IsParentSampled.Should().BeNull();
    }

    [Fact]
    public void ContinueTrace_ReceivesHeadersAsStrings_SetsPropagationContextAndReturnsTransactionContext()
    {
        // Arrange
        var hub = _fixture.GetSut();
        var propagationContext = new SentryPropagationContext(
            SentryId.Parse("43365712692146d08ee11a729dfbcaca"), SpanId.Parse("1000000000000000"));
        hub.ConfigureScope(scope => scope.SetPropagationContext(propagationContext));
        var traceHeader = "5bd5f6d346b442dd9177dce9302fd737-2000000000000000";
        var baggageHeader = "sentry-trace_id=5bd5f6d346b442dd9177dce9302fd737, sentry-public_key=49d0f7386ad645858ae85020e393bef3, sentry-sample_rate=1.0";

        hub.ScopeManager.ConfigureScope(scope => scope.PropagationContext.TraceId.Should().Be(SentryId.Parse("43365712692146d08ee11a729dfbcaca"))); // Sanity check

        // Act
        var transactionContext = hub.ContinueTrace(traceHeader, baggageHeader, "test-name");

        // Assert
        hub.ScopeManager.ConfigureScope(scope =>
        {
            scope.PropagationContext.TraceId.Should().Be(SentryId.Parse("5bd5f6d346b442dd9177dce9302fd737"));
            scope.PropagationContext.ParentSpanId.Should().Be(SpanId.Parse("2000000000000000"));
            Assert.NotNull(scope.PropagationContext._dynamicSamplingContext);
            scope.PropagationContext._dynamicSamplingContext.ToBaggageHeader().Members.Should().Contain(BaggageHeader.TryParse(baggageHeader)!.Members);
        });

        transactionContext.TraceId.Should().Be(SentryId.Parse("5bd5f6d346b442dd9177dce9302fd737"));
        transactionContext.ParentSpanId.Should().Be(SpanId.Parse("2000000000000000"));
    }

    [Fact]
    public void ContinueTrace_DoesNotReceiveHeadersAsStrings_CreatesRootTrace()
    {
        // Arrange
        var hub = _fixture.GetSut();

        // Act
        var transactionContext = hub.ContinueTrace((string)null, (string)null, "test-name");

        // Assert
        hub.ScopeManager.ConfigureScope(scope =>
        {
            Assert.Null(scope.PropagationContext.ParentSpanId);
            Assert.Null(scope.PropagationContext._dynamicSamplingContext);
        });

        transactionContext.Name.Should().Be("test-name");
        transactionContext.Operation.Should().BeEmpty();
        transactionContext.SpanId.Should().NotBeNull();
        transactionContext.ParentSpanId.Should().BeNull();
        transactionContext.TraceId.Should().NotBeNull();
        transactionContext.IsSampled.Should().BeNull();
        transactionContext.IsParentSampled.Should().BeNull();
    }

    [Fact]
    public void CaptureTransaction_AfterTransactionFinishes_ResetsTransactionOnScope()
    {
        // Arrange
        var hub = _fixture.GetSut();
        var transaction = hub.StartTransaction("foo", "bar");

        hub.ConfigureScope(scope => scope.Transaction = transaction);

        // Act
        transaction.Finish();

        // Assert
        hub.ScopeManager.ConfigureScope(scope => scope.Transaction.Should().BeNull());
    }

#nullable enable
    private class ThrowingProfilerFactory : ITransactionProfilerFactory
    {
        public ITransactionProfiler? Start(ITransactionTracer _, CancellationToken __) => new ThrowingProfiler();
    }

    internal class ThrowingProfiler : ITransactionProfiler
    {
        public void Finish() { }

        public Sentry.Protocol.Envelopes.ISerializable Collect(SentryTransaction _) => throw new Exception("test");
    }

    private class AsyncThrowingProfilerFactory : ITransactionProfilerFactory
    {
        public ITransactionProfiler? Start(ITransactionTracer _, CancellationToken __) => new AsyncThrowingProfiler();
    }

    internal class AsyncThrowingProfiler : ITransactionProfiler
    {
        public void Finish() { }

        public Sentry.Protocol.Envelopes.ISerializable Collect(SentryTransaction transaction)
            => AsyncJsonSerializable.CreateFrom(CollectAsync(transaction));

        private async Task<ProfileInfo> CollectAsync(SentryTransaction transaction)
        {
            await Task.Delay(1);
            throw new Exception("test");
        }
    }
    private class TestProfilerFactory : ITransactionProfilerFactory
    {
        public ITransactionProfiler? Start(ITransactionTracer _, CancellationToken __) => new TestProfiler();
    }

    internal class TestProfiler : ITransactionProfiler
    {
        public void Finish() { }

        public Sentry.Protocol.Envelopes.ISerializable Collect(SentryTransaction _) => new JsonSerializable(new ProfileInfo());
    }

#nullable disable

    [Fact]
    public async Task CaptureTransaction_WithSyncThrowingTransactionProfiler_DoesntSendTransaction()
    {
        // Arrange
        var transport = new FakeTransport();
        using var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            AutoSessionTracking = false,
            TracesSampleRate = 1.0,
            ProfilesSampleRate = 1.0,
            Transport = transport,
            TransactionProfilerFactory = new ThrowingProfilerFactory()
        });

        // Act
        hub.StartTransaction("foo", "bar").Finish();
        await hub.FlushAsync();

        // Assert
        transport.GetSentEnvelopes().Should().BeEmpty();
    }

    [SkippableFact]
    public async Task CaptureTransaction_WithAsyncThrowingTransactionProfiler_SendsTransactionWithoutProfile()
    {
#if __ANDROID__
        Skip.If(true, "Flaky on Android");
#endif

        // Arrange
        var transport = new FakeTransport();
        var logger = new TestOutputDiagnosticLogger(_output);
        using var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            AutoSessionTracking = false,
            TracesSampleRate = 1.0,
            ProfilesSampleRate = 1.0,
            Transport = transport,
            TransactionProfilerFactory = new AsyncThrowingProfilerFactory(),
            DiagnosticLogger = logger,
        });

        // Act
        hub.StartTransaction("foo", "bar").Finish();
        await hub.FlushAsync();

        // Assert
        transport.GetSentEnvelopes().Should().HaveCount(1);
        var envelope = transport.GetSentEnvelopes().Single();

        using var stream = new MemoryStream();
        envelope.Serialize(stream, logger);
        stream.Flush();
        var envelopeStr = Encoding.UTF8.GetString(stream.ToArray());
        var lines = envelopeStr.Split('\n');
        lines.Should().HaveCount(4);
        lines[0].Should().StartWith("{\"sdk\"");
        lines[1].Should().StartWith("{\"type\":\"transaction\",\"length\":");
        lines[2].Should().StartWith("{\"type\":\"transaction\"");
        lines[3].Should().BeEmpty();
    }

    [Fact]
    public async Task CaptureTransaction_WithTransactionProfiler_SendsTransactionWithProfile()
    {
        // Arrange
        var transport = new FakeTransport();
        var logger = new TestOutputDiagnosticLogger(_output);
        using var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            AutoSessionTracking = false,
            TracesSampleRate = 1.0,
            ProfilesSampleRate = 1.0,
            Transport = transport,
            TransactionProfilerFactory = new TestProfilerFactory(),
            DiagnosticLogger = logger,
        });

        // Act
        hub.StartTransaction("foo", "bar").Finish();
        await hub.FlushAsync();

        // Assert
        transport.GetSentEnvelopes().Should().HaveCount(1);
        var envelope = transport.GetSentEnvelopes().Single();

        using var stream = new MemoryStream();
        envelope.Serialize(stream, logger);
        stream.Flush();
        var envelopeStr = Encoding.UTF8.GetString(stream.ToArray());
        var lines = envelopeStr.Split('\n');
        lines.Should().HaveCount(6);
        lines[0].Should().StartWith("{\"sdk\"");
        lines[1].Should().StartWith("{\"type\":\"transaction\",\"length\":");
        lines[2].Should().StartWith("{\"type\":\"transaction\"");
        lines[3].Should().StartWith("{\"type\":\"profile\",\"length\":");
        lines[4].Should().Contain("\"profile\":{");
        lines[5].Should().BeEmpty();
    }

    [Fact]
    public void Logger_IsDisabled_DoesNotCaptureLog()
    {
        // Arrange
        Assert.False(_fixture.Options.Experimental.EnableLogs);
        var hub = _fixture.GetSut();

        // Act
        hub.Logger.LogWarning("Message");
        hub.Logger.Flush();

        // Assert
        _fixture.Client.Received(0).CaptureEnvelope(
            Arg.Is<Envelope>(envelope =>
                envelope.Items.Single(item => item.Header["type"].Equals("log")).Payload.GetType().IsAssignableFrom(typeof(JsonSerializable))
            )
        );
        hub.Logger.Should().BeOfType<DisabledSentryStructuredLogger>();
    }

    [Fact]
    public void Logger_IsEnabled_DoesCaptureLog()
    {
        // Arrange
        _fixture.Options.Experimental.EnableLogs = true;
        var hub = _fixture.GetSut();

        // Act
        hub.Logger.LogWarning("Message");
        hub.Logger.Flush();

        // Assert
        _fixture.Client.Received(1).CaptureEnvelope(
            Arg.Is<Envelope>(envelope =>
                envelope.Items.Single(item => item.Header["type"].Equals("log")).Payload.GetType().IsAssignableFrom(typeof(JsonSerializable))
            )
        );
        hub.Logger.Should().BeOfType<DefaultSentryStructuredLogger>();
    }

    [Fact]
    public void Logger_EnableAfterCreate_HasNoEffect()
    {
        // Arrange
        Assert.False(_fixture.Options.Experimental.EnableLogs);
        var hub = _fixture.GetSut();

        // Act
        _fixture.Options.Experimental.EnableLogs = true;

        // Assert
        hub.Logger.Should().BeOfType<DisabledSentryStructuredLogger>();
    }

    [Fact]
    public void Logger_DisableAfterCreate_HasNoEffect()
    {
        // Arrange
        _fixture.Options.Experimental.EnableLogs = true;
        var hub = _fixture.GetSut();

        // Act
        _fixture.Options.Experimental.EnableLogs = false;

        // Assert
        hub.Logger.Should().BeOfType<DefaultSentryStructuredLogger>();
    }

    [Fact]
    public async Task Logger_FlushAsync_DoesCaptureLog()
    {
        // Arrange
        _fixture.Options.Experimental.EnableLogs = true;
        var hub = _fixture.GetSut();

        // Act
        hub.Logger.LogWarning("Message");
        await hub.FlushAsync();

        // Assert
        _fixture.Client.Received(1).CaptureEnvelope(
            Arg.Is<Envelope>(envelope =>
                envelope.Items.Single(item => item.Header["type"].Equals("log")).Payload.GetType().IsAssignableFrom(typeof(JsonSerializable))
            )
        );
        await _fixture.Client.Received(1).FlushAsync(
            Arg.Is<TimeSpan>(timeout =>
                timeout.Equals(_fixture.Options.FlushTimeout)
            )
        );
        hub.Logger.Should().BeOfType<DefaultSentryStructuredLogger>();
    }

    [Fact]
    public void Logger_Dispose_DoesCaptureLog()
    {
        // Arrange
        _fixture.Options.Experimental.EnableLogs = true;
        var hub = _fixture.GetSut();

        // Act
        hub.Logger.LogWarning("Message");
        hub.Dispose();

        // Assert
        _fixture.Client.Received(1).CaptureEnvelope(
            Arg.Is<Envelope>(envelope =>
                envelope.Items.Single(item => item.Header["type"].Equals("log")).Payload.GetType().IsAssignableFrom(typeof(JsonSerializable))
            )
        );
        _fixture.Client.Received(1).FlushAsync(
            Arg.Is<TimeSpan>(timeout =>
                timeout.Equals(_fixture.Options.ShutdownTimeout)
            )
        );
        hub.Logger.Should().BeOfType<DefaultSentryStructuredLogger>();
    }

    [Fact]
    public void Dispose_IsEnabled_SetToFalse()
    {
        // Arrange
        var hub = _fixture.GetSut();
        hub.IsEnabled.Should().BeTrue();

        // Act
        hub.Dispose();

        // Assert
        hub.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Dispose_CalledSecondTime_ClientFlushedOnce()
    {
        var hub = _fixture.GetSut();

        // Act
        hub.Dispose();
        hub.Dispose();

        // Assert
        _fixture.Client.Received(1).FlushAsync(Arg.Any<TimeSpan>());
    }

    [Fact]
    public void StartSession_CapturesUpdate()
    {
        // Arrange
        _fixture.Options.Release = "release";
        var hub = _fixture.GetSut();

        // Act
        hub.StartSession();

        // Assert
        _fixture.Client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => s.IsInitial));
    }

    [Fact]
    public void StartSession_GlobalSessionManager_ExceptionOnCrashLastRun_CapturesUpdate()
    {
        // Arrange
        var sessionUpdate = new GlobalSessionManagerTests().TryRecoverPersistedSessionWithExceptionOnLastRun();
        var newSession = new SessionUpdate(Substitute.For<ISentrySession>(), false, default, 0, null);

        var sessionManager = Substitute.For<ISessionManager>();
        sessionManager.TryRecoverPersistedSession().Returns(sessionUpdate);
        sessionManager.StartSession().Returns(newSession);

        _fixture.SessionManager = sessionManager;
        _fixture.Options.Release = "release";
        var hub = _fixture.GetSut();

        // Act
        hub.StartSession();

        // Assert
        _fixture.Client.Received().CaptureSession(Arg.Is(sessionUpdate));
        _fixture.Client.Received().CaptureSession(Arg.Is(newSession));
    }

    [Fact]
    public void EndSession_CapturesUpdate()
    {
        // Arrange
        _fixture.Options.Release = "release";
        var hub = _fixture.GetSut();

        hub.StartSession();

        // Act
        hub.EndSession();

        // Assert
        _fixture.Client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => !s.IsInitial));
    }

    [Fact]
    public void Ctor_AutoSessionTrackingEnabled_StartsSession()
    {
        // Arrange
        _fixture.Options.AutoSessionTracking = true;
        _fixture.Options.Release = "release";

        // Act
        _ = _fixture.GetSut();

        // Assert
        _fixture.Client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => s.IsInitial));
    }

    [Fact]
    public void Ctor_GlobalModeTrue_DoesNotPushScope()
    {
        // Arrange
        _fixture.ScopeManager = Substitute.For<IInternalScopeManager>();
        _fixture.Options.IsGlobalModeEnabled = true;

        // Act
        _ = _fixture.GetSut();

        // Assert
        _fixture.ScopeManager.DidNotReceiveWithAnyArgs().PushScope();
    }

    [Fact]
    public void Ctor_GlobalModeFalse_DoesPushScope()
    {
        // Arrange
        _fixture.ScopeManager = Substitute.For<IInternalScopeManager>();
        _fixture.Options.IsGlobalModeEnabled = false;

        // Act
        _ = _fixture.GetSut();

        // Assert
        _fixture.ScopeManager.Received(1).PushScope();
    }

    [Fact]
    public void ResumeSession_WithinAutoTrackingInterval_ContinuesSameSession()
    {
        // Arrange
        _fixture.Options.AutoSessionTrackingInterval = TimeSpan.FromSeconds(9999);
        var hub = _fixture.GetSut();

        hub.StartSession();
        hub.PauseSession();

        // Act
        hub.ResumeSession();

        // Assert
        _fixture.Client.DidNotReceive().CaptureSession(Arg.Is<SessionUpdate>(s => s.EndStatus != null));
    }

    [Fact]
    public void ResumeSession_BeyondAutoTrackingInterval_EndsPreviousSessionAndStartsANewOne()
    {
        // Arrange
        _fixture.Options.AutoSessionTrackingInterval = TimeSpan.FromMilliseconds(10);
        _fixture.Options.Release = "release";
        _fixture.Clock = Substitute.For<ISystemClock>();
        _fixture.SessionManager = new GlobalSessionManager(_fixture.Options, _fixture.Clock);

        var hub = _fixture.GetSut();
        var now = DateTimeOffset.Now;

        _fixture.Clock.GetUtcNow().Returns(now);

        hub.StartSession();
        hub.PauseSession();

        _fixture.Clock.GetUtcNow().Returns(now.AddDays(1));

        // Act
        hub.ResumeSession();

        // Assert
        _fixture.Client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => s.EndStatus == SessionEndStatus.Exited));
        _fixture.Client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => s.IsInitial));
    }

    [Fact]
    public void ResumeSession_NoActiveSession_DoesNothing()
    {
        // Arrange
        _fixture.Options.AutoSessionTrackingInterval = TimeSpan.FromMilliseconds(10);
        _fixture.Clock = Substitute.For<ISystemClock>();
        _fixture.SessionManager = new GlobalSessionManager(_fixture.Options, _fixture.Clock);

        var hub = _fixture.GetSut();
        var now = DateTimeOffset.Now;

        _fixture.Clock.GetUtcNow().Returns(now);

        hub.PauseSession();

        _fixture.Clock.GetUtcNow().Returns(now.AddDays(1));

        // Act
        hub.ResumeSession();

        // Assert
        _fixture.Client.DidNotReceive().CaptureSession(Arg.Any<SessionUpdate>());
    }

    [Fact]
    public void ResumeSession_NoPausedSession_DoesNothing()
    {
        // Arrange
        _fixture.Options.AutoSessionTrackingInterval = TimeSpan.FromMilliseconds(10);
        _fixture.Clock = Substitute.For<ISystemClock>();
        _fixture.SessionManager = new GlobalSessionManager(_fixture.Options, _fixture.Clock);

        var hub = _fixture.GetSut();
        var now = DateTimeOffset.Now;

        _fixture.Clock.GetUtcNow().Returns(now);

        hub.StartSession();

        _fixture.Clock.GetUtcNow().Returns(now.AddDays(1));

        // Act
        hub.ResumeSession();

        // Assert
        _fixture.Client.DidNotReceive().CaptureSession(Arg.Is<SessionUpdate>(s => s.EndStatus != null));
    }

    [Theory]
    [InlineData(SentryLevel.Warning)]
    [InlineData(SentryLevel.Info)]
    [InlineData(SentryLevel.Debug)]
    [InlineData(SentryLevel.Error)]
    [InlineData(SentryLevel.Fatal)]
    public void CaptureEvent_MessageOnlyEvent_SpanLinkedToEventContext(SentryLevel level)
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 1.0;
        var hub = _fixture.GetSut();

        var scope = new Scope();
        var evt = new SentryEvent
        {
            Message = "Logger error",
            Level = level
        };
        scope.Transaction = hub.StartTransaction("transaction", "operation");

        var child = scope.Transaction.StartChild("child", "child");

        // Act
        hub.CaptureEvent(evt, scope);

        // Assert
        Assert.Equal(child.SpanId, evt.Contexts.Trace.SpanId);
        Assert.Equal(child.TraceId, evt.Contexts.Trace.TraceId);
        Assert.Equal(child.ParentSpanId, evt.Contexts.Trace.ParentSpanId);
        Assert.False(child.IsFinished);
        Assert.Null(child.Status);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CaptureEvent_HubEnabled(bool enabled)
    {
        // Arrange
        var hub = _fixture.GetSut();
        if (!enabled)
        {
            hub.Dispose();
        }

        var evt = new SentryEvent();

        // Act
        hub.CaptureEvent(evt);

        // Assert
        _fixture.Client.Received(enabled ? 1 : 0).CaptureEvent(Arg.Any<SentryEvent>(), Arg.Any<Scope>(), Arg.Any<SentryHint>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CaptureFeedback_HubEnabled(bool enabled)
    {
        // Arrange
        var expectedId = enabled ? SentryId.Create() : SentryId.Empty;
        var expectedResult = enabled ? CaptureFeedbackResult.Success : CaptureFeedbackResult.DisabledHub;
        var hub = _fixture.GetSut();
        if (enabled)
        {
            _fixture.Client.CaptureFeedback(Arg.Any<SentryFeedback>(), out Arg.Any<CaptureFeedbackResult>(),
                    Arg.Any<Scope>(), Arg.Any<SentryHint>())
                .Returns(callInfo =>
                {
                    callInfo[1] = expectedResult; // Set the out parameter
                    return expectedId; // Return value of the method
                });
        }
        else
        {
            hub.Dispose();
        }

        var feedback = new SentryFeedback("Test feedback");

        // Act
        var id = hub.CaptureFeedback(feedback, out var result);

        // Assert
        id.Should().Be(expectedId);
        result.Should().Be(expectedResult);
        _fixture.Client.Received(enabled ? 1 : 0).CaptureFeedback(Arg.Any<SentryFeedback>(),
            out Arg.Any<CaptureFeedbackResult>(), Arg.Any<Scope>(), Arg.Any<SentryHint>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CaptureFeedback_ConfigureScope_ScopeApplied(bool enabled)
    {
        // Arrange
        var hub = _fixture.GetSut();
        if (!enabled)
        {
            hub.Dispose();
        }

        var feedback = new SentryFeedback("Test feedback");

        // Act
        hub.CaptureFeedback(feedback, s => s.SetTag("foo", "bar"));

        // Assert
        _fixture.Client.Received(enabled ? 1 : 0).CaptureFeedback(Arg.Any<SentryFeedback>(), Arg.Is<Scope>(s => s.Tags["foo"] == "bar"), Arg.Any<SentryHint>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CaptureAttachment_HubEnabled(bool enabled)
    {
        // Arrange
        var hub = _fixture.GetSut();
        if (!enabled)
        {
            hub.Dispose();
        }

        _fixture.Client.CaptureEnvelope(Arg.Any<Envelope>()).Returns(true);

        var eventId = SentryId.Create();
        var attachment = new SentryAttachment(
            AttachmentType.Default,
            new ByteAttachmentContent("test content"u8.ToArray()),
            "test.txt",
            "text/plain");

        // Act
        var result = hub.CaptureAttachment(eventId, attachment);

        // Assert
        result.Should().Be(enabled);
        _fixture.Client.Received(enabled ? 1 : 0).CaptureEnvelope(Arg.Any<Envelope>());
    }

    [Fact]
    public void CaptureAttachment_SentryIdEmpty_ReturnsFalse()
    {
        // Arrange
        var hub = _fixture.GetSut();

        var eventId = SentryId.Empty;
        var attachment = new SentryAttachment(AttachmentType.Default, NullAttachmentContent.Instance, "test.txt", "text/plain");

        // Act
        var result = hub.CaptureAttachment(eventId, attachment);

        // Assert
        result.Should().Be(false);
        _fixture.Client.DidNotReceive().CaptureEnvelope(Arg.Any<Envelope>());
    }

    [Fact]
    public void CaptureAttachment_AttachmentNull_ReturnsFalse()
    {
        // Arrange
        var hub = _fixture.GetSut();
        var eventId = SentryId.Create();

        // Act
        var result = hub.CaptureAttachment(eventId, null!);

        // Assert
        result.Should().Be(false);
        _fixture.Client.DidNotReceive().CaptureEnvelope(Arg.Any<Envelope>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CaptureSession_HubEnabled(bool enabled)
    {
        // Arrange
        _fixture.Options.Release = "release";
        var hub = _fixture.GetSut();
        if (!enabled)
        {
            hub.Dispose();
        }

        // Act
        hub.StartSession();

        // Assert
        _fixture.Client.Received(enabled ? 1 : 0).CaptureSession(Arg.Any<SessionUpdate>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CaptureCheckIn_HubEnabled(bool enabled)
    {
        // Arrange
        var hub = _fixture.GetSut();
        if (!enabled)
        {
            hub.Dispose();
        }

        // Act
        _ = hub.CaptureCheckIn("test-slug", CheckInStatus.InProgress);

        // Assert
        _fixture.Client.Received(enabled ? 1 : 0).CaptureCheckIn(Arg.Any<string>(), Arg.Any<CheckInStatus>(), scope: Arg.Any<Scope>());
    }

    [Fact]
    public void CaptureCheckIn_HubDisabled_ReturnsEmptySentryId()
    {
        // Arrange
        var hub = _fixture.GetSut();
        hub.Dispose();

        // Act
        var checkInId = hub.CaptureCheckIn("test-slug", CheckInStatus.InProgress);

        // Assert
        Assert.Equal(checkInId, SentryId.Empty);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CaptureTransaction_HubEnabled(bool enabled)
    {
        // Arrange
        var hub = _fixture.GetSut();
        if (!enabled)
        {
            hub.Dispose();
        }

        // Act
        var transaction = hub.StartTransaction("test", "test");
        transaction.Finish();

        // Assert
        if (enabled)
        {
            _fixture.Client.Received().CaptureTransaction(Arg.Is<SentryTransaction>(t => t.IsSampled == enabled),
                Arg.Any<Scope>(), Arg.Any<SentryHint>());
        }
        else
        {
            transaction.Should().Be(NoOpTransaction.Instance);
            _fixture.Client.DidNotReceive().CaptureTransaction(Arg.Any<SentryTransaction>(), Arg.Any<Scope>(), Arg.Any<SentryHint>());
        }
    }

    [Fact]
    public void CaptureTransaction_Client_Gets_Hint()
    {
        // Arrange
        var hub = _fixture.GetSut();

        // Act
        var transaction = hub.StartTransaction("test", "test");
        transaction.Finish();

        // Assert
        _fixture.Client.Received().CaptureTransaction(Arg.Any<SentryTransaction>(), Arg.Any<Scope>(), Arg.Any<SentryHint>());
    }

    [SkippableTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FlushOnDispose_SendsEnvelope(bool cachingEnabled)
    {
#if __IOS__
        Skip.If(true, "Flaky on iOS");
#endif

        // Arrange
        using var cacheDirectory = new TempDirectory();
        var transport = Substitute.For<ITransport>();

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            AutoSessionTracking = false,
            IsGlobalModeEnabled = true,
            Transport = transport
        };

        if (cachingEnabled)
        {
            options.CacheDirectoryPath = cacheDirectory.Path;
        }

        var hub = new Hub(options);
        var id = hub.CaptureEvent(new SentryEvent());

        // Act
        // Disposing the hub should flush the client and send the envelope.
        // If caching is enabled, it should flush the cache as well.
        // Either way, the envelope should be sent.
        hub.Dispose();

        // Assert
        await transport.Received(1)
            .SendEnvelopeAsync(Arg.Is<Envelope>(env => (string)env.Header["event_id"] == id.ToString()),
                Arg.Any<CancellationToken>());
    }

    private static Scope GetCurrentScope(Hub hub) => hub.ScopeManager.GetCurrent().Key;

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.com")]
    [InlineData("user+tag@example.com")]
    public void CaptureFeedback_ValidEmail_FeedbackRegistered(string email)
    {
        // Arrange
        var hub = _fixture.GetSut();
        var feedback = new SentryFeedback("Test feedback", email);

        // Act
        hub.CaptureFeedback(feedback);

        // Assert
        _fixture.Client.Received(1).CaptureFeedback(Arg.Any<SentryFeedback>(), Arg.Any<Scope>(), Arg.Any<SentryHint>());
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("missing@domain")]
    [InlineData("@missing-local.com")]
    [InlineData("spaces in@email.com")]
    public void CaptureFeedback_InvalidEmail_FeedbackDropped(string email)
    {
        // Arrange
        _fixture.Options.Debug = true;
        _fixture.Options.DiagnosticLogger = Substitute.For<IDiagnosticLogger>();
        _fixture.Options.DiagnosticLogger!.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
        var hub = _fixture.GetSut();
        var feedback = new SentryFeedback("Test feedback", email);

        // Act
        hub.CaptureFeedback(feedback);

        // Assert
        _fixture.Options.DiagnosticLogger.Received(1).Log(
            SentryLevel.Warning,
            Arg.Is<string>(s => s.Contains("invalid email format")),
            null,
            Arg.Any<object[]>());
        _fixture.Client.Received(1).CaptureFeedback(Arg.Is<SentryFeedback>(f => f.ContactEmail.IsNull()),
            Arg.Any<Scope>(), Arg.Any<SentryHint>());
    }

    private class TestDisposableIntegration : ISdkIntegration, IDisposable
    {
        public int Registered { get; private set; }
        public int Disposed { get; private set; }

        public void Register(IHub hub, SentryOptions options)
        {
            Registered++;
        }

        protected virtual void Cleanup()
        {
            Disposed++;
        }

        public void Dispose()
        {
            Cleanup();
        }
    }

    private class TestFlakyDisposableIntegration : TestDisposableIntegration
    {
        protected override void Cleanup()
        {
            throw new InvalidOperationException("Cleanup failed");
        }
    }

    [Fact]
    public void Dispose_IntegrationsWithCleanup_CleanupCalled()
    {
        // Arrange
        var integration1 = new TestDisposableIntegration();
        var integration2 = Substitute.For<ISdkIntegration>();
        var integration3 = new TestDisposableIntegration();
        _fixture.Options.AddIntegration(integration1);
        _fixture.Options.AddIntegration(integration2);
        _fixture.Options.AddIntegration(integration3);
        var hub = _fixture.GetSut();

        // Act
        hub.Dispose();

        // Assert
        integration1.Disposed.Should().Be(1);
        integration3.Disposed.Should().Be(1);
    }

    [Fact]
    public void Dispose_CleanupThrowsException_ExceptionHandledAndLogged()
    {
        // Arrange
        var integration1 = new TestDisposableIntegration();
        var integration2 = new TestFlakyDisposableIntegration();
        var integration3 = new TestDisposableIntegration();
        _fixture.Options.AddIntegration(integration1);
        _fixture.Options.AddIntegration(integration2);
        _fixture.Options.AddIntegration(integration3);
        _fixture.Options.Debug = true;
        _fixture.Options.DiagnosticLogger = Substitute.For<IDiagnosticLogger>();
        _fixture.Options.DiagnosticLogger!.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
        var hub = _fixture.GetSut();

        // Act
        hub.Dispose();

        // Assert
        integration1.Disposed.Should().Be(1);
        integration2.Disposed.Should().Be(0);
        integration3.Disposed.Should().Be(1);
        _fixture.Options.DiagnosticLogger.Received(1).Log(
            SentryLevel.Error,
            Arg.Is<string>(s => s.Contains("Failed to dispose integration")),
            Arg.Any<InvalidOperationException>(),
            Arg.Any<object[]>());
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_CleanupCalledOnlyOnce()
    {
        // Arrange
        var integration = new TestDisposableIntegration();
        _fixture.Options.AddIntegration(integration);
        var hub = _fixture.GetSut();

        // Act
        hub.Dispose();
        hub.Dispose();

        // Assert
        integration.Disposed.Should().Be(1);
    }
}

#if NET6_0_OR_GREATER
[JsonSerializable(typeof(HubTests.EvilContext))]
internal partial class HubTestsJsonContext : JsonSerializerContext
{
}
#endif

#nullable enable
file static class DynamicSamplingContextExtensions
{
    public static DynamicSamplingContext CloneWithSampleRate(this DynamicSamplingContext? dsc, double sampleRate)
    {
        Assert.NotNull(dsc);
        var newDsc = dsc.Clone();
        newDsc.SetSampleRate(sampleRate);
        return newDsc;
    }

    public static DynamicSamplingContext CloneWithReplayId(this DynamicSamplingContext? dsc, IReplaySession replaySession)
    {
        Assert.NotNull(dsc);
        var newDsc = dsc.Clone();
        newDsc.SetReplayId(replaySession);
        return newDsc;
    }
}
