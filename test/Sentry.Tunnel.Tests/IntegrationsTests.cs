using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Sentry.Tunnel.Tests
{
    [Collection("SentryTunnelCollection")]
    public partial class IntegrationsTests
    {
        private readonly TestServer _server;
        private HttpClient _httpClient;
        private MockHttpMessageHandler _httpMessageHandler;

        public IntegrationsTests()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(s =>
                {
                    s.AddSentryTunneling("sentry.mywebsite.com");
                    _httpMessageHandler = new MockHttpMessageHandler("{}", HttpStatusCode.OK);
                    _httpClient = new HttpClient(_httpMessageHandler);
                    var factory = Substitute.For<IHttpClientFactory>();
                    factory.CreateClient(Arg.Any<string>()).Returns(_httpClient);
                    s.AddSingleton(factory);
                })
                .Configure(app => { app.UseSentryTunneling(); });
            _server = new TestServer(builder);
        }

        [Fact]
        public async Task TunnelMiddleware_CanForwardValidEnvelope()
        {
            var requestMessage = new HttpRequestMessage(new HttpMethod("POST"), "/tunnel");
            requestMessage.Content = new StringContent(
                @"{""sent_at"":""2021-01-01T00:00:00.000Z"",""sdk"":{""name"":""sentry.javascript.browser"",""version"":""6.8.0""},""dsn"":""https://dns@sentry.io/1""}
{""type"":""session""}
{""sid"":""fda00e933162466c849962eaea0cfaff""}");
            await _server.CreateClient().SendAsync(requestMessage);

            Assert.Equal(1, _httpMessageHandler.NumberOfCalls);
        }

        [Fact]
        public async Task TunnelMiddleware_DoesNotForwardEnvelopeWithoutDsn()
        {
            var requestMessage = new HttpRequestMessage(new HttpMethod("POST"), "/tunnel");
            requestMessage.Content = new StringContent(@"{}
{""type"":""session""}
{""sid"":""fda00e933162466c849962eaea0cfaff""}");
            await _server.CreateClient().SendAsync(requestMessage);

            Assert.Equal(0, _httpMessageHandler.NumberOfCalls);
        }

        [Fact]
        public async Task TunnelMiddleware_DoesNotForwardEnvelopeToArbitraryHost()
        {
            var requestMessage = new HttpRequestMessage(new HttpMethod("POST"), "/tunnel");
            requestMessage.Content = new StringContent(
                @"{""sent_at"":""2021-01-01T00:00:00.000Z"",""sdk"":{""name"":""sentry.javascript.browser"",""version"":""6.8.0""},""dsn"":""https://dns@evil.com/1""}
{""type"":""session""}
{""sid"":""fda00e933162466c849962eaea0cfaff""}");
            await _server.CreateClient().SendAsync(requestMessage);

            Assert.Equal(0, _httpMessageHandler.NumberOfCalls);
        }

        [Fact]
        public async Task TunnelMiddleware_CanForwardEnvelopeToWhiteListedHost()
        {
            var requestMessage = new HttpRequestMessage(new HttpMethod("POST"), "/tunnel");
            requestMessage.Content = new StringContent(
                @"{""sent_at"":""2021-01-01T00:00:00.000Z"",""sdk"":{""name"":""sentry.javascript.browser"",""version"":""6.8.0""},""dsn"":""https://dns@sentry.mywebsite.com/1""}
{""type"":""session""}
{""sid"":""fda00e933162466c849962eaea0cfaff""}");
            await _server.CreateClient().SendAsync(requestMessage);

            Assert.Equal(1, _httpMessageHandler.NumberOfCalls);
        }
    }
}
