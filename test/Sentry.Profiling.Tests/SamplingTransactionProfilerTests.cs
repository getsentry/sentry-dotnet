using Sentry.Internal.Http;

namespace Sentry.Profiling.Tests;

// Note: we must not run tests in parallel because we only support profiling one transaction at a time.
// That means setting up a test-collection with parallelization disabled and NOT using any async test functions.
[CollectionDefinition("SamplingProfiler tests", DisableParallelization = true)]
[UsesVerify]
public class SamplingTransactionProfilerTests
{
    private readonly IDiagnosticLogger _testOutputLogger;
    private readonly SentryOptions _testSentryOptions;

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
            Thread.Sleep((int)Math.Min(milliseconds / 5, milliseconds - clock.ElapsedMilliseconds));
        }
    }

    private SampleProfile CaptureAndValidate(ITransactionProfilerFactory factory)
    {
        var clock = SentryStopwatch.StartNew();
        var hub = Substitute.For<IHub>();
        var transactionTracer = new TransactionTracer(hub, "test", "");
        var sut = factory.Start(transactionTracer, CancellationToken.None) as SamplingTransactionProfiler;
        Assert.NotNull(sut);
        transactionTracer.TransactionProfiler = sut;
        RunForMs(200);
        sut.Finish();
        var elapsedNanoseconds = (ulong)((clock.CurrentDateTimeOffset - clock.StartDateTimeOffset).TotalMilliseconds * 1_000_000);

        var transaction = new Transaction(transactionTracer);
        var collectTask = sut.CollectAsync(transaction);
        collectTask.Wait();
        var profileInfo = collectTask.Result;
        Assert.NotNull(profileInfo);
        ValidateProfile(profileInfo.Profile, elapsedNanoseconds);
        return profileInfo.Profile;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void Profiler_SingleProfile_Works(int startTimeoutSeconds)
    {
        using var factory = new SamplingTransactionProfilerFactory(_testSentryOptions, TimeSpan.FromSeconds(startTimeoutSeconds));
        // in the async startup case, we need to wait before collecting
        if (startTimeoutSeconds == 0)
        {
            factory._sessionTask.Wait(5 * 1000);
        }
        var profile = CaptureAndValidate(factory);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void Profiler_MultipleProfiles_Works(int startTimeoutSeconds)
    {
        using var factory = new SamplingTransactionProfilerFactory(_testSentryOptions, TimeSpan.FromSeconds(startTimeoutSeconds));
        // in the async startup case, we need to wait before collecting
        if (startTimeoutSeconds == 0)
        {
            factory._sessionTask.Wait(5 * 1000);
        }
        CaptureAndValidate(factory);
        Thread.Sleep(100);
        CaptureAndValidate(factory);
        Thread.Sleep(300);
        CaptureAndValidate(factory);
    }

    [Fact]
    public void Profiler_AfterTimeout_Stops()
    {
        using var session = SampleProfilerSession.StartNew(_testOutputLogger);
        var limitMs = 50;
        var sut = new SamplingTransactionProfiler(_testSentryOptions, session, limitMs, CancellationToken.None);
        RunForMs(limitMs * 4);
        sut.Finish();

        var collectTask = sut.CollectAsync(new Transaction("foo", "bar"));
        collectTask.Wait();
        var profileInfo = collectTask.Result;

        Assert.NotNull(profileInfo);
        ValidateProfile(profileInfo.Profile, (ulong)(limitMs * 1_000_000));
    }

    [Theory]
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
        var fileSystem = new FakeFileSystem();
        using var cacheDirectory = offlineCaching ? new TempDirectory(fileSystem) : null;

        // profiler temp dir (doesn't support `FileSystem`)
        var tempDir = new TempDirectory();

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            // To go through a round trip serialization of cached envelope
            CacheDirectoryPath = cacheDirectory?.Path,
            FileSystem = fileSystem,
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

        // Disable process exit flush to resolve "There is no currently active test." errors.
        options.DisableAppDomainProcessExitFlush();

        options.AddIntegration(new ProfilingIntegration(TimeSpan.FromSeconds(5)));

        try
        {
            using var hub = new Hub(options);

            var clock = SentryStopwatch.StartNew();
            var tx = hub.StartTransaction("name", "op");
            RunForMs(100);
            tx.Finish();
            var elapsedNanoseconds = (ulong)((clock.CurrentDateTimeOffset - clock.StartDateTimeOffset).TotalMilliseconds * 1_000_000);

            hub.FlushAsync().Wait();

            // Synchronizing in the tests to go through the caching and http transports
            cts.CancelAfter(options.FlushTimeout + TimeSpan.FromSeconds(1));
            var ex = Record.Exception(() => tcs.Task.Wait());
            Assert.Null(ex);
            Assert.True(tcs.Task.IsCompleted);

            var envelopeLines = tcs.Task.Result.Split('\n');
            envelopeLines.Length.Should().Be(6);

            // header rows before payloads
            envelopeLines[1].Should().StartWith("{\"type\":\"transaction\"");
            envelopeLines[3].Should().StartWith("{\"type\":\"profile\"");

            var transaction = Json.Parse(envelopeLines[2], Transaction.FromJson);

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

    [Fact]
    public void ProfilerIntegration_WithProfilingDisabled_LeavesFactoryNull()
    {
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1.0,
            ProfilesSampleRate = 0,
        };
        options.AddIntegration(new ProfilingIntegration());
        using var hub = new Hub(options);
        Assert.Null(hub.Options.TransactionProfilerFactory);
    }

    [Fact]
    public void ProfilerIntegration_WithTracingDisabled_LeavesFactoryNull()
    {
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 0,
            ProfilesSampleRate = 1.0,
        };
        options.AddIntegration(new ProfilingIntegration());
        using var hub = new Hub(options);
        Assert.Null(hub.Options.TransactionProfilerFactory);
    }

    [Fact]
    public void ProfilerIntegration_WithProfilingEnabled_SetsFactory()
    {
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1.0,
            ProfilesSampleRate = 1.0,
        };
        options.AddIntegration(new ProfilingIntegration());
        using var hub = new Hub(options);
        Assert.NotNull(hub.Options.TransactionProfilerFactory);
    }

    [Fact]
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
