using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace Sentry.AspNetCore.Tests.RequestDecompressionMiddleware;

public class RequestDecompressionMiddlewareTests
{
    [Fact]
    public async Task AddRequestDecompression_CompressedBodyContent_IsDecompressed()
    {
        // Arrange
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddRequestDecompression(); // No options needed for default gzip support
            })
            .UseSentry(o =>
            {
                o.Dsn = ValidDsn;
                o.MaxRequestBodySize = RequestSize.Always;
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

        using var server = new TestServer(builder);
        using var client = server.CreateClient();

        var json = "{\"Foo\":\"Bar\"}";
        var gzipped = CompressGzip(json);
        var content = new ByteArrayContent(gzipped);
        content.Headers.Add("Content-Encoding", "gzip");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        // Act
        var response = await client.PostAsync("/echo", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(json, responseBody);
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
