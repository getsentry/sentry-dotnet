#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Sentry.AspNetCore.Tests;

[UsesVerify]
public class WebIntegrationTests
{
    private readonly TestOutputDiagnosticLogger _logger;

    public WebIntegrationTests(ITestOutputHelper output)
    {
        _logger = new(output);
    }

    [Fact]
    public async Task Versioning()
    {
        // Arrange
        var transport = new RecordingTransport();
        using var server = new TestServer(
            new WebHostBuilder()
                .UseSentry(o =>
                {
                    o.Dsn = ValidDsn;
                    o.TracesSampleRate = 1;
                    o.Transport = transport;
                    o.Debug = true;
                    o.DiagnosticLogger = _logger;

                    // Disable process exit flush to resolve "There is no currently active test." errors.
                    o.DisableAppDomainProcessExitFlush();
                })
                .ConfigureServices(services =>
                {
                    services.AddRouting();

                    var controllers = services.AddControllers();
                    controllers.UseSpecificControllers(typeof(VersionController));

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
                    app.UseEndpoints(routeBuilder => routeBuilder.MapControllers());
                }));

        var client = server.CreateClient();

        // Act
        var result = await client.GetStringAsync("/v1.1/Target");

        // dispose will ultimately trigger the background worker to flush
        server.Dispose();

        await Verify(new {result, transport.Payloads})
            .IgnoreStandardSentryMembers()
            .ScrubAspMembers()
            .UniqueForTargetFramework();
    }

    [ApiController]
    [Route("v{version:apiVersion}/Target")]
    [ApiVersion("1.1")]
    public class VersionController : ControllerBase
    {
        [HttpGet]
        public string Method()
        {
            return "Hello world";
        }
    }

    [Fact]
    public async Task PreFlightIgnoresTransaction()
    {
        // Arrange
        var transport = new RecordingTransport();
        using var server = new TestServer(
            new WebHostBuilder()
                .UseSentry(o =>
                {
                    o.Dsn = ValidDsn;
                    o.TracesSampleRate = 1;
                    o.Transport = transport;
                    o.Debug = true;
                    o.DiagnosticLogger = _logger;

                    // Disable process exit flush to resolve "There is no currently active test." errors.
                    o.DisableAppDomainProcessExitFlush();
                })
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddCors(options =>
                    {
                        options.AddDefaultPolicy(
                            builder =>
                            {
                                builder.AllowAnyOrigin();
                                builder.AllowAnyHeader();
                                builder.AllowAnyMethod();
                            });
                    });

                    var controllers = services.AddControllers();
                    controllers.UseSpecificControllers(typeof(TransactionController));
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseCors();
                    app.UseEndpoints(_ => _.MapControllers());
                }));

        var client = server.CreateClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Options, "/Target");
        request.Headers.Add("Access-Control-Request-Headers", "origin");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Origin", "https://sentry.io/foo");
        var result = await client.SendAsync(request);

        // dispose will ultimately trigger the background worker to flush
        server.Dispose();

        await Verify(new {result, transport.Payloads})
            .IgnoreStandardSentryMembers()
            .ScrubAspMembers();
    }

    [ApiController]
    [Route("Target")]
    public class TransactionController : ControllerBase
    {
        [HttpGet]
        public string Method()
        {
            SentrySdk.ConfigureScope(scope => scope.TransactionName = "TheTransaction");
            return "Hello world";
        }
    }
}
#endif
