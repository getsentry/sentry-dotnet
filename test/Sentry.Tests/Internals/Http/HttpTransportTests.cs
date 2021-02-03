using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Sentry.Internal.Http;
using Sentry.Protocol;
using Sentry.Protocol.Envelopes;
using Sentry.Testing;
using Sentry.Tests.Helpers;
using Xunit;

namespace Sentry.Tests.Internals.Http
{
    public class HttpTransportTests
    {
        [Fact]
        public async Task SendEnvelopeAsync_CancellationToken_PassedToClient()
        {
            // Arrange
            using var source = new CancellationTokenSource();
            source.Cancel();
            var token = source.Token;

            var httpHandler = Substitute.For<MockableHttpMessageHandler>();

            httpHandler.VerifiableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(_ => SentryResponses.GetOkResponse());

            var httpTransport = new HttpTransport(
                new SentryOptions {Dsn = DsnSamples.ValidDsnWithSecret},
                new HttpClient(httpHandler)
            );

            var envelope = Envelope.FromEvent(
                new SentryEvent(eventId: SentryResponses.ResponseId)
            );

#if NET5_0
            await Assert.ThrowsAsync<TaskCanceledException>(() => httpTransport.SendEnvelopeAsync(envelope, token));
#else
            // Act
            await httpTransport.SendEnvelopeAsync(envelope, token);

            // Assert
            await httpHandler
                .Received(1)
                .VerifiableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Is<CancellationToken>(c => c.IsCancellationRequested));
#endif
        }

        [Fact]
        public async Task SendEnvelopeAsync_ResponseNotOkWithJsonMessage_LogsError()
        {
            // Arrange
            const HttpStatusCode expectedCode = HttpStatusCode.BadGateway;
            const string expectedMessage = "Bad Gateway!";
            var expectedCauses = new[] {"invalid file", "wrong arguments"};
            var expectedCausesFormatted = string.Join(", ", expectedCauses);

            var httpHandler = Substitute.For<MockableHttpMessageHandler>();

            httpHandler.VerifiableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(_ => SentryResponses.GetJsonErrorResponse(expectedCode, expectedMessage, expectedCauses));

            var logger = new InMemoryDiagnosticLogger();

            var httpTransport = new HttpTransport(
                new SentryOptions
                {
                    Dsn = DsnSamples.ValidDsnWithSecret,
                    Debug = true,
                    DiagnosticLogger = logger
                },
                new HttpClient(httpHandler)
            );

            var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            await httpTransport.SendEnvelopeAsync(envelope);

            // Assert
            logger.Entries.Any(e =>
                e.Level == SentryLevel.Error &&
                e.Message == "Sentry rejected the envelope {0}. Status code: {1}. Error detail: {2}. Error causes: {3}." &&
                e.Exception == null &&
                e.Args[0].ToString() == envelope.TryGetEventId().ToString() &&
                e.Args[1].ToString() == expectedCode.ToString() &&
                e.Args[2].ToString() == expectedMessage &&
                e.Args[3].ToString() == expectedCausesFormatted
            ).Should().BeTrue();
        }

        [Fact]
        public async Task SendEnvelopeAsync_ResponseNotOkWithStringMessage_LogsError()
        {
            // Arrange
            const HttpStatusCode expectedCode = HttpStatusCode.RequestEntityTooLarge;
            const string expectedMessage = "413 Request Entity Too Large";

            var httpHandler = Substitute.For<MockableHttpMessageHandler>();

            _ = httpHandler.VerifiableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                  .Returns(_ => SentryResponses.GetTextErrorResponse(expectedCode, expectedMessage));

            var logger = new InMemoryDiagnosticLogger();

            var httpTransport = new HttpTransport(
                new SentryOptions
                {
                    Dsn = DsnSamples.ValidDsnWithSecret,
                    Debug = true,
                    DiagnosticLogger = logger
                },
                new HttpClient(httpHandler)
            );

            var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            await httpTransport.SendEnvelopeAsync(envelope);

            // Assert
            _ = logger.Entries.Any(e =>
                    e.Level == SentryLevel.Error &&
                    e.Message == "Sentry rejected the envelope {0}. Status code: {1}. Error detail: {2}." &&
                    e.Exception == null &&
                    e.Args[0].ToString() == envelope.TryGetEventId().ToString() &&
                    e.Args[1].ToString() == expectedCode.ToString() &&
                    e.Args[2].ToString() == expectedMessage
            ).Should().BeTrue();
        }

        [Fact]
        public async Task SendEnvelopeAsync_ResponseNotOkNoMessage_LogsError()
        {
            // Arrange
            const HttpStatusCode expectedCode = HttpStatusCode.BadGateway;

            var httpHandler = Substitute.For<MockableHttpMessageHandler>();

            httpHandler.VerifiableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(_ => SentryResponses.GetJsonErrorResponse(expectedCode, null));

            var logger = new InMemoryDiagnosticLogger();

            var httpTransport = new HttpTransport(
                new SentryOptions
                {
                    Dsn = DsnSamples.ValidDsnWithSecret,
                    Debug = true,
                    DiagnosticLogger = logger
                },
                new HttpClient(httpHandler)
            );

            var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            await httpTransport.SendEnvelopeAsync(envelope);

            // Assert
            logger.Entries.Any(e =>
                e.Level == SentryLevel.Error &&
                e.Message == "Sentry rejected the envelope {0}. Status code: {1}. Error detail: {2}. Error causes: {3}." &&
                e.Exception == null &&
                e.Args[0].ToString() == envelope.TryGetEventId().ToString() &&
                e.Args[1].ToString() == expectedCode.ToString() &&
                e.Args[2].ToString() == HttpTransport.DefaultErrorMessage &&
                e.Args[3].ToString() == string.Empty
            ).Should().BeTrue();
        }

        [Fact]
        public async Task SendEnvelopeAsync_ItemRateLimit_DropsItem()
        {
            // Arrange
            using var httpHandler = new RecordingHttpMessageHandler(
                new FakeHttpMessageHandler(
                    () => SentryResponses.GetRateLimitResponse("1234:event, 897:transaction")
                )
            );

            var httpTransport = new HttpTransport(
                new SentryOptions
                {
                    Dsn = DsnSamples.ValidDsnWithSecret
                },
                new HttpClient(httpHandler)
            );

            // First request always goes through
            await httpTransport.SendEnvelopeAsync(Envelope.FromEvent(new SentryEvent()));

            var envelope = new Envelope(
                new Dictionary<string, object>(),
                new[]
                {
                    // Should be dropped
                    new EnvelopeItem(
                        new Dictionary<string, object> {["type"] = "event"},
                        new EmptySerializable()),
                    new EnvelopeItem(
                        new Dictionary<string, object> {["type"] = "event"},
                        new EmptySerializable()),
                    new EnvelopeItem(
                        new Dictionary<string, object> {["type"] = "transaction"},
                        new EmptySerializable()),

                    // Should stay
                    new EnvelopeItem(
                        new Dictionary<string, object> {["type"] = "other"},
                        new EmptySerializable())
                }
            );

            var expectedEnvelope = new Envelope(
                new Dictionary<string, object>(),
                new[]
                {
                    new EnvelopeItem(
                        new Dictionary<string, object> {["type"] = "other"},
                        new EmptySerializable())
                }
            );

            var expectedEnvelopeSerialized = await expectedEnvelope.SerializeToStringAsync();

            // Act
            await httpTransport.SendEnvelopeAsync(envelope);

            var lastRequest = httpHandler.GetRequests().Last();
            var actualEnvelopeSerialized = await lastRequest.Content.ReadAsStringAsync();

            // Assert
            actualEnvelopeSerialized.Should().BeEquivalentTo(expectedEnvelopeSerialized);
        }

        [Fact]
        public async Task SendEnvelopeAsync_AttachmentTooLarge_DropsItem()
        {
            // Arrange
            using var httpHandler = new RecordingHttpMessageHandler(
                new FakeHttpMessageHandler()
            );

            var logger = new InMemoryDiagnosticLogger();

            var httpTransport = new HttpTransport(
                new SentryOptions
                {
                    Dsn = DsnSamples.ValidDsnWithSecret,
                    MaxAttachmentSize = 1,
                    DiagnosticLogger = logger,
                    Debug = true
                },
                new HttpClient(httpHandler)
            );

            var attachmentNormal = new Attachment(
                AttachmentType.Default,
                new StreamAttachmentContent(new MemoryStream(new byte[] {1})),
                "test1.txt",
                null
            );

            var attachmentTooBig = new Attachment(
                AttachmentType.Default,
                new StreamAttachmentContent(new MemoryStream(new byte[] {1, 2, 3, 4, 5})),
                "test2.txt",
                null
            );

            using var envelope = Envelope.FromEvent(
                new SentryEvent(),
                new[] {attachmentNormal, attachmentTooBig}
            );

            // Act
            await httpTransport.SendEnvelopeAsync(envelope);

            var lastRequest = httpHandler.GetRequests().Last();
            var actualEnvelopeSerialized = await lastRequest.Content.ReadAsStringAsync();

            // Assert
            // (the envelope should have only one item)

            logger.Entries.Should().Contain(e =>
                e.Message == "Attachment '{0}' dropped because it's too large ({1} bytes)." &&
                e.Args[0].ToString() == "test2.txt" &&
                e.Args[1].ToString() == "5"
            );

            actualEnvelopeSerialized.Should().NotContain("test2.txt");
        }

        [Fact]
        public void CreateRequest_AuthHeader_IsSet()
        {
            // Arrange
            var httpTransport = new HttpTransport(
                new SentryOptions {Dsn = DsnSamples.ValidDsnWithSecret},
                new HttpClient()
            );

            var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            using var request = httpTransport.CreateRequest(envelope);
            var authHeader = request.Headers.GetValues("X-Sentry-Auth").FirstOrDefault();

            // Assert
            authHeader.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void CreateRequest_RequestMethod_Post()
        {
            // Arrange
            var httpTransport = new HttpTransport(
                new SentryOptions {Dsn = DsnSamples.ValidDsnWithSecret},
                new HttpClient()
            );

            var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            var request = httpTransport.CreateRequest(envelope);

            // Assert
            request.Method.Should().Be(HttpMethod.Post);
        }

        [Fact]
        public void CreateRequest_SentryUrl_FromOptions()
        {
            // Arrange
            var httpTransport = new HttpTransport(
                new SentryOptions {Dsn = DsnSamples.ValidDsnWithSecret},
                new HttpClient()
            );

            var envelope = Envelope.FromEvent(new SentryEvent());

            var uri = Dsn.Parse(DsnSamples.ValidDsnWithSecret).GetEnvelopeEndpointUri();

            // Act
            var request = httpTransport.CreateRequest(envelope);

            // Assert
            request.RequestUri.Should().Be(uri);
        }

        [Fact]
        public async Task CreateRequest_Content_IncludesEvent()
        {
            // Arrange
            var httpTransport = new HttpTransport(
                new SentryOptions {Dsn = DsnSamples.ValidDsnWithSecret},
                new HttpClient()
            );

            var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            var request = httpTransport.CreateRequest(envelope);
            var requestContent = await request.Content.ReadAsStringAsync();

            // Assert
            requestContent.Should().Contain(envelope.TryGetEventId().ToString());
        }
    }
}
