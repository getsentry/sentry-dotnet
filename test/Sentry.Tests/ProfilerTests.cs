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
            using var hub = SentrySdk.InitHub(options) as Hub;
            options.TransactionProfilerFactory.Should().NotBeNull();

            var clock = SentryStopwatch.StartNew();
            var tx = hub.StartTransaction("name", "op");
            RunForMs(500);
            tx.Finish();
            await hub.FlushAsync();

            // Asserts
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1_000));
            completedTask.Should().Be(tcs.Task);
            var envelopeLines = (await tcs.Task).Split('\n');
            envelopeLines.Length.Should().Be(6);

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
            Thread.Sleep((int)Math.Max(0, Math.Min(milliseconds / 5, milliseconds - clock.ElapsedMilliseconds)));
        }
    }
}
