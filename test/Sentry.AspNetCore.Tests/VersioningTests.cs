
#if NET6_0
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

[UsesVerify]
public class VersioningTests
{
    [Fact]
    public async Task Simple()
    {
        var builder = WebApplication.CreateBuilder();

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

        app.MapControllers();


        await app.StartAsync();

        using var client = new HttpClient();
        var result = client.GetStringAsync($"{app.Urls.First()}/v1.1/Target");

        await Verify(result);
    }

    [ApiController]
    [Route("[controller]")]
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
