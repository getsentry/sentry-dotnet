using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Internal.Http;
using Sentry.Protocol;
using Sentry.Testing;
using Sentry.Tests.Helpers;
using Xunit;

namespace Sentry.Tests.Internals.Http
{
    public class HttpTransportTests
    {
        private class Fixture
        {
            public SentryOptions SentryOptions { get; set; } = new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                DiagnosticLogger = Substitute.For<IDiagnosticLogger>()
            };

            public HttpClient HttpClient { get; set; }
            public MockableHttpMessageHandler HttpMessageHandler { get; set; } = Substitute.For<MockableHttpMessageHandler>();
            public HttpContent HttpContent { get; set; } = Substitute.For<HttpContent>();
            public Action<HttpRequestHeaders> AddAuth { get; set; } = _ => { };

            public Fixture()
            {
                _ = HttpMessageHandler.VerifyableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                        .Returns(_ => SentryResponses.GetOkResponse());

                HttpClient = new HttpClient(HttpMessageHandler);
            }

            public HttpTransport GetSut() => new HttpTransport(SentryOptions, HttpClient, AddAuth);
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public async Task CaptureEventAsync_CancellationToken_PassedToClient()
        {
            var source = new CancellationTokenSource();
            source.Cancel();
            var token = source.Token;
            var sut = _fixture.GetSut();

            await sut.CaptureEventAsync(
                new SentryEvent(
                    id: SentryResponses.ResponseId),
                token);

            _ = await _fixture.HttpMessageHandler
                    .Received(1)
                    .VerifyableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Is<CancellationToken>(c => c.IsCancellationRequested));
        }

        [Fact]
        public async Task CaptureEventAsync_ResponseNotOkWithMessage_LogsError()
        {
            const HttpStatusCode expectedCode = HttpStatusCode.BadGateway;
            const string expectedMessage = "Bad Gateway!";
            var expectedEvent = new SentryEvent();

            _fixture.SentryOptions.Debug = true;
            _ = _fixture.SentryOptions.DiagnosticLogger.IsEnabled(SentryLevel.Error).Returns(true);
            _ = _fixture.HttpMessageHandler.VerifyableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                    .Returns(_ => SentryResponses.GetErrorResponse(expectedCode, expectedMessage));

            var sut = _fixture.GetSut();

            await sut.CaptureEventAsync(expectedEvent);

            _fixture.SentryOptions.DiagnosticLogger.Received(1).Log(SentryLevel.Error,
                "Sentry rejected the event {0}. Status code: {1}. Sentry response: {2}", null,
                Arg.Is<object[]>(p => p[0].ToString() == expectedEvent.EventId.ToString()
                                      && p[1].ToString() == expectedCode.ToString()
                                      && p[2].ToString() == expectedMessage));
        }

        [Fact]
        public async Task CaptureEventAsync_ResponseNotOkNoMessage_LogsError()
        {
            const HttpStatusCode expectedCode = HttpStatusCode.BadGateway;
            var expectedEvent = new SentryEvent();

            _fixture.SentryOptions.Debug = true;
            _ = _fixture.SentryOptions.DiagnosticLogger.IsEnabled(SentryLevel.Error).Returns(true);
            _ = _fixture.HttpMessageHandler.VerifyableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                    .Returns(_ => SentryResponses.GetErrorResponse(expectedCode, null));

            var sut = _fixture.GetSut();

            await sut.CaptureEventAsync(expectedEvent);

            _fixture.SentryOptions.DiagnosticLogger.Received(1).Log(SentryLevel.Error,
                "Sentry rejected the event {0}. Status code: {1}. Sentry response: {2}", null,
                Arg.Is<object[]>(p => p[0].ToString() == expectedEvent.EventId.ToString()
                                      && p[1].ToString() == expectedCode.ToString()
                                      && p[2].ToString() == HttpTransport.NoMessageFallback));
        }

        [Fact]
        public void CreateRequest_AuthHeader_Invoked()
        {
            var callbackInvoked = false;
            _fixture.AddAuth = headers =>
            {
                Assert.NotNull(headers);
                callbackInvoked = true;
            };

            var sut = _fixture.GetSut();

            var evt = new SentryEvent();
            _ = sut.CreateRequest(evt);

            Assert.True(callbackInvoked);
        }

        [Fact]
        public void CreateRequest_RequestMethod_Post()
        {
            var sut = _fixture.GetSut();

            var evt = new SentryEvent();
            var actual = sut.CreateRequest(evt);

            Assert.Equal(HttpMethod.Post, actual.Method);
        }

        [Fact]
        public void CreateRequest_SentryUrl_FromOptions()
        {
            var sut = _fixture.GetSut();

            var evt = new SentryEvent();
            var actual = sut.CreateRequest(evt);

            var uri = Dsn.Parse(_fixture.SentryOptions.Dsn!).GetStoreEndpointUri();

            Assert.Equal(uri, actual.RequestUri);
        }

        [Fact]
        public async Task CreateRequest_Content_IncludesEvent()
        {
            var sut = _fixture.GetSut();

            var evt = new SentryEvent();
            var actual = sut.CreateRequest(evt);

            Assert.Contains(evt.EventId.ToString(), await actual.Content.ReadAsStringAsync());
        }
    }
}
