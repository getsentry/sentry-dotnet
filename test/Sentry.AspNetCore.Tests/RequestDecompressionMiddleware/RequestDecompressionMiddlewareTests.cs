using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RequestDecompression;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Sentry.AspNetCore.TestUtils;

namespace Sentry.AspNetCore.Tests.RequestDecompressionMiddleware;

public class RequestDecompressionMiddlewareTests
{
    private class Fixture : IDisposable
    {
        private TestServer _server;
        private HttpClient _client;
        private IRequestDecompressionProvider _provider;
        public Exception LastException { get; private set; }

        private IWebHostBuilder GetBuilder()
        {
            var exceptionProcessor = Substitute.For<ISentryEventExceptionProcessor>();
            exceptionProcessor.Process(Arg.Do<Exception>(e => LastException = e),
                Arg.Any<SentryEvent>());

            var sentry = FakeSentryServer.CreateServer();
            var sentryHttpClient = sentry.CreateClient();
            return new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    if (_provider is not null)
                    {
                        services.AddSingleton(_provider);
                    }
                    else
                    {
                        services.AddRequestDecompression();
                    }
                })
                .UseSentry(o =>
                {
                    o.Dsn = ValidDsn;
                    o.MaxRequestBodySize = RequestSize.Always;
                    o.SentryHttpClientFactory = new DelegateHttpClientFactory(_ => sentryHttpClient);
                    o.AddExceptionProcessor(exceptionProcessor);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapPost("/echo", async context =>
                        {
                            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
                            var body = await reader.ReadToEndAsync();
                            await context.Response.WriteAsync(body);
                        });
                    });
                });
        }

        public void FakeDecompressionError()
        {
            _provider = new FlakyDecompressionProvider();
        }

        private class FlakyDecompressionProvider : IRequestDecompressionProvider
        {
            public Stream GetDecompressionStream(HttpContext context)
            {
                // Simulate a decompression error
                throw new InvalidDataException("Flaky decompression error");
            }
        }

        public HttpClient GetSut()
        {
            _server = new TestServer(GetBuilder());
            _client = _server.CreateClient();
            return _client;
        }

        public void Dispose()
        {
            _client.Dispose();
            _server.Dispose();
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public async Task AddRequestDecompression_PlainBodyContent_IsUnaltered()
    {
        var client = _fixture.GetSut();

        var json = "{\"Foo\":\"Bar\"}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/echo", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(json, responseBody);
    }

    [Fact]
    public async Task AddRequestDecompression_CompressedBodyContent_IsDecompressed()
    {
        var client = _fixture.GetSut();

        var json = "{\"Foo\":\"Bar\"}";
        var gzipped = CompressGzip(json);
        var content = new ByteArrayContent(gzipped);
        content.Headers.Add("Content-Encoding", "gzip");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await client.PostAsync("/echo", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(json, responseBody);
    }

    [Fact]
    public async Task DecompressionError_SentryCapturesException()
    {
        // Arrange
        _fixture.FakeDecompressionError();
        var client = _fixture.GetSut();

        var json = "{\"Foo\":\"Bar\"}";
        var gzipped = CompressGzip(json);
        var content = new ByteArrayContent(gzipped);
        content.Headers.Add("Content-Encoding", "gzip");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        // Act
        try
        {
            _ = await client.PostAsync("/echo", content);
        }
        catch
        {
            // We're expecting an exception here... what we're interested in is what happens on the server
        }

        // Assert
        using (new AssertionScope())
        {
            _fixture.LastException.Should().NotBeNull();
            _fixture.LastException?.Message.Should().Be("Flaky decompression error");
        }
    }

    private static byte[] CompressGzip(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }
        return output.ToArray();
    }
}
