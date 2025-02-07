using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Sentry.Internal.Http;

namespace Sentry.Profiling.Tests;

// Note: we must not run tests in parallel because we only support profiling one transaction at a time.
// That means setting up a test-collection with parallelization disabled and NOT using any async test functions.
[CollectionDefinition(nameof(SamplingTransactionProfilerTests), DisableParallelization = true)]
public class SamplingTransactionProfilerTests
{
    private readonly TestOutputDiagnosticLogger _testOutputLogger;
    private readonly SentryOptions _testSentryOptions;

    private int RuntimeMs => TestEnvironment.IsGitHubActions ? 5_000 : 300;

    // Note: these tests are flaky in CI. Mostly it's because the profiler sometimes takes very long time to start
    // or it won't start at all in a given timeout. To avoid failing the CI under these expected circumstances, we
    // skip the test if it fails on a particular check.
    // Don't use xUnit asserts in the given callback, only standard exceptions.
    private void SkipIfFailsInCI(Action checks)
    {
        if (TestEnvironment.IsGitHubActions)
        {
            try
            {
                checks.Invoke();
            }
            catch (Exception e)
            {
                _testOutputLogger.LogWarning(e, "Caught an exception in a test block that is allowed to fail when in CI.");
                Skip.If(true, "Caught an exception in a test block that is allowed to fail when in CI.");
            }
        }
        else
        {
            checks.Invoke();
        }
    }

    public SamplingTransactionProfilerTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
        _testSentryOptions = new SentryOptions { Debug = true, DiagnosticLogger = _testOutputLogger };

        // TODO: Change this API. This static API is problematic because state isn't getting cleared anywhere after it's used (end of test runs).
        // This could be resolved if on IDisposable of each test class setting a value, it reset to null.
        // But expects folks to remember doing that, and it hasn't be done so far.
        // Additionally and parallel tests will race and fail.
        SentryClientExtensions.SentryOptionsForTestingOnly = _testSentryOptions;
    }

    private void ValidateProfile(SampleProfile profile, ulong maxTimestampNs)
    {
        profile.Samples.Should().NotBeEmpty();
        profile.Frames.Should().NotBeEmpty();
        profile.Stacks.Should().NotBeEmpty();

        // Verify that downsampling works.
        var previousSamplesByThread = new Dictionary<int, SampleProfile.Sample>();

        foreach (var sample in profile.Samples)
        {
            sample.Timestamp.Should().BeInRange(0, maxTimestampNs);
            sample.StackId.Should().BeInRange(0, profile.Stacks.Count);
            sample.ThreadId.Should().BeInRange(0, profile.Threads.Count);

            if (previousSamplesByThread.TryGetValue(sample.ThreadId, out var prevSample))
            {
                sample.Timestamp.Should().BeGreaterThan(prevSample.Timestamp + 8_000_000,
                    "Downsampling: there must be at least 9ms between samples on the same thread.");
            }
            previousSamplesByThread[sample.ThreadId] = sample;
        }

        foreach (var thread in profile.Threads)
        {
            thread.Name.Should().NotBeNullOrEmpty();
        }

        // We can't check that all Frame names are filled because there may be native frames which we currently don't filter out.
        // Let's just check there are some frames with names...
        profile.Frames.Where((frame) => frame.Function is not null).Should().NotBeEmpty();
    }

    private void RunForMs(int milliseconds)
    {
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            _testOutputLogger.LogDebug("Sleeping... time remaining: {0} ms", milliseconds - clock.ElapsedMilliseconds);
            Thread.Sleep((int)Math.Max(0, Math.Min(milliseconds / 5, milliseconds - clock.ElapsedMilliseconds)));
        }
    }

    private SampleProfile CaptureAndValidate(ITransactionProfilerFactory factory)
    {
        var clock = SentryStopwatch.StartNew();
        var hub = Substitute.For<IHub>();
        var transactionTracer = new TransactionTracer(hub, "test", "");
        var sut = factory.Start(transactionTracer, CancellationToken.None) as SamplingTransactionProfiler;
        SkipIfFailsInCI(() => ArgumentNullException.ThrowIfNull(sut));
        transactionTracer.TransactionProfiler = sut;
        RunForMs(RuntimeMs);
        sut!.Finish();
        var elapsedNanoseconds = (ulong)((clock.CurrentDateTimeOffset - clock.StartDateTimeOffset).TotalMilliseconds * 1_000_000);

        var transaction = new SentryTransaction(transactionTracer);
        var collectTask = sut.CollectAsync(transaction);
        collectTask.Wait();
        var profileInfo = collectTask.Result;
        Assert.NotNull(profileInfo);
        ValidateProfile(profileInfo.Profile, elapsedNanoseconds);
        return profileInfo.Profile;
    }

    [SkippableFact]
    public void Profiler_WithZeroStartupTimeout_CapturesAfterStartingAsynchronously()
    {
        using var factory = new SamplingTransactionProfilerFactory(_testSentryOptions, TimeSpan.Zero);
        var profiler = factory.Start(new TransactionTracer(Substitute.For<IHub>(), "test", ""), CancellationToken.None);
        Assert.Null(profiler);
        SkipIfFailsInCI(() => factory._sessionTask.Wait(60_000));
        CaptureAndValidate(factory);
    }

    [SkippableTheory]
    [InlineData(0)]
    [InlineData(10)]
    private void Profiler_SingleProfile_Works(int startTimeoutSeconds)
    {
        // This test is flaky both on CI and locally.
        Skip.If(true);

        using var factory = new SamplingTransactionProfilerFactory(_testSentryOptions, TimeSpan.FromSeconds(startTimeoutSeconds));
        // in the async startup case, we need to wait before collecting
        if (startTimeoutSeconds == 0)
        {
            factory._sessionTask.Wait(60_000);
        }
        var profile = CaptureAndValidate(factory);
    }

    [SkippableTheory]
    [InlineData(0)]
    [InlineData(10)]
    public void Profiler_MultipleProfiles_Works(int startTimeoutSeconds)
    {
        using var factory = new SamplingTransactionProfilerFactory(_testSentryOptions, TimeSpan.FromSeconds(startTimeoutSeconds));
        // in the async startup case, we need to wait before collecting
        if (startTimeoutSeconds == 0)
        {
            SkipIfFailsInCI(() => factory._sessionTask.Wait(60_000));
        }
        CaptureAndValidate(factory);
        Thread.Sleep(100);
        CaptureAndValidate(factory);
        Thread.Sleep(300);
        CaptureAndValidate(factory);
    }

    [SkippableFact]
    public async Task Profiler_AfterTimeout_Stops()
    {
        SampleProfilerSession? session = null;
        SkipIfFailsInCI(() => session = SampleProfilerSession.StartNew(_testOutputLogger));
        using (session)
        {
            await session!.WaitForFirstEventAsync(CancellationToken.None);
            var limitMs = 50;
            var sut = new SamplingTransactionProfiler(_testSentryOptions, session, limitMs, CancellationToken.None);
            RunForMs(limitMs * 10);
            sut.Finish();

            var collectTask = sut.CollectAsync(new SentryTransaction("foo", "bar"));
            collectTask.Wait();
            var profileInfo = collectTask.Result;

            Assert.NotNull(profileInfo);
            Assert.Contains("Profiling is being cut-of after 50 ms because the transaction takes longer than that.", _testOutputLogger.Entries.Select(e => e.Message));
        }
    }

    [SkippableFact]
    public async Task EventPipeSession_ReceivesExpectedCLREvents()
    {
        SampleProfilerSession? session = null;
        SkipIfFailsInCI(() => session = SampleProfilerSession.StartNew(_testOutputLogger));
        using (session)
        {
            var eventsReceived = new HashSet<string>();
            session!.EventSource.Clr.All += (TraceEvent ev) => eventsReceived.Add(ev.EventName);

            var loadedMethods = new HashSet<string>();
            session!.EventSource.Clr.MethodLoadVerbose += (MethodLoadUnloadVerboseTraceData ev) => loadedMethods.Add(ev.MethodName);


            await session.WaitForFirstEventAsync(CancellationToken.None);
            var limitMs = 50;
            var sut = new SamplingTransactionProfiler(_testSentryOptions, session, limitMs, CancellationToken.None);
            RunForMs(limitMs * 5);
            MethodToBeLoaded(100);
            RunForMs(limitMs * 5);
            sut.Finish();

            Assert.Contains("Method/LoadVerbose", eventsReceived);
            Assert.Contains("Method/ILToNativeMap", eventsReceived);

            Assert.Contains("MethodToBeLoaded", loadedMethods);
        }
    }

    private static long MethodToBeLoaded(int n)
    {
        return -n;
    }

    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ProfilerIntegration_FullRoundtrip_Works(bool offlineCaching)
    {
        var tcs = new TaskCompletionSource<string>();
        async Task VerifyAsync(HttpRequestMessage message)
        {
            var payload = await message.Content!.ReadAsStringAsync();
            // We're actually looking for type:profile but it must be sent in the same envelope as the transaction.
            if (payload.Contains("\"type\":\"transaction\""))
            {
                tcs.TrySetResult(payload);
            }
        }

        var cts = new CancellationTokenSource();
        cts.Token.Register(() => tcs.TrySetCanceled());

        // envelope cache dir
        using var cacheDirectory = offlineCaching ? new TempDirectory() : null;

        // profiler temp dir (doesn't support `FileSystem`)
        var tempDir = new TempDirectory();

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            // To go through a round trip serialization of cached envelope
            CacheDirectoryPath = cacheDirectory?.Path,
            // So we don't need to deal with gzip'ed payload
            RequestBodyCompressionLevel = CompressionLevel.NoCompression,
            CreateHttpMessageHandler = () => new CallbackHttpClientHandler(VerifyAsync),
            // Not to send some session envelope
            AutoSessionTracking = false,
            Debug = true,
            DiagnosticLogger = _testOutputLogger,
            TracesSampleRate = 1.0,
            ProfilesSampleRate = 1.0,
            // This keeps all writing-to-file operations in memory instead of actually writing to disk
            FileSystem = new FakeFileSystem()
        };

        // Disable process exit flush to resolve "There is no currently active test." errors.
        options.DisableAppDomainProcessExitFlush();

        options.AddProfilingIntegration(TimeSpan.FromSeconds(10));

        try
        {
            using var hub = new Hub(options);

            var clock = SentryStopwatch.StartNew();
            var tx = hub.StartTransaction("name", "op");
            RunForMs(RuntimeMs);
            tx.Finish();
            var elapsedNanoseconds = (ulong)((clock.CurrentDateTimeOffset - clock.StartDateTimeOffset).TotalMilliseconds * 1_000_000);

            hub.FlushAsync().Wait();

            // Synchronizing in the tests to go through the caching and http transports
            cts.CancelAfter(options.FlushTimeout + TimeSpan.FromSeconds(1));
            var ex = Record.Exception(tcs.Task.Wait);
            Assert.Null(ex);
            Assert.True(tcs.Task.IsCompleted);

            var envelopeLines = tcs.Task.Result.Split('\n');
            SkipIfFailsInCI(() =>
            {
                if (envelopeLines.Length != 6)
                {
                    throw new ArgumentOutOfRangeException("envelopeLines", "Invalid number of envelope lines.");
                }
            });

            // header rows before payloads
            envelopeLines[1].Should().StartWith("{\"type\":\"transaction\"");
            envelopeLines[3].Should().StartWith("{\"type\":\"profile\"");

            var transaction = Json.Parse(envelopeLines[2], SentryTransaction.FromJson);

            // TODO do we want to bother with JSON parsing just to do this? Doing at least simple checks for now...
            // var profileInfo = Json.Parse(envelopeLines[4], ProfileInfo.FromJson);
            // ValidateProfile(profileInfo.Profile, elapsedNanoseconds);
            envelopeLines[4].Should().Contain("\"profile\":{");
            envelopeLines[4].Should().Contain($"\"id\":\"{transaction.EventId}\"");
            envelopeLines[4].Length.Should().BeGreaterThan(10000);

            Directory.GetFiles(tempDir.Path).Should().BeEmpty("When profiling is done, the temp dir should be empty.");
        }
        finally
        {
            // ensure the task is complete before leaving the test
            tcs.TrySetResult("");
            tcs.Task.Wait();

            if (options.Transport is CachingTransport cachingTransport)
            {
                // Disposing the caching transport will ensure its worker
                // is shut down before we try to dispose and delete the temp folder
                cachingTransport.Dispose();
            }
        }
    }

    [SkippableFact]
    private async Task Profiler_ThrowingOnSessionStartup_DoesntBreakSentryInit()
    {
        // This test is flaky both on CI and locally.
        Skip.If(true);

        SampleProfilerSession.ThrowOnNextStartupForTests = true;

        var tcs = new TaskCompletionSource<string>();
        async Task VerifyAsync(HttpRequestMessage message)
        {
            var payload = await message.Content!.ReadAsStringAsync();
            if (payload.Contains("\"type\":\"transaction\""))
            {
                tcs!.TrySetResult(payload);
            }
        }

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            // So we don't need to deal with gzip'ed payload
            RequestBodyCompressionLevel = CompressionLevel.NoCompression,
            CreateHttpMessageHandler = () => new CallbackHttpClientHandler(VerifyAsync),
            // Not to send some session envelope
            AutoSessionTracking = false,
            Debug = true,
            DiagnosticLogger = _testOutputLogger,
            TracesSampleRate = 1.0,
            ProfilesSampleRate = 1.0,
        };

        options.AddProfilingIntegration(TimeSpan.FromSeconds(10));

        try
        {
            SampleProfilerSession.ThrowOnNextStartupForTests.Should().BeTrue();
            options.TransactionProfilerFactory.Should().BeNull();
            using var hub = (SentrySdk.InitHub(options) as Hub)!;
            SampleProfilerSession.ThrowOnNextStartupForTests.Should().BeFalse();

            if (options.TransactionProfilerFactory is SamplingTransactionProfilerFactory factory)
            {
                factory.StartupTimedOut.Should().BeTrue();
                Skip.If(TestEnvironment.IsGitHubActions, "Session sometimes takes too long to start in CI.");
            }

            options.TransactionProfilerFactory.Should().BeNull();

            var clock = SentryStopwatch.StartNew();
            var tx = hub.StartTransaction("name", "op");
            RunForMs(100);
            tx.Finish();
            await hub.FlushAsync();

            // Asserts
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(10_000)).ConfigureAwait(false);
            completedTask.Should().Be(tcs.Task);
            var envelopeLines = tcs.Task.Result.Split('\n');
            envelopeLines.Length.Should().Be(4);
            envelopeLines[1].Should().StartWith("{\"type\":\"transaction\"");
        }
        finally
        {
            // Ensure the task is complete before leaving the test so there's no async code left running in next tests.
            tcs.TrySetResult("");
            await tcs.Task;
        }
    }

    [SkippableFact]
    public void ProfilerIntegration_WithProfilingDisabled_LeavesFactoryNull()
    {
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1.0,
            ProfilesSampleRate = 0,
        };
        options.AddProfilingIntegration();
        using var hub = new Hub(options);
        Assert.Null(hub.Options.TransactionProfilerFactory);
    }

    [SkippableFact]
    public void ProfilerIntegration_WithTracingDisabled_LeavesFactoryNull()
    {
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 0,
            ProfilesSampleRate = 1.0,
        };
        options.AddProfilingIntegration();
        using var hub = new Hub(options);
        Assert.Null(hub.Options.TransactionProfilerFactory);
    }

    [SkippableFact]
    public void ProfilerIntegration_WithProfilingEnabled_SetsFactory()
    {
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1.0,
            ProfilesSampleRate = 1.0,
        };
        options.AddProfilingIntegration();
        using var hub = new Hub(options);
        Assert.NotNull(hub.Options.TransactionProfilerFactory);
    }

    [SkippableFact]
    public void Downsampler_ShouldSample_Works()
    {
        var sut = new Downsampler();
        sut.NewThreadAdded(0);
        Assert.True(sut.ShouldSample(0, 5));
        Assert.False(sut.ShouldSample(0, 3));
        Assert.False(sut.ShouldSample(0, 6));
        Assert.True(sut.ShouldSample(0, 15));
        Assert.False(sut.ShouldSample(0, 6));
        Assert.False(sut.ShouldSample(0, 16));
    }
}
