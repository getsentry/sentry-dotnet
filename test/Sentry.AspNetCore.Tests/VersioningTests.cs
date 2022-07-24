#if NET6_0
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Sentry.Testing;

namespace Sentry.AspNetCore.Tests;

[UsesVerify]
public class VersioningTests
{
    private readonly TestOutputDiagnosticLogger _logger;

    public VersioningTests(ITestOutputHelper output)
    {
        _logger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public async Task Simple()
    {
        // Arrange
        var transport = new RecordingTransport();
        using var server = new TestServer(new WebHostBuilder()
            .UseSentry(o =>
            {
                o.Dsn = ValidDsn;
                o.TracesSampleRate = 1;
                o.Transport = transport;
                o.Debug = true;
                o.DiagnosticLogger = _logger;
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();

                var controllers = services.AddControllers();
                controllers.UseSpecificControllers(typeof(TargetController));

                services.AddApiVersioning(versioningOptions =>
                {
                    versioningOptions.DefaultApiVersion = new ApiVersion(1, 0);
                    versioningOptions.AssumeDefaultVersionWhenUnspecified = true;
                    versioningOptions.ReportApiVersions = true;
                });
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseSentryTracing();
                app.UseEndpoints(routeBuilder => routeBuilder.MapControllers());
            }));

        var client = server.CreateClient();

        // Act
        var result = await client.GetStringAsync("/v1.1/Target");

        // dispose will ultimately trigger the background worker to flush
        server.Dispose();

        // Assert
        var payloads = transport.Envelopes
            .SelectMany(x => x.Items)
            .Select(x => x.Payload)
            .ToList();

        await Verify(new {result, payloads})
            .IgnoreStandardSentryMembers()
            .ScrubLinesContaining("Message: Executed action ")
            .IgnoreMembers("ConnectionId", "RequestId");
    }

    [ApiController]
    [Route("v{version:apiVersion}/Target")]
    [ApiVersion("1.1")]
    public class TargetController : ControllerBase
    {
        [HttpGet]
        public string Method()
        {
            return "Hello world";
        }
    }
}

#endif
