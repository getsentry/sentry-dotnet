using System.Reflection;
using Sentry.Internal.Http;
using Sentry.Internal.ScopeStack;
using Sentry.Testing;
using static Sentry.DsnSamples;
using static Sentry.Internal.Constants;

namespace Sentry.Tests;

[Collection(nameof(SentrySdkCollection))]
public class SentrySdkTests : SentrySdkTestFixture
{
    private readonly IDiagnosticLogger _logger;

    public SentrySdkTests(ITestOutputHelper testOutputHelper)
    {
        _logger = new TestOutputDiagnosticLogger(testOutputHelper);
    }

    [Fact]
    public void IsEnabled_StartsOfFalse()
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
        EnvironmentVariableGuard.WithVariable(
            DsnEnvironmentVariable,
            ValidDsnWithSecret,
            () =>
            {
                using (SentrySdk.Init())
                {
                    var id = SentrySdk.CaptureMessage("test");
                    Assert.Equal(id, SentrySdk.LastEventId);
                }
            });
    }

    [Fact]
    public void LastEventId_Transaction_DoesNotReset()
    {
        EnvironmentVariableGuard.WithVariable(
            DsnEnvironmentVariable,
            ValidDsnWithSecret,
            () =>
            {
                using (SentrySdk.Init(o => o.TracesSampleRate = 1.0))
                {
                    var id = SentrySdk.CaptureMessage("test");
                    var transaction = SentrySdk.StartTransaction("test", "test");
                    transaction.Finish();
                    Assert.Equal(id, SentrySdk.LastEventId);
                }
            });
    }

    [Fact]
    public void Init_BrokenDsn_Throws()
    {
        _ = Assert.Throws<UriFormatException>(() => SentrySdk.Init("invalid stuff"));
    }

    [Fact]
    public void Init_ValidDsnWithSecret_EnablesSdk()
    {
        using (SentrySdk.Init(ValidDsnWithSecret))
        {
            Assert.True(SentrySdk.IsEnabled);
        }
    }

    [Fact]
    public void Init_ValidDsnWithoutSecret_EnablesSdk()
    {
        using (SentrySdk.Init(ValidDsnWithoutSecret))
        {
            Assert.True(SentrySdk.IsEnabled);
        }
    }

    [Fact]
    public void Init_CallbackWithoutDsn_ValidDsnEnvironmentVariable_LocatesDsnEnvironmentVariable()
    {
        EnvironmentVariableGuard.WithVariable(
            DsnEnvironmentVariable,
            ValidDsnWithSecret,
            () =>
            {
                using (SentrySdk.Init(_ => { }))
                {
                    Assert.True(SentrySdk.IsEnabled);
                }
            });
    }

    [Fact]
    public void Init_CallbackWithoutDsn_InvalidDsnEnvironmentVariable_Throws()
    {
        EnvironmentVariableGuard.WithVariable(
            DsnEnvironmentVariable,
            InvalidDsn,
            () =>
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    using (SentrySdk.Init(_ => { }))
                    {
                    }
                });
            });
    }

    [Fact]
    public void Init_ValidDsnEnvironmentVariable_EnablesSdk()
    {
        EnvironmentVariableGuard.WithVariable(
            DsnEnvironmentVariable,
            ValidDsnWithSecret,
            () =>
            {
                using (SentrySdk.Init())
                {
                    Assert.True(SentrySdk.IsEnabled);
                }
            });
    }

    [Fact]
    public void Init_InvalidDsnEnvironmentVariable_Throws()
    {
        EnvironmentVariableGuard.WithVariable(
            DsnEnvironmentVariable,
            // If the variable was set, to non empty string but value is broken, better crash than silently disable
            InvalidDsn,
            () =>
            {
                var ex = Assert.Throws<ArgumentException>(() => SentrySdk.Init());
                Assert.Equal("Invalid DSN: A Project Id is required.", ex.Message);
            });
    }

    [Fact]
    public void Init_DisableDsnEnvironmentVariable_DisablesSdk()
    {
        EnvironmentVariableGuard.WithVariable(
            DsnEnvironmentVariable,
            Constants.DisableSdkDsnValue,
            () =>
            {
                using (SentrySdk.Init())
                {
                    Assert.False(SentrySdk.IsEnabled);
                }
            });
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
        var logger = Substitute.For<IDiagnosticLogger>();
        _ = logger.IsEnabled(SentryLevel.Warning).Returns(true);

        var options = new SentryOptions
        {
            DiagnosticLogger = logger,
            Debug = true
        };

        using (SentrySdk.Init(options))
        {
            logger.Received(1).Log(SentryLevel.Warning, "Init was called but no DSN was provided nor located. Sentry SDK will be disabled.");
        }
    }

    [Fact]
    public void Init_EmptyDsnDisabledDiagnostics_DoesNotLogWarning()
    {
        var logger = Substitute.For<IDiagnosticLogger>();
        _ = logger.IsEnabled(SentryLevel.Warning).Returns(true);

        var options = new SentryOptions
        {
            DiagnosticLogger = logger,
            Debug = false,
        };

        using (SentrySdk.Init(options))
        {
            logger.DidNotReceive().Log(Arg.Any<SentryLevel>(), Arg.Any<string>());
        }
    }

    [Fact]
    public void Init_MultipleCalls_ReplacesHubWithLatest()
    {
        var first = SentrySdk.Init(ValidDsnWithSecret);
        SentrySdk.AddBreadcrumb("test", "category");
        var called = false;
        SentrySdk.ConfigureScope(p =>
        {
            called = true;
            _ = Assert.Single(p.Breadcrumbs);
        });
        Assert.True(called);
        called = false;

        var second = SentrySdk.Init(ValidDsnWithSecret);
        SentrySdk.ConfigureScope(p =>
        {
            called = true;
            Assert.Empty(p.Breadcrumbs);
        });
        Assert.True(called);

        first.Dispose();
        second.Dispose();
    }

    [Fact(Skip = "Flaky (after review)")]
    public async Task Init_WithCache_BlocksUntilExistingCacheIsFlushed()
    {
        // Arrange
        using var cacheDirectory = new TempDirectory();

        {
            // Pre-populate cache
            var initialInnerTransport = new FakeFailingTransport();
            await using var initialTransport = new CachingTransport(initialInnerTransport, new SentryOptions
            {
                DiagnosticLogger = _logger,
                Dsn = ValidDsnWithoutSecret,
                CacheDirectoryPath = cacheDirectory.Path
            });

            // Shutdown the worker to make sure nothing gets processed
            await initialTransport.StopWorkerAsync();

            for (var i = 0; i < 3; i++)
            {
                using var envelope = Envelope.FromEvent(new SentryEvent());
                await initialTransport.SendEnvelopeAsync(envelope);
            }
        }

        // Act
        using var transport = new FakeTransport();
        using var _ = SentrySdk.Init(o =>
        {
            o.Dsn = ValidDsnWithoutSecret;
            o.DiagnosticLogger = _logger;
            o.CacheDirectoryPath = cacheDirectory.Path;
            o.InitCacheFlushTimeout = TimeSpan.FromSeconds(30);
            o.Transport = transport;
        });

        // Assert
        Directory
            .EnumerateFiles(cacheDirectory.Path, "*", SearchOption.AllDirectories)
            .ToArray()
            .Should().BeEmpty();
    }

    [Fact]
    public void Disposable_MultipleCalls_NoOp()
    {
        var disposable = SentrySdk.Init();
        disposable.Dispose();
        disposable.Dispose();
        Assert.False(SentrySdk.IsEnabled);
    }

    [Fact]
    public void Dispose_DisposingFirst_DoesntAffectSecond()
    {
        var first = SentrySdk.Init(ValidDsnWithSecret);
        var second = SentrySdk.Init(ValidDsnWithSecret);
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
    public async Task FlushAsync_NotInit_NoOp() => await SentrySdk.FlushAsync(TimeSpan.FromDays(1));

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
    public void AddBreadcrumb_NoClock_NoOp() => SentrySdk.AddBreadcrumb(null);

    [Fact]
    public void AddBreadcrumb_WithClock_NoOp() => SentrySdk.AddBreadcrumb(clock: null, null);

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
        using (SentrySdk.Init(ValidDsnWithoutSecret))
        {
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
    }

    [Obsolete]
    [Fact]
    public void WithScope_DisabledSdk_CallbackNeverInvoked()
    {
        var invoked = false;
        SentrySdk.WithScope(_ => invoked = true);
        Assert.False(invoked);
    }

    [Obsolete]
    [Fact]
    public void WithScope_InvokedWithNewScope()
    {
        using (SentrySdk.Init(ValidDsnWithoutSecret))
        {
            Scope expected = null;
            SentrySdk.ConfigureScope(s => expected = s);

            Scope actual = null;
            SentrySdk.WithScope(s => actual = s);
            Assert.NotNull(actual);

            Assert.NotSame(expected, actual);

            SentrySdk.ConfigureScope(s => Assert.Same(expected, s));
        }
    }

    [Fact]
    public void CaptureEvent_WithConfiguredScope_ScopeAppliesToEvent()
    {
        const string expected = "test";
        var worker = Substitute.For<IBackgroundWorker>();

        using (SentrySdk.Init(o =>
               {
                   o.Dsn = ValidDsnWithoutSecret;
                   o.BackgroundWorker = worker;
               }))
        {
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
    }

    [Fact]
    public void CaptureEvent_WithConfiguredScope_ScopeOnlyAppliesOnlyOnce()
    {
        using (SentrySdk.Init(ValidDsnWithoutSecret))
        {
            var callbackCounter = 0;
            SentrySdk.CaptureEvent(new SentryEvent(), _ => callbackCounter++);
            SentrySdk.CaptureEvent(new SentryEvent());

            Assert.Equal(1, callbackCounter);
        }
    }

    [Fact]
    public void CaptureEvent_WithConfiguredScope_ScopeCallbackGetsInvoked()
    {
        var scopeCallbackWasInvoked = false;
        using (SentrySdk.Init(o => o.Dsn = ValidDsnWithoutSecret))
        {
            SentrySdk.CaptureEvent(new SentryEvent(), _ => scopeCallbackWasInvoked = true);

            Assert.True(scopeCallbackWasInvoked);
        }
    }

    [Fact]
    public void CaptureException_WithConfiguredScope_ScopeCallbackGetsInvoked()
    {
        var scopeCallbackWasInvoked = false;
        using (SentrySdk.Init(o => o.Dsn = ValidDsnWithoutSecret))
        {
            SentrySdk.CaptureException(new Exception(), _ => scopeCallbackWasInvoked = true);

            Assert.True(scopeCallbackWasInvoked);
        }
    }

    [Fact]
    public void CaptureMessage_WithConfiguredScope_ScopeCallbackGetsInvoked()
    {
        var scopeCallbackWasInvoked = false;
        using (SentrySdk.Init(o => o.Dsn = ValidDsnWithoutSecret))
        {
            SentrySdk.CaptureMessage("TestMessage", _ => scopeCallbackWasInvoked = true);

            Assert.True(scopeCallbackWasInvoked);
        }
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
        const string expected = "test";
        using (SentrySdk.Init(o =>
               {
                   o.Dsn = ValidDsnWithSecret;
                   o.BackgroundWorker = worker;
               }))
        {
            SentrySdk.AddBreadcrumb(expected);
            _ = SentrySdk.CaptureMessage("message");

            _ = worker.EnqueueEnvelope(
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
            .Select(m => m.ToString().Replace($"({typeof(ISentryClient).FullName}, ", "("));
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
        var sut = SentrySdk.InitHub(new SentryOptions()) as IDisposable;
        sut?.Dispose();
    }

    [Fact]
    public async Task InitHub_NoDsn_FlushAsyncDoesNotThrow()
    {
        var sut = SentrySdk.InitHub(new SentryOptions());
        await sut.FlushAsync(TimeSpan.FromDays(1));
    }

    [Fact]
    public void InitHub_GlobalModeOff_AsyncLocalContainer()
    {
        // Act
        var sut = SentrySdk.InitHub(new SentryOptions
        {
            Dsn = ValidDsnWithoutSecret,
            IsGlobalModeEnabled = false
        });

        var hub = (Hub)sut;

        // Assert
        hub.ScopeManager.ScopeStackContainer.Should().BeOfType<AsyncLocalScopeStackContainer>();
    }

    [Fact]
    public void InitHub_GlobalModeOn_GlobalContainer()
    {
        // Act
        var sut = SentrySdk.InitHub(new SentryOptions
        {
            Dsn = ValidDsnWithoutSecret,
            IsGlobalModeEnabled = true
        });

        var hub = (Hub)sut;

        // Assert
        hub.ScopeManager.ScopeStackContainer.Should().BeOfType<GlobalScopeStackContainer>();
    }

    [Fact]
    public void InitHub_GlobalModeOn_NoWarningOrErrorLogged()
    {
        var logger = Substitute.For<IDiagnosticLogger>();
        logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);

        _ = SentrySdk.InitHub(new SentryOptions
        {
            Dsn = ValidDsnWithoutSecret,
            DiagnosticLogger = logger,
            IsGlobalModeEnabled = true,
            Debug = true
        });

        logger.DidNotReceive().Log(
            SentryLevel.Warning,
            Arg.Any<string>(),
            Arg.Any<Exception>(),
            Arg.Any<object[]>());

        logger.DidNotReceive().Log(
            SentryLevel.Error,
            Arg.Any<string>(),
            Arg.Any<Exception>(),
            Arg.Any<object[]>());
    }

    [Fact]
    public void InitHub_GlobalModeOff_NoWarningOrErrorLogged()
    {
        var logger = Substitute.For<IDiagnosticLogger>();
        logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);

        _ = SentrySdk.InitHub(new SentryOptions
        {
            Dsn = ValidDsnWithoutSecret,
            DiagnosticLogger = logger,
            IsGlobalModeEnabled = false,
            Debug = true
        });

        logger.DidNotReceive().Log(
            SentryLevel.Warning,
            Arg.Any<string>(),
            Arg.Any<Exception>(),
            Arg.Any<object[]>());

        logger.DidNotReceive().Log(
            SentryLevel.Error,
            Arg.Any<string>(),
            Arg.Any<Exception>(),
            Arg.Any<object[]>());
    }

    [Fact]
    public void InitHub_DebugEnabled_DebugLogsLogged()
    {
        var logger = Substitute.For<IDiagnosticLogger>();
        logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);

        _ = SentrySdk.InitHub(new SentryOptions
        {
            Dsn = ValidDsnWithoutSecret,
            DiagnosticLogger = logger,
            IsGlobalModeEnabled = true,
            Debug = true
        });

        logger.Received().Log(
            SentryLevel.Debug,
            Arg.Any<string>(),
            Arg.Any<Exception>(),
            Arg.Any<object[]>());
    }
}
