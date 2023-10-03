using Sentry.Internal.Http;
using Sentry.Internal.ScopeStack;
using static Sentry.Internal.Constants;

namespace Sentry.Tests;

[Collection(nameof(SentrySdkCollection))]
public class SentrySdkTests : IDisposable
{
    private readonly IDiagnosticLogger _logger;

    public SentrySdkTests(ITestOutputHelper testOutputHelper)
    {
        _logger = Substitute.ForPartsOf<TestOutputDiagnosticLogger>(testOutputHelper);
    }

    [Fact]
    public void IsEnabled_StartsOffFalse()
    {
        Assert.False(SentrySdk.IsEnabled);
    }

    [Fact]
    public void LastEventId_NoEventsCaptured_IsEmpty()
    {
        Assert.Equal(default, SentrySdk.LastEventId);
    }

    [Fact]
    public void LastEventId_SetToEventId()
    {
        using var _ = SentrySdk.Init(o =>
        {
            o.Dsn = ValidDsn;
            o.AutoSessionTracking = false;
            o.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            o.InitNativeSdks = false;
        });

        var id = SentrySdk.CaptureMessage("test");
        Assert.Equal(id, SentrySdk.LastEventId);
    }

    [Fact]
    public void LastEventId_Transaction_DoesNotReset()
    {
        using var _ = SentrySdk.Init(o =>
        {
            o.Dsn = ValidDsn;
            o.TracesSampleRate = 1.0;
            o.AutoSessionTracking = false;
            o.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            o.InitNativeSdks = false;
        });

        var id = SentrySdk.CaptureMessage("test");
        var transaction = SentrySdk.StartTransaction("test", "test");
        transaction.Finish();
        Assert.Equal(id, SentrySdk.LastEventId);
    }

    [Fact]
    public void Init_BrokenDsn_Throws()
    {
        _ = Assert.Throws<UriFormatException>(() => SentrySdk.Init("invalid stuff"));
    }

    [Fact]
    public void Init_ValidDsn_EnablesSdk()
    {
        using var _ = SentrySdk.Init(o =>
        {
            o.Dsn = ValidDsn;
            o.AutoSessionTracking = false;
            o.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            o.InitNativeSdks = false;
        });

        Assert.True(SentrySdk.IsEnabled);
    }

    [Fact]
    public void Init_ValidDsnWithSecret_EnablesSdk()
    {
        using var _ = SentrySdk.Init(o =>
        {
#pragma warning disable CS0618
            o.Dsn = ValidDsnWithSecret;
#pragma warning restore CS0618
            o.AutoSessionTracking = false;
            o.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            o.InitNativeSdks = false;
        });

        Assert.True(SentrySdk.IsEnabled);
    }

    [Fact]
    public void Init_ValidDsnEnvironmentVariable_EnablesSdk()
    {
        using var _ = SentrySdk.Init(o =>
        {
            o.FakeSettings().EnvironmentVariables[DsnEnvironmentVariable] = ValidDsn;
            o.AutoSessionTracking = false;
            o.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            o.InitNativeSdks = false;
        });

        Assert.True(SentrySdk.IsEnabled);
    }

    [Fact]
    public void Init_InvalidDsnEnvironmentVariable_Throws()
    {
        // If the variable was set, to non empty string but value is broken, better crash than silently disable
        var ex = Assert.Throws<ArgumentException>(() =>
            SentrySdk.Init(o => o.FakeSettings().EnvironmentVariables[DsnEnvironmentVariable] = InvalidDsn));

        Assert.Equal("Invalid DSN: A Project Id is required.", ex.Message);
    }

    [Fact]
    public void Init_DisableDsnEnvironmentVariable_DisablesSdk()
    {
        using var _ = SentrySdk.Init(o => o.FakeSettings().EnvironmentVariables[DsnEnvironmentVariable] = Constants.DisableSdkDsnValue);

        Assert.False(SentrySdk.IsEnabled);
    }

    [Fact]
    public void Init_EmptyDsn_DisabledSdk()
    {
        using (SentrySdk.Init(string.Empty))
        {
            Assert.False(SentrySdk.IsEnabled);
        }
    }

    [Fact]
    public void Init_EmptyDsn_LogsWarning()
    {
        var options = new SentryOptions
        {
            Dsn = Constants.DisableSdkDsnValue,
            DiagnosticLogger = _logger,
            Debug = true,
            InitNativeSdks = false
        };

        using (SentrySdk.Init(options))
        {
            _logger.Received(1).Log(SentryLevel.Warning, "Init called with an empty string as the DSN. Sentry SDK will be disabled.");
        }
    }

    [Fact]
    public void Init_DsnWithSecret_LogsWarning()
    {
        var options = new SentryOptions
        {
            DiagnosticLogger = _logger,
            Debug = true,
            Dsn = "https://d4d82fc1c2c4032a83f3a29aa3a3aff:ed0a8589a0bb4d4793ac4c70375f3d65@fake-sentry.io:65535/2147483647",
            AutoSessionTracking = false,
            BackgroundWorker = Substitute.For<IBackgroundWorker>(),
            InitNativeSdks = false
        };

        using (SentrySdk.Init(options))
        {
            _logger.Received(1).Log(SentryLevel.Warning, "The provided DSN that contains a secret key. This is not required and will be ignored.");
        }
    }

    [Fact]
    public void Init_EmptyDsnDisabledDiagnostics_DoesNotLogWarning()
    {
        var options = new SentryOptions
        {
            Dsn = Constants.DisableSdkDsnValue,
            DiagnosticLogger = _logger,
            Debug = false,
            InitNativeSdks = false,
        };

        using (SentrySdk.Init(options))
        {
            _logger.DidNotReceive().Log(Arg.Is(SentryLevel.Warning), Arg.Any<string>());
        }
    }

    [Fact]
    public void Init_MultipleCalls_ReplacesHubWithLatest()
    {
        var first = SentrySdk.Init(o =>
        {
            o.Dsn = ValidDsn;
            o.AutoSessionTracking = false;
            o.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            o.InitNativeSdks = false;
        });
        SentrySdk.AddBreadcrumb("test", "category");
        var called = false;
        SentrySdk.ConfigureScope(p =>
        {
            called = true;
            _ = Assert.Single(p.Breadcrumbs);
        });
        Assert.True(called);
        called = false;

        var second = SentrySdk.Init(o =>
        {
            o.Dsn = ValidDsn;
            o.AutoSessionTracking = false;
            o.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            o.InitNativeSdks = false;
        });
        SentrySdk.ConfigureScope(p =>
        {
            called = true;
            Assert.Empty(p.Breadcrumbs);
        });
        Assert.True(called);

        first.Dispose();
        second.Dispose();
    }

#if !__MOBILE__ && !CI_BUILD // Too slow on mobile. Too flaky in CI.
    [Theory]
    [InlineData(true)] // InitCacheFlushTimeout is more than enough time to process all messages
    [InlineData(false)] // InitCacheFlushTimeout is less time than needed to process all messages
    [InlineData(null)] // InitCacheFlushTimeout is not set
    public async Task Init_WithCache_BlocksUntilExistingCacheIsFlushed(bool? testDelayWorking)
    {
        // Note: We use a fake filesystem for this test, which uses only memory instead of disk.
        //       This keeps file IO access time out of the test.

        // Not too many, or this will be slow.  Not too few or this will be flaky.
        const int numEnvelopes = 5;

        // Set the delay for the transport here.  If the test becomes flaky, increase the timeout.
        var processingDelayPerEnvelope = TimeSpan.FromMilliseconds(200);

        // Arrange
        var fileSystem = new FakeFileSystem();
        using var cacheDirectory = new TempDirectory(fileSystem);
        var cachePath = cacheDirectory.Path;

        // Pre-populate cache
        var initialInnerTransport = Substitute.For<ITransport>();
        var initialTransport = CachingTransport.Create(
            initialInnerTransport,
            new SentryOptions
            {
                Debug = true,
                DiagnosticLogger = _logger,
                Dsn = ValidDsn,
                CacheDirectoryPath = cachePath,
                FileSystem = fileSystem,
                AutoSessionTracking = false,
                InitNativeSdks = false,
            },
            startWorker: false);
        await using (initialTransport)
        {
            for (var i = 0; i < numEnvelopes; i++)
            {
                using var envelope = Envelope.FromEvent(new SentryEvent());
                await initialTransport.SendEnvelopeAsync(envelope);
            }
        }

        _logger.Log(SentryLevel.Debug, "Done adding to cache directory.");

        var countCompleted = 0;
        var transport = Substitute.For<ITransport>();
        transport.SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var token = callInfo.Arg<CancellationToken>();
                await Task.Delay(processingDelayPerEnvelope, token);
                Interlocked.Increment(ref countCompleted);
                _logger.Log(SentryLevel.Debug, $"Sent envelope {countCompleted}.");
            });

        // Set the timeout for the desired result
        var initFlushTimeout = testDelayWorking switch
        {
            // more than enough
            true => TimeSpan.FromTicks(processingDelayPerEnvelope.Ticks * (numEnvelopes * 10)),
            // enough for at least one, but not all
            false => TimeSpan.FromTicks(processingDelayPerEnvelope.Ticks * (numEnvelopes - 1)),
            // none at all
            null => TimeSpan.Zero
        };

        // Act
        SentryOptions options = null;
        try
        {
            var stopwatch = Stopwatch.StartNew();

            using var _ = SentrySdk.Init(o =>
            {
                // Disable process exit flush to resolve "There is no currently active test." errors.
                o.DisableAppDomainProcessExitFlush();

                o.Dsn = ValidDsn;
                o.Debug = true;
                o.DiagnosticLogger = _logger;
                o.CacheDirectoryPath = cachePath;
                o.FileSystem = fileSystem;
                o.InitCacheFlushTimeout = initFlushTimeout;
                o.Transport = transport;
                o.AutoSessionTracking = false;
                o.InitNativeSdks = false;
                options = o;
            });

            stopwatch.Stop();

            // Assert
            switch (testDelayWorking)
            {
                case true:
                    // We waited long enough to have them all
                    Assert.Equal(numEnvelopes, countCompleted);

                    // But we should not have waited longer than we needed to
                    Assert.True(stopwatch.Elapsed < initFlushTimeout, "Should not have waited for the entire timeout!");
                    break;
                case false:
                    // We only waited long enough to have at least one, but not all of them
                    Assert.InRange(countCompleted, 1, numEnvelopes - 1);
                    break;
                case null:
                    // We shouldn't have any, as we didn't ask to flush the cache on init
                    Assert.Equal(0, countCompleted);
                    break;
            }
        }
        finally
        {
            // cleanup to avoid disposing/deleting the temp directory while the cache worker is still running
            var cachingTransport = (CachingTransport)options!.Transport;
            await cachingTransport!.StopWorkerAsync();
        }
    }
#endif

    [Fact]
    public void Disposable_MultipleCalls_NoOp()
    {
        var disposable = SentrySdk.Init(o =>
        {
            o.Dsn = ValidDsn;
            o.AutoSessionTracking = false;
            o.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            o.InitNativeSdks = false;
        });
        disposable.Dispose();
        disposable.Dispose();
        Assert.False(SentrySdk.IsEnabled);
    }

    [Fact]
    public void Dispose_DisposingFirst_DoesntAffectSecond()
    {
        var first = SentrySdk.Init(o =>
        {
            o.Dsn = ValidDsn;
            o.AutoSessionTracking = false;
            o.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            o.InitNativeSdks = false;
        });
        var second = SentrySdk.Init(o =>
        {
            o.Dsn = ValidDsn;
            o.AutoSessionTracking = false;
            o.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            o.InitNativeSdks = false;
        });
        SentrySdk.AddBreadcrumb("test", "category");
        first.Dispose();
        var called = false;
        SentrySdk.ConfigureScope(p =>
        {
            called = true;
            _ = Assert.Single(p.Breadcrumbs);
        });
        Assert.True(called);
        second.Dispose();
    }

    [Fact]
    public Task FlushAsync_NotInit_NoOp() => SentrySdk.FlushAsync();

    [Fact]
    public void PushScope_InstanceOf_DisabledClient()
    {
        Assert.Same(DisabledHub.Instance, SentrySdk.PushScope());
    }

    [Fact]
    public void PushScope_NullArgument_NoOp()
    {
        var scopeGuard = SentrySdk.PushScope(null as object);
        Assert.False(SentrySdk.IsEnabled);
        scopeGuard.Dispose();
    }

    [Fact]
    public void PushScope_Parameterless_NoOp()
    {
        var scopeGuard = SentrySdk.PushScope();
        Assert.False(SentrySdk.IsEnabled);
        scopeGuard.Dispose();
    }

    [Fact]
    public void PushScope_MultiCallState_SameDisposableInstance()
    {
        var state = new object();
        Assert.Same(SentrySdk.PushScope(state), SentrySdk.PushScope(state));
    }

    [Fact]
    public void PushScope_MultiCallParameterless_SameDisposableInstance() => Assert.Same(SentrySdk.PushScope(), SentrySdk.PushScope());

    [Fact]
    public void AddBreadcrumb_NoClock_NoOp() => SentrySdk.AddBreadcrumb(message: null!);

    [Fact]
    public void AddBreadcrumb_WithClock_NoOp() => SentrySdk.AddBreadcrumb(clock: null, null!);

    [Fact]
    public void ConfigureScope_Sync_CallbackNeverInvoked()
    {
        var invoked = false;
        SentrySdk.ConfigureScope(_ => invoked = true);
        Assert.False(invoked);
    }

    [Fact]
    public async Task ConfigureScope_OnTask_PropagatedToCaller()
    {
        const string expected = "test";
        using var _ = SentrySdk.Init(o =>
        {
            o.Dsn = ValidDsn;
            o.AutoSessionTracking = false;
            o.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            o.InitNativeSdks = false;
        });

        await ModifyScope();

        string actual = null;
        SentrySdk.ConfigureScope(s => actual = s.Breadcrumbs.First().Message);

        Assert.Equal(expected, actual);

        async Task ModifyScope()
        {
            await Task.Yield();
            SentrySdk.AddBreadcrumb(expected);
        }
    }

    [Obsolete]
    [Fact]
    public void WithScope_DisabledSdk_CallbackNeverInvoked()
    {
        var invoked = false;

        // Note: Specifying void in the lambda ensures we are testing WithScope, rather than WithScope<T>.
        SentrySdk.WithScope(void (_) => invoked = true);
        Assert.False(invoked);
    }

    [Obsolete]
    [Fact]
    public void WithScopeT_DisabledSdk_CallbackNeverInvoked()
    {
        var invoked = SentrySdk.WithScope(_ => true);
        Assert.False(invoked);
    }

    [Obsolete]
    [Fact]
    public async Task WithScopeAsync_DisabledSdk_CallbackNeverInvoked()
    {
        var invoked = false;
        await SentrySdk.WithScopeAsync(_ =>
        {
            invoked = true;
            return Task.CompletedTask;
        });
        Assert.False(invoked);
    }

    [Obsolete]
    [Fact]
    public async Task WithScopeAsyncT_DisabledSdk_CallbackNeverInvoked()
    {
        var invoked = await SentrySdk.WithScopeAsync(_ => Task.FromResult(true));
        Assert.False(invoked);
    }

    [Obsolete]
    [Fact]
    public void WithScope_InvokedWithNewScope()
    {
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            IsGlobalModeEnabled = false,
            AutoSessionTracking = false,
            BackgroundWorker = Substitute.For<IBackgroundWorker>(),
            InitNativeSdks = false
        };

        using var _ = SentrySdk.Init(options);

        Scope expected = null;
        SentrySdk.ConfigureScope(s => expected = s);

        Scope actual = null;
        // Note: Specifying void in the lambda ensures we are testing WithScope, rather than WithScope<T>.
        SentrySdk.WithScope(void (s) => actual = s);

        Assert.NotNull(actual);
        Assert.NotSame(expected, actual);
        SentrySdk.ConfigureScope(s => Assert.Same(expected, s));
    }

    [Obsolete]
    [Fact]
    public void WithScopeT_InvokedWithNewScope()
    {
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            IsGlobalModeEnabled = false,
            AutoSessionTracking = false,
            BackgroundWorker = Substitute.For<IBackgroundWorker>(),
            InitNativeSdks = false
        };

        using var _ = SentrySdk.Init(options);

        Scope expected = null;
        SentrySdk.ConfigureScope(s => expected = s);

        var actual = SentrySdk.WithScope(s => s);

        Assert.NotNull(actual);
        Assert.NotSame(expected, actual);
        SentrySdk.ConfigureScope(s => Assert.Same(expected, s));
    }

    [Obsolete]
    [Fact]
    public async Task WithScopeAsync_InvokedWithNewScope()
    {
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            IsGlobalModeEnabled = false,
            AutoSessionTracking = false,
            BackgroundWorker = Substitute.For<IBackgroundWorker>(),
            InitNativeSdks = false
        };

        using var _ = SentrySdk.Init(options);

        Scope expected = null;
        SentrySdk.ConfigureScope(s => expected = s);

        Scope actual = null;
        await SentrySdk.WithScopeAsync(s =>
        {
            actual = s;
            return Task.CompletedTask;
        });

        Assert.NotNull(actual);
        Assert.NotSame(expected, actual);
        SentrySdk.ConfigureScope(s => Assert.Same(expected, s));
    }

    [Obsolete]
    [Fact]
    public async Task WithScopeAsyncT_InvokedWithNewScope()
    {
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            IsGlobalModeEnabled = false,
            AutoSessionTracking = false,
            BackgroundWorker = Substitute.For<IBackgroundWorker>(),
            InitNativeSdks = false
        };

        using var _ = SentrySdk.Init(options);

        Scope expected = null;
        SentrySdk.ConfigureScope(s => expected = s);

        var actual = await SentrySdk.WithScopeAsync(Task.FromResult);

        Assert.NotNull(actual);
        Assert.NotSame(expected, actual);
        SentrySdk.ConfigureScope(s => Assert.Same(expected, s));
    }

    [Fact]
    public void CaptureEvent_WithConfiguredScope_ScopeAppliesToEvent()
    {
        const string expected = "test";
        var worker = Substitute.For<IBackgroundWorker>();

        using var _ = SentrySdk.Init(o =>
        {
            o.Dsn = ValidDsn;
            o.BackgroundWorker = worker;
            o.AutoSessionTracking = false;
            o.InitNativeSdks = false;
        });
        SentrySdk.CaptureEvent(new SentryEvent(), s => s.AddBreadcrumb(expected));

        worker.EnqueueEnvelope(
            Arg.Is<Envelope>(e => e.Items
                .Select(i => i.Payload)
                .OfType<JsonSerializable>()
                .Select(i => i.Source)
                .OfType<SentryEvent>()
                .Single()
                .Breadcrumbs
                .Single()
                .Message == expected));
    }

    [Fact]
    public void CaptureEvent_WithConfiguredScope_ScopeOnlyAppliesOnlyOnce()
    {
        using var _ = SentrySdk.Init(o =>
        {
            o.Dsn = ValidDsn;
            o.AutoSessionTracking = false;
            o.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            o.InitNativeSdks = false;
        });

        var callbackCounter = 0;
        SentrySdk.CaptureEvent(new SentryEvent(), _ => callbackCounter++);
        SentrySdk.CaptureEvent(new SentryEvent());

        Assert.Equal(1, callbackCounter);
    }

    [Fact]
    public void CaptureEvent_WithConfiguredScopeNull_LogsError()
    {
        var logger = new InMemoryDiagnosticLogger();

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = logger,
            Debug = true,
            AutoSessionTracking = false,
            BackgroundWorker = Substitute.For<IBackgroundWorker>(),
            InitNativeSdks = false
        };

        using var _ = SentrySdk.Init(options);
        SentrySdk.CaptureEvent(new SentryEvent(), (null as Action<Scope>)!);

        logger.Entries.Any(e =>
                e.Level == SentryLevel.Error &&
                e.Message == "Failure to capture event: {0}")
            .Should()
            .BeTrue();
    }

    [Fact]
    public void CaptureEvent_WithConfiguredScope_ScopeCallbackGetsInvoked()
    {
        using var _ = SentrySdk.Init(o =>
        {
            o.Dsn = ValidDsn;
            o.AutoSessionTracking = false;
            o.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            o.InitNativeSdks = false;
        });

        var scopeCallbackWasInvoked = false;
        SentrySdk.CaptureEvent(new SentryEvent(), _ => scopeCallbackWasInvoked = true);

        Assert.True(scopeCallbackWasInvoked);
    }

    [Fact]
    public void CaptureException_WithConfiguredScope_ScopeCallbackGetsInvoked()
    {
        using var _ = SentrySdk.Init(o =>
        {
            o.Dsn = ValidDsn;
            o.AutoSessionTracking = false;
            o.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            o.InitNativeSdks = false;
        });

        var scopeCallbackWasInvoked = false;
        SentrySdk.CaptureException(new Exception(), _ => scopeCallbackWasInvoked = true);

        Assert.True(scopeCallbackWasInvoked);
    }

    [Fact]
    public void CaptureMessage_WithConfiguredScope_ScopeCallbackGetsInvoked()
    {
        using var _ = SentrySdk.Init(o =>
        {
            o.Dsn = ValidDsn;
            o.AutoSessionTracking = false;
            o.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            o.InitNativeSdks = false;
        });

        var scopeCallbackWasInvoked = false;
        SentrySdk.CaptureMessage("TestMessage", _ => scopeCallbackWasInvoked = true);

        Assert.True(scopeCallbackWasInvoked);
    }

    [Fact]
    public async Task ConfigureScope_Async_CallbackNeverInvoked()
    {
        var invoked = false;
        await SentrySdk.ConfigureScopeAsync(_ =>
        {
            invoked = true;
            return Task.CompletedTask;
        });
        Assert.False(invoked);
    }

    [Fact]
    public void CaptureEvent_Instance_NoOp() => SentrySdk.CaptureEvent(new SentryEvent());

    [Fact]
    public void CaptureException_Instance_NoOp() => SentrySdk.CaptureException(new Exception());

    [Fact]
    public void CaptureMessage_Message_NoOp() => SentrySdk.CaptureMessage("message");

    [Fact]
    public void CaptureMessage_MessageLevel_NoOp() => SentrySdk.CaptureMessage("message", SentryLevel.Debug);

    [Fact]
    public void CaptureMessage_SdkInitialized_IncludesScope()
    {
        var worker = Substitute.For<IBackgroundWorker>();
        using var _ = SentrySdk.Init(o =>
        {
            o.Dsn = ValidDsn;
            o.BackgroundWorker = worker;
            o.AutoSessionTracking = false;
            o.InitNativeSdks = false;
        });

        const string expected = "test";
        SentrySdk.AddBreadcrumb(expected);
        SentrySdk.CaptureMessage("message");

        worker.Received(1).EnqueueEnvelope(
            Arg.Is<Envelope>(e => e.Items
                .Select(i => i.Payload)
                .OfType<JsonSerializable>()
                .Select(i => i.Source)
                .OfType<SentryEvent>()
                .Single()
                .Breadcrumbs
                .Single()
                .Message == expected));
    }

    [Fact]
    public void Implements_Client()
    {
        var clientMembers = typeof(ISentryClient)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.ToString())
            .ToArray();

        var sentrySdkMembers = typeof(SentrySdk)
            .GetMembers(BindingFlags.Public | BindingFlags.Static)
            .Select(m => m.ToString())
            .ToArray();

        sentrySdkMembers.Should().Contain(clientMembers);
    }

    [Fact]
    public void Implements_ClientExtensions()
    {
        var clientExtensions = typeof(SentryClientExtensions).GetMembers(BindingFlags.Public | BindingFlags.Static)
            // Remove the extension argument: Method(this ISentryClient client, ...
            .Select(m => m.ToString()!
                .Replace($"({typeof(ISentryClient).FullName}", "(")
                .Replace("(, ", "("));
        var sentrySdk = typeof(SentrySdk).GetMembers(BindingFlags.Public | BindingFlags.Static);

        Assert.Empty(clientExtensions.Except(sentrySdk.Select(m => m.ToString())));
    }

    [Fact]
    public void Implements_ScopeManagement()
    {
        var scopeManagement = typeof(ISentryScopeManager).GetMembers(BindingFlags.Public | BindingFlags.Instance);
        var sentrySdk = typeof(SentrySdk).GetMembers(BindingFlags.Public | BindingFlags.Static);

        Assert.Empty(scopeManagement.Select(m => m.ToString()).Except(sentrySdk.Select(m => m.ToString())));
    }

    // Issue: https://github.com/getsentry/sentry-dotnet/issues/123
    [Fact]
    public void InitHub_NoDsn_DisposeDoesNotThrow()
    {
        var sut = SentrySdk.InitHub(new SentryOptions(){Dsn = Constants.DisableSdkDsnValue}) as IDisposable;
        sut?.Dispose();
    }

    [Fact]
    public async Task InitHub_NoDsn_FlushAsyncDoesNotThrow()
    {
        var sut = SentrySdk.InitHub(new SentryOptions(){Dsn = Constants.DisableSdkDsnValue});
        await sut.FlushAsync();
    }

    [Fact]
    public void InitHub_GlobalModeOff_AsyncLocalContainer()
    {
        // Arrange
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            IsGlobalModeEnabled = false,
            AutoSessionTracking = false,
            BackgroundWorker = Substitute.For<IBackgroundWorker>(),
            InitNativeSdks = false
        };

        // Act
        var sut = SentrySdk.InitHub(options);

        // Assert
        var hub = (Hub)sut;
        hub.ScopeManager.ScopeStackContainer.Should().BeOfType<AsyncLocalScopeStackContainer>();
    }

    [Fact]
    public void InitHub_GlobalModeOn_GlobalContainer()
    {
        // Arrange
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            IsGlobalModeEnabled = true,
            AutoSessionTracking = false,
            BackgroundWorker = Substitute.For<IBackgroundWorker>(),
            InitNativeSdks = false
        };

        // Act
        var sut = SentrySdk.InitHub(options);

        // Assert
        var hub = (Hub)sut;
        hub.ScopeManager.ScopeStackContainer.Should().BeOfType<GlobalScopeStackContainer>();
    }

#if !__MOBILE__ // On mobile, we'll always have logs from the Android/iOS SDK, so we can't reliably test for silence.
    [Fact]
    public void InitHub_GlobalModeOn_NoWarningOrErrorLogged()
    {
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

        SentrySdk.InitHub(options);

        _logger.DidNotReceive().Log(
            SentryLevel.Warning,
            Arg.Any<string>(),
            Arg.Any<Exception>(),
            Arg.Any<object[]>());

        _logger.DidNotReceive().Log(
            SentryLevel.Error,
            Arg.Any<string>(),
            Arg.Any<Exception>(),
            Arg.Any<object[]>());
    }

    [Fact]
    public void InitHub_GlobalModeOff_NoWarningOrErrorLogged()
    {
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            IsGlobalModeEnabled = false,
            Debug = true,
            AutoSessionTracking = false,
            BackgroundWorker = Substitute.For<IBackgroundWorker>(),
            InitNativeSdks = false
        };

        SentrySdk.InitHub(options);

        _logger.DidNotReceive().Log(
            SentryLevel.Warning,
            Arg.Any<string>(),
            Arg.Any<Exception>(),
            Arg.Any<object[]>());

        _logger.DidNotReceive().Log(
            SentryLevel.Error,
            Arg.Any<string>(),
            Arg.Any<Exception>(),
            Arg.Any<object[]>());
    }
#endif

    [Fact]
    public void InitHub_DebugEnabled_DebugLogsLogged()
    {
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

        SentrySdk.InitHub(options);

        _logger.Received().Log(
            SentryLevel.Debug,
            Arg.Any<string>(),
            Arg.Any<Exception>(),
            Arg.Any<object[]>());
    }

    public void Dispose()
    {
        SentrySdk.Close();
    }
}
