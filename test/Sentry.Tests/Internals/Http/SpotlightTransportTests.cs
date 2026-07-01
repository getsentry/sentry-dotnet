using Sentry.Http;
using Sentry.Tests.Helpers;

namespace Sentry.Tests.Internals.Http;

public class SpotlightTransportTests
{
    private static readonly DateTimeOffset StartTime = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Uri SpotlightUrl = new("http://localhost:8969/stream");
    private static readonly byte[] TestPayload = "test-envelope-payload"u8.ToArray();

    private class Fixture
    {
        public MockClock Clock { get; } = new(StartTime);
        public MockableHttpMessageHandler Handler { get; } = Substitute.For<MockableHttpMessageHandler>();
        public InMemoryDiagnosticLogger Logger { get; } = new();

        private bool _shouldFail;

        public void ConfigureHandlerToThrow(Exception exception = null)
        {
            Handler.WhenForAnyArgs(h => h.VerifiableSendAsync(null!, CancellationToken.None))
                .Throw(exception ?? new HttpRequestException("Connection refused"));
        }

        public void ConfigureHandlerWithFlag(bool shouldFail = true)
        {
            _shouldFail = shouldFail;
            Handler.VerifiableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    if (_shouldFail)
                        throw new HttpRequestException("Connection refused");
                    return SentryResponses.GetOkResponse();
                });
        }

        public void SetShouldFail(bool value) => _shouldFail = value;

        public int SpotlightErrorCount()
        {
            return Logger.Entries.Count(e =>
                e.Level == SentryLevel.Error &&
                e.Message.Contains("Failed sending envelope to Spotlight"));
        }

        public SpotlightHttpTransport GetSut()
        {
            var options = new SentryOptions
            {
                Dsn = ValidDsn,
                Debug = true,
                DiagnosticLogger = Logger
            };
            return new SpotlightHttpTransport(
                options,
                new HttpClient(Handler),
                SpotlightUrl,
                Clock);
        }
    }

    // Spec: Unreachable Server — SDKs "MUST log an error message at least once"
    [Fact]
    public async Task SendAsync_FirstFailure_LogsError()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.ConfigureHandlerToThrow();
        var sut = fixture.GetSut();

        // Act
        await sut.SendAsync(TestPayload);

        // Assert
        fixture.SpotlightErrorCount().Should().Be(1);
    }

    // Spec: Unreachable Server — SDKs "MUST NOT log an error message for every failed envelope"
    // Spec: Logging — "MUST avoid logging errors for every failed envelope to prevent log spam"
    [Fact]
    public async Task SendAsync_SecondFailureAfterBackoff_DoesNotLogAgain()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.ConfigureHandlerToThrow();
        var sut = fixture.GetSut();

        // Act — first failure (logs error, sets backoff to 1s)
        await sut.SendAsync(TestPayload);

        // Advance past backoff so second attempt is made
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromSeconds(2));
        await sut.SendAsync(TestPayload);

        // Assert — still only one error logged
        fixture.SpotlightErrorCount().Should().Be(1);
    }

    // Spec: Unreachable Server — SDKs "SHOULD implement exponential backoff retry logic"
    // Verifies sends are skipped during backoff.
    [Fact]
    public async Task SendAsync_DuringBackoffPeriod_SkipsSpotlightSend()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.ConfigureHandlerToThrow();
        var sut = fixture.GetSut();

        // First failure — sets backoff to 1s
        await sut.SendAsync(TestPayload);

        // Advance only 500ms (still within 1s backoff)
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromMilliseconds(500));
        fixture.Handler.ClearReceivedCalls();

        // Act
        await sut.SendAsync(TestPayload);

        // Assert — Spotlight HTTP call was NOT made during backoff
        await fixture.Handler.DidNotReceiveWithAnyArgs().VerifiableSendAsync(null!, CancellationToken.None);
    }

    // Spec: Unreachable Server — SDKs "SHOULD implement exponential backoff retry logic"
    // Verifies that after the backoff period expires, Spotlight send is retried on the next envelope.
    [Fact]
    public async Task SendAsync_AfterBackoffExpires_RetriesSpotlightSend()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.ConfigureHandlerToThrow();
        var sut = fixture.GetSut();

        // First failure — sets backoff to 1s
        await sut.SendAsync(TestPayload);

        // Advance past the 1s backoff
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromSeconds(1.5));
        fixture.Handler.ClearReceivedCalls();

        // Act
        await sut.SendAsync(TestPayload);

        // Assert — Spotlight HTTP call WAS made after backoff expired
        await fixture.Handler.ReceivedWithAnyArgs(1).VerifiableSendAsync(null!, CancellationToken.None);
    }

    // Spec: Unreachable Server — SDKs "SHOULD implement exponential backoff retry logic"
    // Verifies the doubling sequence: 1s -> 2s -> ...
    [Fact]
    public async Task SendAsync_ConsecutiveFailures_BackoffDoubles()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.ConfigureHandlerToThrow();
        var sut = fixture.GetSut();

        // First failure — backoff = 1s
        await sut.SendAsync(TestPayload);

        // Advance past 1s, second failure — backoff = 2s
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromSeconds(1.5));
        await sut.SendAsync(TestPayload);

        // Advance only 1.5s more (total 3s from start, but need 2s from last failure at t=1.5s)
        // Last failure was at t=1.5s, backoff=2s, so retryAfter=3.5s
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromSeconds(3));
        fixture.Handler.ClearReceivedCalls();

        // Act — should be skipped (3.0 < 3.5)
        await sut.SendAsync(TestPayload);

        // Assert — still in backoff, no call made
        await fixture.Handler.DidNotReceiveWithAnyArgs().VerifiableSendAsync(null!, CancellationToken.None);

        // Advance past 3.5s — now should retry
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromSeconds(4));
        fixture.Handler.ClearReceivedCalls();

        await sut.SendAsync(TestPayload);

        // Assert — call was made
        await fixture.Handler.ReceivedWithAnyArgs(1).VerifiableSendAsync(null!, CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_BackoffCapsAtSixtySeconds()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.ConfigureHandlerToThrow();
        var sut = fixture.GetSut();

        var currentTime = StartTime;

        // Fail repeatedly to escalate backoff: 1, 2, 4, 8, 16, 32, 64->60
        // Each iteration: send (fails at currentTime), then advance clock past the backoff
        var expectedDelays = new[] { 1, 2, 4, 8, 16, 32, 60 };
        foreach (var delay in expectedDelays)
        {
            await sut.SendAsync(TestPayload);
            // retryAfter was set to currentTime + delay inside the catch
            currentTime += TimeSpan.FromSeconds(delay) + TimeSpan.FromMilliseconds(100);
            fixture.Clock.SetUtcNow(currentTime);
        }

        // The 7th failure happened at clock = StartTime + sum(1..32) + 6*0.1 = StartTime + 63.6s
        // retryDelay capped at 60 (not 64), so retryAfter = StartTime + 63.6 + 60 = StartTime + 123.6s

        // At 123s from start — should still be in backoff
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromSeconds(123));
        fixture.Handler.ClearReceivedCalls();

        await sut.SendAsync(TestPayload);
        await fixture.Handler.DidNotReceiveWithAnyArgs().VerifiableSendAsync(null!, CancellationToken.None);

        // At 124s from start — should retry (proves cap at 60, not 64)
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromSeconds(124));
        fixture.Handler.ClearReceivedCalls();

        await sut.SendAsync(TestPayload);
        await fixture.Handler.ReceivedWithAnyArgs(1).VerifiableSendAsync(null!, CancellationToken.None);
    }

    // Spec: Logging — "MAY consider logging once per connection failure, then periodically if
    //        failures persist"
    // Verifies that a successful send resets all backoff state (delay, log flag), so the next
    // failure is treated as a fresh first failure.
    [Fact]
    public async Task SendAsync_SuccessAfterFailure_ResetsBackoffState()
    {
        // Arrange — use a flag to control handler behavior
        var fixture = new Fixture();
        fixture.ConfigureHandlerWithFlag(shouldFail: true);
        var sut = fixture.GetSut();

        // First failure — error logged, backoff = 1s
        await sut.SendAsync(TestPayload);
        fixture.SpotlightErrorCount().Should().Be(1);

        // Advance past backoff, switch to success
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromSeconds(2));
        fixture.SetShouldFail(false);

        // Success — resets all backoff state
        await sut.SendAsync(TestPayload);

        // Switch back to failure
        fixture.SetShouldFail(true);

        // Act — fail again immediately (no backoff because state was reset)
        await sut.SendAsync(TestPayload);

        // Assert — a second error IS logged (hasLoggedError was reset)
        fixture.SpotlightErrorCount().Should().Be(2);

        // Verify backoff reset to 1s (not 2s): advance 1.5s should allow retry
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromSeconds(4));
        fixture.Handler.ClearReceivedCalls();
        await sut.SendAsync(TestPayload);
        await fixture.Handler.ReceivedWithAnyArgs(1).VerifiableSendAsync(null!, CancellationToken.None);
    }

    // Spotlight transport swallows all exceptions — never throws.
    [Fact]
    public async Task SendAsync_SpotlightFailure_DoesNotThrow()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.ConfigureHandlerToThrow(new Exception("catastrophic failure"));
        var sut = fixture.GetSut();

        // Act & Assert — no exception thrown
        await sut.SendAsync(TestPayload);
    }
}
