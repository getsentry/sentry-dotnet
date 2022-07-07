
#if NET6_0
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

[UsesVerify]
public class VersioningTests
{
    [Fact]
    public async Task Simple()
    {
        var transport = new RecordingTransport();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseSentry(_ =>
        {
            _.TracesSampleRate = 1;
            _.Transport = transport;
            _.Dsn = ValidDsn;
        });
        var services = builder.Services;
        var controllers = services.AddControllers();
        controllers.UseSpecificControllers(typeof(TargetController));
        services.AddApiVersioning(_ =>
        {
            _.DefaultApiVersion = new ApiVersion(1, 0);
            _.AssumeDefaultVersionWhenUnspecified = true;
            _.ReportApiVersions = true;
        });

        await using var app = builder.Build();
        app.UseSentryTracing();
        app.MapControllers();

        await app.StartAsync();

        using var client = new HttpClient();
        var result = await client.GetStringAsync($"{app.Urls.First()}/v1.1/Target");

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
