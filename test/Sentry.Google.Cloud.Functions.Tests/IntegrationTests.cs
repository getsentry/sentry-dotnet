using System.IO.Compression;
using System.Net;
using System.Net.Http;
using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sentry.AspNetCore;
using Sentry.Testing;

namespace Sentry.Google.Cloud.Functions.Tests;

public class IntegrationTests
{
    public static string ExpectedMessage = Guid.NewGuid().ToString();

    [Fact]
    public async Task SentryIntegrationTest_CaptureUnhandledException()
    {
        var evt = new ManualResetEventSlim();

        var requests = new List<string>();
        async Task VerifyAsync(HttpRequestMessage message)
        {
            var content = await message.Content.ReadAsStringAsync();
            requests.Add(content);
            evt.Set();
        }

        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHost(webHostBuilder => webHostBuilder
                .ConfigureServices((_, services) =>
                {
                    services.Configure<SentryAspNetCoreOptions>(o =>
                    {
                        // So we can assert on the payload without the need to Gzip decompress
                        o.RequestBodyCompressionLevel = CompressionLevel.NoCompression;
                        o.CreateHttpClientHandler = () => new CallbackHttpClientHandler(VerifyAsync);
                    });
                    services.AddFunctionTarget<FailingFunction>();
                })
                // Based on: https://github.com/GoogleCloudPlatform/functions-framework-dotnet/blob/a8a34526053c40e84ff096a43b1d357ea4d3be6c/src/Google.Cloud.Functions.Hosting.Tests/FunctionsStartupTest.cs#L117
                .UseFunctionsStartup(new SentryStartup())
                .Configure((context, app) => app.UseFunctionsFramework(context))
                .ConfigureAppConfiguration(c => c.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Sentry:Dsn", "https://key@sentry.io/project")
                }))
                .UseTestServer())
            .Build();

        await host.StartAsync();

        using var testServer = host.GetTestServer();
        using var client = testServer.CreateClient();
        try
        {
            using var response = await client.GetAsync("/");
        }
        catch (Exception e) when (e.Message == ExpectedMessage)
        {
            // Synchronizing in the tests because `OnCompleted` is not being called with TestServer.
            Assert.True(evt.Wait(TimeSpan.FromSeconds(3)));
            Assert.True(requests.Any(p => p.Contains(ExpectedMessage)),
                "Expected error to be captured");
            Assert.True(requests.All(p => p.Contains("sentry.dotnet.google-cloud-function")),
                "Expected SDK name to be in the payload");
            return; // pass
        }
        Assert.False(true, "Exception should bubble from Middleware");
    }

    public class FailingFunction : IHttpFunction
    {
        public Task HandleAsync(HttpContext context) => throw new Exception(ExpectedMessage);
    }
}
