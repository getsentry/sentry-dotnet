using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Sentry.Tunnel.Tests;

public class IntegrationsTests : IDisposable
{
    private readonly TestServer _server;
    private HttpClient _httpClient;
    private MockHttpMessageHandler _httpMessageHandler;
    private const string FakeRequestIp = "192.168.200.200";

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
            .Configure(app =>
            {
                app.Use((context, next) =>
                {
                    // The context doesn't get sent by TestServer automatically... so we fake a remote request here
                    context.Connection.RemoteIpAddress = IPAddress.Parse(FakeRequestIp);
                    return next();
                });
                app.UseSentryTunneling();
            });
        _server = new TestServer(builder);
    }

    [Theory]
    [InlineData("sentry.io")]
    [InlineData("ingest.sentry.io")]
    [InlineData("o12345.ingest.sentry.io")]
    public async Task TunnelMiddleware_CanForwardValidEnvelope(string host)
    {
        var requestMessage = new HttpRequestMessage(new HttpMethod("POST"), "/tunnel")
        {
            Content = new StringContent(
            @"{""sent_at"":""2021-01-01T00:00:00.000Z"",""sdk"":{""name"":""sentry.javascript.browser"",""version"":""6.8.0""},""dsn"":""https://dns@" + host + @"/1""}
{""type"":""session""}
{""sid"":""fda00e933162466c849962eaea0cfaff""}")
        };
        await _server.CreateClient().SendAsync(requestMessage);

        Assert.Equal(1, _httpMessageHandler.NumberOfCalls);
    }

    [Fact]
    public async Task TunnelMiddleware_DoesNotForwardEnvelopeWithoutDsn()
    {
        var requestMessage = new HttpRequestMessage(new HttpMethod("POST"), "/tunnel")
        {
            Content = new StringContent(@"{}
{""type"":""session""}
{""sid"":""fda00e933162466c849962eaea0cfaff""}")
        };
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
        var requestMessage = new HttpRequestMessage(new HttpMethod("POST"), "/tunnel")
        {
            Content = new StringContent(
            @"{""sent_at"":""2021-01-01T00:00:00.000Z"",""sdk"":{""name"":""sentry.javascript.browser"",""version"":""6.8.0""},""dsn"":""https://dns@sentry.mywebsite.com/1""}
{""type"":""session""}
{""sid"":""fda00e933162466c849962eaea0cfaff""}")
        };
        await _server.CreateClient().SendAsync(requestMessage);

        Assert.Equal(1, _httpMessageHandler.NumberOfCalls);
    }

    [Fact]
    public async Task TunnelMiddleware_XForwardedFor_RetainsOriginIp()
    {
        // Arrange: Create a request with X-Forwarded-For header
        var requestMessage = new HttpRequestMessage(new HttpMethod("POST"), "/tunnel")
        {
            Content = new StringContent(
                @"{""sent_at"":""2021-01-01T00:00:00.000Z"",""sdk"":{""name"":""sentry.javascript.browser"",""version"":""6.8.0""},""dsn"":""https://dns@sentry.io/1""}
{""type"":""session""}
{""sid"":""fda00e933162466c849962eaea0cfaff""}")
        };
        const string originalForwardedFor = "192.168.1.100, 10.0.0.1";
        requestMessage.Headers.Add("X-Forwarded-For", originalForwardedFor);

        // Act
        await _server.CreateClient().SendAsync(requestMessage);

        // Assert
        Assert.Equal(1, _httpMessageHandler.NumberOfCalls);

        var forwardedRequest = _httpMessageHandler.LastRequest;
        Assert.NotNull(forwardedRequest);

        Assert.True(forwardedRequest.Headers.Contains("X-Forwarded-For"));
        var forwardedForHeader = forwardedRequest.Headers.GetValues("X-Forwarded-For").FirstOrDefault();
        Assert.NotNull(forwardedForHeader);
        forwardedForHeader.Should().Be($"{originalForwardedFor}, {FakeRequestIp}");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _httpMessageHandler.Dispose();
        _server.Dispose();
    }
}
