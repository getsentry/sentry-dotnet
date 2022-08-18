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
public class OptionsTests
{
    private readonly TestOutputDiagnosticLogger _logger;

    public OptionsTests(ITestOutputHelper output)
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
                controllers.UseSpecificControllers(typeof(OptionsController));
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseSentryTracing();
                app.UseEndpoints(_ => _.MapControllers());
            }));

        var client = server.CreateClient();

        // Act
        var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Options, "/Target");
        var result = await client.SendAsync(request);

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
            .ScrubLinesWithReplace(_ => _.Split(new[] {" (Sentry.AspNetCore.Tests) "}, StringSplitOptions.None)[0]);
    }

    [ApiController]
    [Route("Target")]
    public class OptionsController : ControllerBase
    {
        [HttpOptions]
        public string Method()
        {   SentrySdk.ConfigureScope(scope => scope.TransactionName = "TheTransaction");
            return "Hello world";
        }
    }
}

#endif
