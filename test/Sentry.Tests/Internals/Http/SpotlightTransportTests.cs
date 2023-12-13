using Sentry.Http;
using Sentry.Tests.Helpers;

namespace Sentry.Tests.Internals.Http;

public class SpotlightTransportTests
{
    // Makes sure it'll call both inner transport and spotlight, even if spotlight's request fails.
    // Inner transport error actually bubbles up instead of Spotlights'
    [Fact]
    public async Task SendEnvelopeAsync_SpotlightRequestFailed_InnerTransportFailureBubblesUp()
    {
        // Arrange
        var httpHandler = Substitute.For<MockableHttpMessageHandler>();
        var expectedSpotlightTransportException = new Exception("Spotlight request fails");
        httpHandler.WhenForAnyArgs(h => h.VerifiableSendAsync(null, CancellationToken.None))
            .Throw(expectedSpotlightTransportException);

        var innerTransport = Substitute.For<ITransport>();
        var logger = new InMemoryDiagnosticLogger();

        var sut = new SpotlightHttpTransport(
            innerTransport,
            new SentryOptions
            {
                Dsn = ValidDsn,
                Debug = true,
                DiagnosticLogger = logger
            },
            new HttpClient(httpHandler),
            new Uri("http://localhost:8969/stream"),
            Substitute.For<ISystemClock>());

        var envelope = Envelope.FromEvent(new SentryEvent());
        var expectedInnerTransportException = new Exception("expected inner transport exception");
        var tcs = new TaskCompletionSource<bool>();
        tcs.SetException(expectedInnerTransportException);
        innerTransport.SendEnvelopeAsync(envelope).Returns(tcs.Task);

        // Act
        var actualException = await Assert.ThrowsAsync<Exception>(() => sut.SendEnvelopeAsync(envelope));

        // Assert
        // Inner transport Exception bubbles out
        Assert.Same(expectedInnerTransportException, actualException);

        // Spotlight request failure logged out to diagnostic logger
        logger.Entries.Any(e =>
            e.Level == SentryLevel.Error &&
            e.Message == "Failed sending envelope to Spotlight." &&
            ReferenceEquals(expectedSpotlightTransportException, e.Exception)
        ).Should().BeTrue();
    }
}
