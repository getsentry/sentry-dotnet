
#if NET6_0
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

public class VersioningTests
{
    [Fact]
    public async Task Foo()
    {
        var builder = WebApplication.CreateBuilder();
       // builder.Services.AddControllers();
        await using var app = builder.Build();
      //  app.MapControllers();

        app.MapGet("/", () => "a");
        var task = app.RunAsync();

        var httpClient = new HttpClient();
        var uri = $"{app.Urls.First()}";
        var message = await httpClient.GetStringAsync(uri);
        Assert.Equal("Hello world",message);
        await app.StopAsync();
    }

    [ApiController]
    [Route("Controller")]
    public class MyController : ControllerBase
    {
        [HttpGet]
        public string Method()
        {
            return "Hello world";
        }
    }
}

#endif
