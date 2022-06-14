
#if NET6_0
using System.Net.Http;
using Microsoft.AspNetCore.Builder;

public class VersioningTests
{
    [Fact]
    public async Task Foo()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.MapGet("/", () => "Hello World!");

        var task = app.StartAsync();

        var httpClient = new HttpClient();
        var message = await httpClient.GetAsync(app.Urls.First());
        await app.StopAsync();
    }
}

#endif
