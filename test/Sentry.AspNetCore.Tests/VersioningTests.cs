
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

        var controllers = builder.Services.AddControllers();
        controllers.UseSpecificControllers(typeof(TargetController));
        await using var app = builder.Build();

        app.MapControllers();

        await app.StartAsync();

        using var client = new HttpClient();
        var result = client.GetStringAsync($"{app.Urls.First()}/Target");

        await Verify(result);
    }

    [ApiController]
    [Route("[controller]")]
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
