#if NET6_0 || NETCOREAPP3_1
using System.Net.Http;
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

                // Disable process exit flush to resolve "There is no currently active test." errors.
                o.DisableAppDomainProcessExitFlush();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();

                var controllers = services.AddControllers();
#if NET6_0_OR_GREATER
                controllers.UseSpecificControllers(typeof(TargetController));

                services.AddApiVersioning(versioningOptions =>
                {
                    versioningOptions.DefaultApiVersion = new ApiVersion(1, 0);
                    versioningOptions.AssumeDefaultVersionWhenUnspecified = true;
                    versioningOptions.ReportApiVersions = true;
                });
#endif
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
            .IgnoreMembers("ConnectionId", "RequestId")
            .ScrubLinesWithReplace(_=>_.Split(new []{" (Sentry.AspNetCore.Tests) "},StringSplitOptions.None)[0]);
    }
    [Fact]

    public async Task SimpleOptionRoute()
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

                // Disable process exit flush to resolve "There is no currently active test." errors.
                o.DisableAppDomainProcessExitFlush();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();

                var controllers = services.AddControllers();
#if NET6_0_OR_GREATER
                controllers.UseSpecificControllers(typeof(TargetController));

                services.AddApiVersioning(versioningOptions =>
                {
                    versioningOptions.DefaultApiVersion = new ApiVersion(1, 0);
                    versioningOptions.AssumeDefaultVersionWhenUnspecified = true;
                    versioningOptions.ReportApiVersions = true;
                });
#endif
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseSentryTracing();
                app.UseEndpoints(routeBuilder => routeBuilder.MapControllers());
            }));

        var client = server.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/v1.1/Target");

        // Act
        var result = await client.SendAsync(request);

        // dispose will ultimately trigger the background worker to flush
        server.Dispose();

        // Assert
        var payloads = transport.Envelopes
            .SelectMany(x => x.Items)
            .Select(x => x.Payload)
            .ToList();

        await Verify(new { result, payloads })
            .IgnoreStandardSentryMembers()
            .IgnoreMembers("ConnectionId", "RequestId")
            .ScrubLinesWithReplace(_ => _.Split(new[] { " (Sentry.AspNetCore.Tests) " }, StringSplitOptions.None)[0]);
    }

    [ApiController]
    [Route("v{version:apiVersion}/Target")]
#if NET6_0_OR_GREATER
    [ApiVersion("1.1")]
#endif
    public class TargetController : ControllerBase
    {
        [HttpGet]
        public string Method()
        {
            return "Hello world";
        }


        [HttpOptions]
        public string MethodOption()
        {
            return "Hello world";
        }
    }
}

#endif
