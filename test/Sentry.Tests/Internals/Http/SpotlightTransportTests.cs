using Sentry.Http;
using Sentry.Tests.Helpers;

namespace Sentry.Tests.Internals.Http;

public class SpotlightTransportTests
{
    private static readonly DateTimeOffset StartTime = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private class Fixture
    {
        public MockClock Clock { get; } = new(StartTime);
        public MockableHttpMessageHandler Handler { get; } = Substitute.For<MockableHttpMessageHandler>();
        public InMemoryDiagnosticLogger Logger { get; } = new();
        public ITransport InnerTransport { get; } = Substitute.For<ITransport>();

        private bool _shouldFail;

        public Fixture()
        {
            InnerTransport.SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
        }

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
                    if (_shouldFail) throw new HttpRequestException("Connection refused");
                    return SentryResponses.GetOkResponse();
                });
        }

        public void SetShouldFail(bool value) => _shouldFail = value;

        public int SpotlightErrorCount()
        {
            return Logger.Entries.Count(e =>
                e.Level == SentryLevel.Error &&
                e.Message == "Failed sending envelope to Spotlight.");
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
                InnerTransport,
                options,
                new HttpClient(Handler),
                new Uri("http://localhost:8969/stream"),
                Clock);
        }
    }

    // Makes sure it'll call both inner transport and spotlight, even if spotlight's request fails.
    // Inner transport error actually bubbles up instead of Spotlights'
    [Fact]
    public async Task SendEnvelopeAsync_SpotlightRequestFailed_InnerTransportFailureBubblesUp()
    {
        // Arrange
        var fixture = new Fixture();
        var expectedSpotlightTransportException = new Exception("Spotlight request fails");
        fixture.ConfigureHandlerToThrow(expectedSpotlightTransportException);
        var sut = fixture.GetSut();

        var envelope = Envelope.FromEvent(new SentryEvent());
        var expectedInnerTransportException = new Exception("expected inner transport exception");
        var tcs = new TaskCompletionSource<bool>();
        tcs.SetException(expectedInnerTransportException);
        fixture.InnerTransport.SendEnvelopeAsync(envelope).Returns(tcs.Task);

        // Act
        var actualException = await Assert.ThrowsAsync<Exception>(() => sut.SendEnvelopeAsync(envelope));

        // Assert
        // Inner transport Exception bubbles out
        Assert.Same(expectedInnerTransportException, actualException);

        // Spotlight request failure logged out to diagnostic logger
        fixture.Logger.Entries.Any(e =>
            e.Level == SentryLevel.Error &&
            e.Message == "Failed sending envelope to Spotlight." &&
            ReferenceEquals(expectedSpotlightTransportException, e.Exception)
        ).Should().BeTrue();
    }

    // Test error handling and backoff logic
    // Ref: https://develop.sentry.dev/sdk/foundations/client/integrations/spotlight/#error-handling

    // Spec: Unreachable Server — SDKs "MUST log an error message at least once"
    [Fact]
    public async Task SendEnvelopeAsync_FirstFailure_LogsError()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.ConfigureHandlerToThrow();
        var sut = fixture.GetSut();

        // Act
        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        // Assert
        fixture.SpotlightErrorCount().Should().Be(1);
    }

    // Spec: Unreachable Server — SDKs "MUST NOT log an error message for every failed envelope"
    // Spec: Logging — "MUST avoid logging errors for every failed envelope to prevent log spam"
    [Fact]
    public async Task SendEnvelopeAsync_SecondFailureAfterBackoff_DoesNotLogAgain()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.ConfigureHandlerToThrow();
        var sut = fixture.GetSut();

        // Act — first failure (logs error, sets backoff to 1s)
        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        // Advance past backoff so second attempt is made
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromSeconds(2));
        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        // Assert — still only one error logged
        fixture.SpotlightErrorCount().Should().Be(1);
    }

    // Spec: Unreachable Server — SDKs "SHOULD implement exponential backoff retry logic"
    // Spec: "Spotlight transmission MUST never block normal Sentry operation."
    // Verifies sends are skipped during backoff, but inner transport still runs.
    [Fact]
    public async Task SendEnvelopeAsync_DuringBackoffPeriod_SkipsSpotlightSend()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.ConfigureHandlerToThrow();
        var sut = fixture.GetSut();

        // First failure — sets backoff to 1s
        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        // Advance only 500ms (still within 1s backoff)
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromMilliseconds(500));
        fixture.Handler.ClearReceivedCalls();

        // Act
        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        // Assert — Spotlight HTTP call was NOT made during backoff
        await fixture.Handler.DidNotReceiveWithAnyArgs().VerifiableSendAsync(null!, CancellationToken.None);

        // Inner transport was still called for both envelopes
        await fixture.InnerTransport.Received(2).SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>());
    }

    // Spec: Unreachable Server — SDKs "SHOULD implement exponential backoff retry logic"
    // Verifies that after the backoff period expires, Spotlight send is retried on the next envelope.
    [Fact]
    public async Task SendEnvelopeAsync_AfterBackoffExpires_RetriesSpotlightSend()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.ConfigureHandlerToThrow();
        var sut = fixture.GetSut();

        // First failure — sets backoff to 1s
        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        // Advance past the 1s backoff
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromSeconds(1.5));
        fixture.Handler.ClearReceivedCalls();

        // Act
        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        // Assert — Spotlight HTTP call WAS made after backoff expired
        await fixture.Handler.ReceivedWithAnyArgs(1).VerifiableSendAsync(null!, CancellationToken.None);
    }

    // Spec: Unreachable Server — SDKs "SHOULD implement exponential backoff retry logic"
    // Verifies the doubling sequence: 1s -> 2s -> ...
    [Fact]
    public async Task SendEnvelopeAsync_ConsecutiveFailures_BackoffDoubles()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.ConfigureHandlerToThrow();
        var sut = fixture.GetSut();

        // First failure — backoff = 1s
        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        // Advance past 1s, second failure — backoff = 2s
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromSeconds(1.5));
        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        // Advance only 1.5s more (total 3s from start, but need 2s from last failure at t=1.5s)
        // Last failure was at t=1.5s, backoff=2s, so retryAfter=3.5s
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromSeconds(3));
        fixture.Handler.ClearReceivedCalls();

        // Act — should be skipped (3.0 < 3.5)
        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        // Assert — still in backoff, no call made
        await fixture.Handler.DidNotReceiveWithAnyArgs().VerifiableSendAsync(null!, CancellationToken.None);

        // Advance past 3.5s — now should retry
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromSeconds(4));
        fixture.Handler.ClearReceivedCalls();

        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        // Assert — call was made
        await fixture.Handler.ReceivedWithAnyArgs(1).VerifiableSendAsync(null!, CancellationToken.None);
    }

    [Fact]
    public async Task SendEnvelopeAsync_BackoffCapsAtSixtySeconds()
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
            await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));
            // retryAfter was set to currentTime + delay inside the catch
            currentTime += TimeSpan.FromSeconds(delay) + TimeSpan.FromMilliseconds(100);
            fixture.Clock.SetUtcNow(currentTime);
        }

        // The 7th failure happened at clock = StartTime + sum(1..32) + 6*0.1 = StartTime + 63.6s
        // retryDelay capped at 60 (not 64), so retryAfter = StartTime + 63.6 + 60 = StartTime + 123.6s

        // At 123s from start — should still be in backoff
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromSeconds(123));
        fixture.Handler.ClearReceivedCalls();

        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));
        await fixture.Handler.DidNotReceiveWithAnyArgs().VerifiableSendAsync(null!, CancellationToken.None);

        // At 124s from start — should retry (proves cap at 60, not 64)
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromSeconds(124));
        fixture.Handler.ClearReceivedCalls();

        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));
        await fixture.Handler.ReceivedWithAnyArgs(1).VerifiableSendAsync(null!, CancellationToken.None);
    }

    // Spec: Logging — "MAY consider logging once per connection failure, then periodically if
    //        failures persist"
    // Verifies that a successful send resets all backoff state (delay, log flag), so the next
    // failure is treated as a fresh first failure.
    [Fact]
    public async Task SendEnvelopeAsync_SuccessAfterFailure_ResetsBackoffState()
    {
        // Arrange — use a flag to control handler behavior
        var fixture = new Fixture();
        fixture.ConfigureHandlerWithFlag(shouldFail: true);
        var sut = fixture.GetSut();

        // First failure — error logged, backoff = 1s
        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));
        fixture.SpotlightErrorCount().Should().Be(1);

        // Advance past backoff, switch to success
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromSeconds(2));
        fixture.SetShouldFail(false);

        // Success — resets all backoff state
        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        // Switch back to failure
        fixture.SetShouldFail(true);

        // Act — fail again immediately (no backoff because state was reset)
        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

        // Assert — a second error IS logged (hasLoggedError was reset)
        fixture.SpotlightErrorCount().Should().Be(2);

        // Verify backoff reset to 1s (not 2s): advance 1.5s should allow retry
        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromSeconds(4));
        fixture.Handler.ClearReceivedCalls();
        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));
        await fixture.Handler.ReceivedWithAnyArgs(1).VerifiableSendAsync(null!, CancellationToken.None);
    }

    // Spec: "Spotlight transmission MUST never block normal Sentry operation."
    // Spec: Unreachable Server — SDKs "MUST continue normal Sentry operation without interruption"
    // Spec: "Spotlight failures MUST NOT affect event capture, transaction recording, or any
    //        other SDK functionality"
    // Verifies inner transport is called for every envelope, even those skipped during backoff.
    [Fact]
    public async Task SendEnvelopeAsync_SpotlightFails_InnerTransportAlwaysRuns()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.ConfigureHandlerToThrow();
        var sut = fixture.GetSut();

        // Act — send 3 envelopes: first fails, second during backoff, third after backoff
        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent())); // fails, backoff=1s

        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromMilliseconds(500)); // during backoff
        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent())); // skipped

        fixture.Clock.SetUtcNow(StartTime + TimeSpan.FromSeconds(2)); // after backoff
        await sut.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent())); // retried, fails

        // Assert — inner transport was called for ALL 3 envelopes
        await fixture.InnerTransport.Received(3).SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>());
    }
}
