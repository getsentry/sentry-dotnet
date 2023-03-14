using Sentry.Internal.Http;

namespace Sentry.Profiling.Tests;

// Note: we must not run tests in parallel because we only support profiling one transaction at a time.
// That means setting up a test-collection with parallelization disabled and NOT using any async test functions.
[CollectionDefinition("SamplingProfiler tests", DisableParallelization = true)]
public class SamplingTransactionProfilerTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public SamplingTransactionProfilerTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    private void ValidateProfile(SampleProfile profile, ulong maxTimestampNs)
    {
        profile.Samples.Should().NotBeEmpty();
        profile.Frames.Should().NotBeEmpty();
        profile.Stacks.Should().NotBeEmpty();

        var threadIds = profile.Threads.Keys();

        // Verify that downsampling works.
        var previousSamplesByThread = new Dictionary<int, SampleProfile.Sample>();

        foreach (var sample in profile.Samples)
        {
            sample.Timestamp.Should().BeInRange(0, maxTimestampNs);
            sample.StackId.Should().BeInRange(0, profile.Stacks.Count);
            sample.ThreadId.Should().BeOneOf(threadIds);

            if (previousSamplesByThread.TryGetValue(sample.ThreadId, out var prevSample))
            {
                sample.Timestamp.Should().BeGreaterThan(prevSample.Timestamp + 9_000_000,
                    "Downsampling: there must be at least 9ms between samples on the same thread.");
            }
            previousSamplesByThread[sample.ThreadId] = sample;
        }

        profile.Threads.Foreach((i, thread) =>
        {
            thread.Name.Should().NotBeNullOrEmpty();
        });

        // We can't check that all Frame names are filled because there may be native frames which we currently don't filter out.
        // Let's just check there are some frames with names...
        profile.Frames.Where((frame) => frame.Function is not null).Should().NotBeEmpty();
    }

    private void RunForMs(int milliseconds)
    {
        for (int i = 0; i < milliseconds / 20; i++)
        {
            _testOutputLogger.LogDebug("sleeping...");
            Thread.Sleep(20);
        }
    }

    [Fact]
    public void Profiler_StartedNormally_Works()
    {
        var hub = Substitute.For<IHub>();
        var transactionTracer = new TransactionTracer(hub, "test", "");

        var factory = new SamplingTransactionProfilerFactory(Path.GetTempPath(), _testOutputLogger);
        var clock = SentryStopwatch.StartNew();
        var sut = factory.OnTransactionStart(transactionTracer, clock.CurrentDateTimeOffset, CancellationToken.None);
        transactionTracer.TransactionProfiler = sut;
        RunForMs(100);
        sut.OnTransactionFinish(clock.CurrentDateTimeOffset);
        var elapsedNanoseconds = (ulong)((clock.CurrentDateTimeOffset - clock.StartDateTimeOffset).TotalMilliseconds * 1_000_000);

        var transaction = new Transaction(transactionTracer);
        var collectTask = sut.Collect(transaction);
        collectTask.Wait();
        var profileInfo = collectTask.Result;
        Assert.NotNull(profileInfo);
        ValidateProfile(profileInfo.Profile, elapsedNanoseconds);
    }

    [Fact]
    public void Profiler_AfterTimeout_Stops()
    {
        var hub = Substitute.For<IHub>();

        var clock = SentryStopwatch.StartNew();
        var limitMs = 50;
        var sut = new SamplingTransactionProfiler(Path.GetTempPath(), clock.CurrentDateTimeOffset, limitMs, _testOutputLogger, CancellationToken.None);
        RunForMs(limitMs * 4);
        clock.Elapsed.TotalMilliseconds.Should().BeGreaterThan(limitMs * 4);
        sut.OnTransactionFinish(clock.CurrentDateTimeOffset);

        var collectTask = sut.Collect(new Transaction("foo", "bar"));
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
            CreateHttpClientHandler = () => new CallbackHttpClientHandler(VerifyAsync),
            // Not to send some session envelope
            AutoSessionTracking = false,
            Debug = true,
            DiagnosticLogger = _testOutputLogger,
            TracesSampleRate = 1.0,
        };

        // Disable process exit flush to resolve "There is no currently active test." errors.
        options.DisableAppDomainProcessExitFlush();

        options.AddIntegration(new ProfilingIntegration(tempDir.Path));

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
}
