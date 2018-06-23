using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sentry.Http;
using Sentry.Internal.Http;
using Sentry.Testing;
using Sentry.Tests.Helpers;
using Xunit;

namespace Sentry.Tests.Internals.Http
{
    public class HttpTransportTests
    {
        private class Fixture
        {
            public HttpOptions HttpOptions { get; set; } = new HttpOptions(new Uri("https://sentry.yo/store"));
            public HttpClient HttpClient { get; set; }
            public MockableHttpMessageHandler HttpMessageHandler { get; set; } = Substitute.For<MockableHttpMessageHandler>();
            public HttpContent HttpContent { get; set; } = Substitute.For<HttpContent>();
            public Action<HttpRequestHeaders> AddAuth { get; set; } = _ => { };

            public Fixture()
            {
                HttpMessageHandler.VerifyableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                    .Returns(_ => SentryResponses.GetOkResponse());

                HttpClient = new HttpClient(HttpMessageHandler);
            }

            public HttpTransport GetSut() => new HttpTransport(HttpOptions, HttpClient, AddAuth);
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public async Task CaptureEventAsync_NullEvent_NoOp()
        {
            var sut = _fixture.GetSut();
            await sut.CaptureEventAsync(null);
            await _fixture.HttpMessageHandler.DidNotReceive()
                .VerifyableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CaptureEventAsync_CancellationToken_PassedToClient()
        {
            var source = new CancellationTokenSource();
            source.Cancel();
            var token = source.Token;
            var sut = _fixture.GetSut();

            await sut.CaptureEventAsync(
                new SentryEvent(
                    id: SentryResponses.ResponseId,
                    populate: false),
                token);

            await _fixture.HttpMessageHandler
                .Received(1)
                .VerifyableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Is<CancellationToken>(c => c.IsCancellationRequested));
        }

        [Fact]
        public async Task CaptureEventAsync_ResponseNotOkWithMessage_CallbackFired()
        {
            const HttpStatusCode expectedCode = HttpStatusCode.BadGateway;
            const string expectedMessage = "Bad Gateway!";
            var expectedEvent = new SentryEvent(populate: false);

            var callbackInvoked = false;
            _fixture.HttpOptions.HandleFailedEventSubmission = (e, c, m) =>
            {
                Assert.Same(e, expectedEvent);
                Assert.Equal(expectedMessage, m);
                Assert.Equal(expectedCode, c);
                callbackInvoked = true;
            };
            _fixture.HttpMessageHandler.VerifyableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(_ => SentryResponses.GetErrorResponse(expectedCode, expectedMessage));

            var sut = _fixture.GetSut();

            await sut.CaptureEventAsync(expectedEvent);

            Assert.True(callbackInvoked);
        }

        [Fact]
        public async Task CaptureEventAsync_ResponseNotOkNoMessage_CallbackFired()
        {
            const HttpStatusCode expectedCode = HttpStatusCode.BadGateway;
            var expectedEvent = new SentryEvent(populate: false);

            var callbackInvoked = false;
            _fixture.HttpOptions.HandleFailedEventSubmission = (e, c, m) =>
            {
                Assert.Same(e, expectedEvent);
                Assert.Equal(HttpTransport.NoMessageFallback, m);
                Assert.Equal(expectedCode, c);
                callbackInvoked = true;
            };
            _fixture.HttpMessageHandler.VerifyableSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(_ => SentryResponses.GetErrorResponse(expectedCode, null));

            var sut = _fixture.GetSut();

            await sut.CaptureEventAsync(expectedEvent);

            Assert.True(callbackInvoked);
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

            var evt = new SentryEvent(populate: false);
            sut.CreateRequest(evt);

            Assert.True(callbackInvoked);
        }

        [Fact]
        public void CreateRequest_RequestMethod_Post()
        {
            var sut = _fixture.GetSut();

            var evt = new SentryEvent(populate: false);
            var actual = sut.CreateRequest(evt);

            Assert.Equal(HttpMethod.Post, actual.Method);
        }

        [Fact]
        public void CreateRequest_SentryUrl_FromOptions()
        {
            var sut = _fixture.GetSut();

            var evt = new SentryEvent(populate: false);
            var actual = sut.CreateRequest(evt);

            Assert.Equal(_fixture.HttpOptions.SentryUri, actual.RequestUri);
        }

        [Fact]
        public async Task CreateRequest_Content_IncludesEvent()
        {
            var sut = _fixture.GetSut();

            var evt = new SentryEvent(
                populate: false);
            var actual = sut.CreateRequest(evt);

            Assert.Contains(evt.EventId.ToString("N"), await actual.Content.ReadAsStringAsync());
        }
    }
}
