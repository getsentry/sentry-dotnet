using System.IO.Compression;
using System.Net.Http;
using Sentry.Internal.Http;
using Sentry.Testing;

namespace Sentry.Tests;

[UsesVerify]
public class HubTests
{
    private readonly ITestOutputHelper _output;

    public HubTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void PushScope_BreadcrumbWithinScope_NotVisibleOutside()
    {
        // Arrange
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            BackgroundWorker = new FakeBackgroundWorker(),
            IsGlobalModeEnabled = false
        };

        var hub = new Hub(options);

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
        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            BackgroundWorker = new FakeBackgroundWorker()
        });

        // Act & assert
        hub.ConfigureScope(s => Assert.False(s.Locked));
        using (hub.PushAndLockScope())
        {
            hub.ConfigureScope(s => Assert.True(s.Locked));
        }

        hub.ConfigureScope(s => Assert.False(s.Locked));
    }

    [Fact]
    public void CaptureMessage_FailedQueue_LastEventIdSetToEmpty()
    {
        // Arrange
        var worker = Substitute.For<IBackgroundWorker>();
        worker.EnqueueEnvelope(Arg.Any<Envelope>()).Returns(false);

        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            BackgroundWorker = worker
        });

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
        var client = Substitute.For<ISentryClient>();

        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1
        }, client);

        var exception = new Exception("error");

        var transaction = hub.StartTransaction("foo", "bar");
        transaction.Finish(exception);

        // Act
        hub.CaptureException(exception);

        // Assert
        client.Received(1).CaptureEvent(
            Arg.Is<SentryEvent>(evt =>
                evt.Contexts.Trace.TraceId == transaction.TraceId &&
                evt.Contexts.Trace.SpanId == transaction.SpanId),
            Arg.Any<Scope>());
    }

    [Fact]
    public void CaptureException_ActiveSpanExistsOnScope_EventIsLinkedToSpan()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();

        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1
        }, client);

        var exception = new Exception("error");

        var transaction = hub.StartTransaction("foo", "bar");

        hub.ConfigureScope(scope => scope.Transaction = transaction);

        // Act
        hub.CaptureException(exception);

        // Assert
        client.Received(1).CaptureEvent(
            Arg.Is<SentryEvent>(evt =>
                evt.Contexts.Trace.TraceId == transaction.TraceId &&
                evt.Contexts.Trace.SpanId == transaction.SpanId),
            Arg.Any<Scope>());
    }

    [Fact]
    public void CaptureException_ActiveSpanExistsOnScopeButIsSampledOut_EventIsNotLinkedToSpan()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();

        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 0
        }, client);

        var exception = new Exception("error");

        var transaction = hub.StartTransaction("foo", "bar");

        hub.ConfigureScope(scope => scope.Transaction = transaction);

        // Act
        hub.CaptureException(exception);

        // Assert
        client.Received(1).CaptureEvent(
            Arg.Is<SentryEvent>(evt =>
                evt.Contexts.Trace.TraceId == default &&
                evt.Contexts.Trace.SpanId == default),
            Arg.Any<Scope>());
    }

    [Fact]
    public void CaptureException_NoActiveSpanAndNoSpanBoundToSameException_EventIsNotLinkedToSpan()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();

        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1
        }, client);

        // Act
        hub.CaptureException(new Exception("error"));

        // Assert
        client.Received(1).CaptureEvent(
            Arg.Is<SentryEvent>(evt =>
                evt.Contexts.Trace.TraceId == default &&
                evt.Contexts.Trace.SpanId == default),
            Arg.Any<Scope>());
    }

    [Fact]
    public void CaptureEvent_SessionActive_NoExceptionDoesNotReportError()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();

        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            Release = "release"
        }, client);

        hub.StartSession();

        // Act
        hub.CaptureEvent(new SentryEvent());
        hub.EndSession();

        // Assert
        client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => s.ErrorCount == 0));
    }

    [Fact]
    public void CaptureEvent_ExceptionWithOpenSpan_SpanLinkedToEventContext()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();

        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1
        }, client);
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

    private class EvilContext
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
        var tcs = new TaskCompletionSource<bool>();
        var expectedMessage = Guid.NewGuid().ToString();

        var requests = new List<string>();
        async Task VerifyAsync(HttpRequestMessage message)
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

        var fileSystem = new FakeFileSystem();
        using var tempDirectory = offlineCaching ? new TempDirectory(fileSystem) : null;

        var logger = Substitute.ForPartsOf<TestOutputDiagnosticLogger>(_output);

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            // To go through a round trip serialization of cached envelope
            CacheDirectoryPath = tempDirectory?.Path,
            FileSystem = fileSystem,
            // So we don't need to deal with gzip'ed payload
            RequestBodyCompressionLevel = CompressionLevel.NoCompression,
            CreateHttpClientHandler = () => new CallbackHttpClientHandler(VerifyAsync),
            // Not to send some session envelope
            AutoSessionTracking = false,
            Debug = true,
            DiagnosticLogger = logger
        };

        // Disable process exit flush to resolve "There is no currently active test." errors.
        options.DisableAppDomainProcessExitFlush();

        try
        {
            using var hub = new Hub(options);

            var expectedContextKey = Guid.NewGuid().ToString();
            var evt = new SentryEvent
            {
                Contexts = {[expectedContextKey] = new EvilContext()},
                Message = new()
                {
                    Formatted = expectedMessage
                }
            };

            hub.CaptureEvent(evt);
            await hub.FlushAsync(options.ShutdownTimeout);

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
    public void CaptureEvent_SessionActive_ExceptionReportsError()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();

        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            Release = "release"
        }, client);

        hub.StartSession();

        // Act
        hub.CaptureEvent(new SentryEvent(new Exception()));
        hub.EndSession();

        // Assert
        client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => s.ErrorCount == 1));
    }

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
        var client = new SentryClient(options, worker);
        var hub = new Hub(options, client);

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
    [Trait("Category", "Verify")]
    public async Task CaptureEvent_ActiveTransaction_UnhandledExceptionTransactionEndedAsCrashed()
    {
        // Arrange
        var worker = new FakeBackgroundWorker();

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            Release = "release",
            TracesSampleRate = 1.0
        };
        var client = new SentryClient(options, worker);
        var hub = new Hub(options, client);

        var transaction = hub.StartTransaction("my transaction", "my operation");
        hub.ConfigureScope(scope => scope.Transaction = transaction);
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

        await Verify(worker.Envelopes)
            .IgnoreStandardSentryMembers()
            .IgnoreMember("Stacktrace")
            .IgnoreMember<SentryThread>(_ => _.Name);
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
        var client = new SentryClient(options, worker);
        var hub = new Hub(options, client);

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
        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn
        });

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
        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1
        });

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
        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 0
        });

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
        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampler = _ => 0,
            TracesSampleRate = 1
        });

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
        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1
        });

        // Act
        var transaction = hub.StartTransaction("name", "operation");

        // Assert
        transaction.IsSampled.Should().BeTrue();
    }

    [Fact]
    public void StartTransaction_StaticSampling_SampledOut()
    {
        // Arrange
        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 0
        });

        // Act
        var transaction = hub.StartTransaction("name", "operation");

        // Assert
        transaction.IsSampled.Should().BeFalse();
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
        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampler = ctx => ctx.TransactionContext.Name == "foo" ? 1 : 0
        });

        // Act
        var transaction = hub.StartTransaction("foo", "op");

        // Assert
        transaction.IsSampled.Should().BeTrue();
    }

    [Fact]
    public void StartTransaction_TracesSampler_SampledOut()
    {
        // Arrange
        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampler = ctx => ctx.TransactionContext.Name == "foo" ? 1 : 0
        });

        // Act
        var transaction = hub.StartTransaction("bar", "op");

        // Assert
        transaction.IsSampled.Should().BeFalse();
    }

    [Fact]
    public void StartTransaction_TracesSampler_WithCustomContext_SampledIn()
    {
        // Arrange
        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampler = ctx => ctx.CustomSamplingContext.GetValueOrDefault("xxx") as string == "zzz" ? 1 : 0
        });

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
        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampler = ctx => ctx.CustomSamplingContext.GetValueOrDefault("xxx") as string == "zzz" ? 1 : 0
        });

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
        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampler = _ => null,
            TracesSampleRate = 1
        });

        // Act
        var transaction = hub.StartTransaction("foo", "bar");

        // Assert
        transaction.IsSampled.Should().BeTrue();
    }

    [Fact]
    public void StartTransaction_TracesSampler_FallbackToStatic_SampledOut()
    {
        // Arrange
        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampler = _ => null,
            TracesSampleRate = 0
        });

        // Act
        var transaction = hub.StartTransaction("foo", "bar");

        // Assert
        transaction.IsSampled.Should().BeFalse();
    }

    [Fact]
    public void GetTraceHeader_ReturnsHeaderForActiveSpan()
    {
        // Arrange
        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn
        });

        var transaction = hub.StartTransaction("foo", "bar");

        // Act
        hub.WithScope(scope =>
        {
            scope.Transaction = transaction;

            var header = hub.GetTraceHeader();

            // Assert
            header.Should().NotBeNull();
            header?.SpanId.Should().Be(transaction.SpanId);
            header?.TraceId.Should().Be(transaction.TraceId);
            header?.IsSampled.Should().Be(transaction.IsSampled);
        });
    }

    [Fact]
    public void CaptureTransaction_AfterTransactionFinishes_ResetsTransactionOnScope()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();

        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn
        }, client);

        var transaction = hub.StartTransaction("foo", "bar");

        hub.WithScope(scope => scope.Transaction = transaction);

        // Act
        transaction.Finish();

        // Assert
        hub.WithScope(scope => scope.Transaction.Should().BeNull());
    }

    [Fact]
    public void Dispose_IsEnabled_SetToFalse()
    {
        // Arrange
        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn
        });

        hub.IsEnabled.Should().BeTrue();

        // Act
        hub.Dispose();

        // Assert
        hub.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Dispose_CalledSecondTime_ClientDisposedOnce()
    {
        var client = Substitute.For<ISentryClient, IDisposable>();
        var options = new SentryOptions
        {
            Dsn = ValidDsn
        };
        var hub = new Hub(options, client);

        // Act
        hub.Dispose();
        hub.Dispose();

        // Assert
        client.Received(1).FlushAsync(options.ShutdownTimeout);
    }

    [Fact]
    public void StartSession_CapturesUpdate()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();

        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            Release = "release"
        }, client);

        // Act
        hub.StartSession();

        // Assert
        client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => s.IsInitial));
    }

    [Fact]
    public void StartSession_GlobalSessionManager_ExceptionOnCrashLastRun_CapturesUpdate()
    {
        // Arrange
        var sessionUpdate = new GlobalSessionManagerTests().TryRecoverPersistedSessionWithExceptionOnLastRun();
        var newSession = new SessionUpdate(Substitute.For<ISession>(), false, default, 0, null);

        var client = Substitute.For<ISentryClient>();
        var sessionManager = Substitute.For<ISessionManager>();
        sessionManager.TryRecoverPersistedSession().Returns(sessionUpdate);
        sessionManager.StartSession().Returns(newSession);

        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            Release = "release"
        }, client, sessionManager);

        // Act
        hub.StartSession();

        // Assert
        client.Received().CaptureSession(Arg.Is(sessionUpdate));
        client.Received().CaptureSession(Arg.Is(newSession));
    }

    [Fact]
    public void EndSession_CapturesUpdate()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();

        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            Release = "release"
        }, client);

        hub.StartSession();

        // Act
        hub.EndSession();

        // Assert
        client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => !s.IsInitial));
    }

    [Fact]
    public void Ctor_AutoSessionTrackingEnabled_StartsSession()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();

        // Act
        _ = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            AutoSessionTracking = true,
            Release = "release"
        }, client);

        // Assert
        client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => s.IsInitial));
    }

    [Fact]
    public void Ctor_GlobalModeTrue_DoesNotPushScope()
    {
        // Arrange
        var scopeManager = Substitute.For<IInternalScopeManager>();

        // Act
        _ = new Hub(new SentryOptions
        {
            IsGlobalModeEnabled = true,
            Dsn = ValidDsn,
        }, scopeManager: scopeManager);

        // Assert
        scopeManager.DidNotReceiveWithAnyArgs().PushScope();
    }

    [Fact]
    public void Ctor_GlobalModeFalse_DoesPushScope()
    {
        // Arrange
        var scopeManager = Substitute.For<IInternalScopeManager>();

        // Act
        var options = new SentryOptions
        {
            IsGlobalModeEnabled = false,
            Dsn = ValidDsn,
        };

        _ = new Hub(options, scopeManager: scopeManager);

        // Assert
        scopeManager.Received(1).PushScope();
    }

    [Fact]
    public void ResumeSession_WithinAutoTrackingInterval_ContinuesSameSession()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();

        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            AutoSessionTrackingInterval = TimeSpan.FromSeconds(9999)
        }, client);

        hub.StartSession();
        hub.PauseSession();

        // Act
        hub.ResumeSession();

        // Assert
        client.DidNotReceive().CaptureSession(Arg.Is<SessionUpdate>(s => s.EndStatus != null));
    }

    [Fact]
    public void ResumeSession_BeyondAutoTrackingInterval_EndsPreviousSessionAndStartsANewOne()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();
        var clock = Substitute.For<ISystemClock>();

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            AutoSessionTrackingInterval = TimeSpan.FromMilliseconds(10),
            Release = "release"
        };

        var hub = new Hub(
            options,
            client,
            clock: clock,
            sessionManager: new GlobalSessionManager(options, clock));

        clock.GetUtcNow().Returns(DateTimeOffset.Now);

        hub.StartSession();
        hub.PauseSession();

        clock.GetUtcNow().Returns(DateTimeOffset.Now + TimeSpan.FromDays(1));

        // Act
        hub.ResumeSession();

        // Assert
        client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => s.EndStatus == SessionEndStatus.Exited));
        client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => s.IsInitial));
    }

    [Fact]
    public void ResumeSession_NoActiveSession_DoesNothing()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();
        var clock = Substitute.For<ISystemClock>();

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            AutoSessionTrackingInterval = TimeSpan.FromMilliseconds(10)
        };

        var hub = new Hub(
            options,
            clock: clock,
            sessionManager: new GlobalSessionManager(options, clock));

        clock.GetUtcNow().Returns(DateTimeOffset.Now);

        hub.PauseSession();

        clock.GetUtcNow().Returns(DateTimeOffset.Now + TimeSpan.FromDays(1));

        // Act
        hub.ResumeSession();

        // Assert
        client.DidNotReceive().CaptureSession(Arg.Any<SessionUpdate>());
    }

    [Fact]
    public void ResumeSession_NoPausedSession_DoesNothing()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();
        var clock = Substitute.For<ISystemClock>();

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            AutoSessionTrackingInterval = TimeSpan.FromMilliseconds(10)
        };

        var hub = new Hub(
            options,
            clock: clock,
            sessionManager: new GlobalSessionManager(options, clock));

        clock.GetUtcNow().Returns(DateTimeOffset.Now);

        hub.StartSession();

        clock.GetUtcNow().Returns(DateTimeOffset.Now + TimeSpan.FromDays(1));

        // Act
        hub.ResumeSession();

        // Assert
        client.DidNotReceive().CaptureSession(Arg.Is<SessionUpdate>(s => s.EndStatus != null));
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
        var client = Substitute.For<ISentryClient>();

        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1
        }, client);
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
}
