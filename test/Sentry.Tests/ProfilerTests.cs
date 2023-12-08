namespace Sentry.Tests;

public class ProfilerTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public ProfilerTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

#if __IOS__
    [Fact]
#else
    [Fact(Skip = "Profiling is not supported on this platform")]
#endif
    public async Task Profiler_RunningUnderFullClient_SendsProfileData()
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

        try
        {
            using var hub = new Hub(options);

            var clock = SentryStopwatch.StartNew();
            var tx = hub.StartTransaction("name", "op");
            RunForMs(500);
            tx.Finish();
            await hub.FlushAsync();

            // Synchronizing in the tests to go through the caching and http transports
            cts.CancelAfter(options.FlushTimeout + TimeSpan.FromSeconds(1));
            var ex = Record.Exception(() => tcs.Task.Wait());
            ex.Should().BeNull();
            tcs.Task.IsCompleted.Should().BeTrue();

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
        }
        finally
        {
            // Ensure the task is complete before leaving the test so there's no async code left running in next tests.
            tcs.TrySetResult("");
            await tcs.Task;
        }
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
}
