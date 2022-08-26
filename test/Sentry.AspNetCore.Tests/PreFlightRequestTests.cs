#if NET6_0
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Sentry.Testing;

namespace Sentry.AspNetCore.Tests;

[UsesVerify]
public class PreflighRequests
{
    private readonly TestOutputDiagnosticLogger _logger;

    public PreflighRequests(ITestOutputHelper output)
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
                controllers.UseSpecificControllers(typeof(OptionsController));
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseSentryTracing();
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
    [Route("Target")]
    public class OptionsController : ControllerBase
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
