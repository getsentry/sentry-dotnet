using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Sentry.Tunnel
{
    public class SentryTunnelMiddleware : IMiddleware
    {
        private readonly string[] _allowedHosts;

        public SentryTunnelMiddleware(string[] allowedHosts)
        {
            _allowedHosts = new[] {"sentry.io"}.Concat(allowedHosts).ToArray();
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Method == "OPTIONS")
            {
                context.Response.Headers.Add("Access-Control-Allow-Origin", new[] {(string) context.Request.Headers["Origin"]});
                context.Response.Headers.Add("Access-Control-Allow-Headers", new[] {"Origin, X-Requested-With, Content-Type, Accept"});
                context.Response.Headers.Add("Access-Control-Allow-Methods", new[] {"POST, OPTIONS"});
                context.Response.Headers.Add("Access-Control-Allow-Credentials", new[] {"true"});
                context.Response.StatusCode = 200;
                return;
            }

            var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
            var client = httpClientFactory.CreateClient("SentryTunnel");
            var ms = new MemoryStream();
            await context.Request.Body.CopyToAsync(ms);
            ms.Position = 0;
            using (var reader = new StreamReader(ms))
            {
                var header = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(header))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                try
                {
                    var headerJson = JsonSerializer.Deserialize<Dictionary<string, object>>(header);
                    if (headerJson == null)
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Invalid DSN JSON supplied");
                        return;
                    }
                    if (headerJson.TryGetValue("dsn", out var dsnString) && Uri.TryCreate(dsnString.ToString(), UriKind.Absolute, out var dsn) && _allowedHosts.Contains(dsn.Host))
                    {
                        var projectId = dsn.AbsolutePath.Trim('/');
                        ms.Position = 0;
                        var responseMessage = await client.PostAsync($"https://{dsn.Host}/api/{projectId}/envelope/",
                            new StreamContent(ms));
                        context.Response.Headers["content-type"] = "application/json";
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        await responseMessage.Content.CopyToAsync(context.Response.Body);
                    }
                }
                catch(JsonException)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Invalid DSN JSON supplied");
                }
                catch(ArgumentNullException)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Received empty body");
                }
            }
        }
    }
}
