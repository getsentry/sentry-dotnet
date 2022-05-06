using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Sentry.AspNetCore;

/// <summary>
/// Middleware that can forward Sentry envelopes.
/// </summary>
/// <seealso href="https://docs.sentry.io/platforms/javascript/troubleshooting/#dealing-with-ad-blockers"/>
public class SentryTunnelMiddleware : IMiddleware
{
    private readonly string[] _allowedHosts;
    private Lazy<string> Version => new(() => (GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty));

    /// <summary>
    /// Middleware that can forward Sentry envelopes.
    /// </summary>
    /// <seealso href="https://docs.sentry.io/platforms/javascript/troubleshooting/#dealing-with-ad-blockers"/>
    public SentryTunnelMiddleware(string[] allowedHosts)
    {
        _allowedHosts = new[] { "sentry.io" }.Concat(allowedHosts).ToArray();
    }

    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Method == "OPTIONS")
        {
            context.Response.Headers.Add("Access-Control-Allow-Origin", new[] { (string)context.Request.Headers["Origin"] });
            context.Response.Headers.Add("Access-Control-Allow-Headers", new[] { "Origin, X-Requested-With, Content-Type, Accept" });
            context.Response.Headers.Add("Access-Control-Allow-Methods", new[] { "POST, OPTIONS" });
            context.Response.Headers.Add("Access-Control-Allow-Credentials", new[] { "true" });
            context.Response.StatusCode = 200;
            return;
        }

        var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = httpClientFactory.CreateClient("SentryTunnel");
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Sentry.NET_Tunnel", Version.Value));
        var ms = new MemoryStream();
        await context.Request.Body.CopyToAsync(ms).ConfigureAwait(false);
        ms.Position = 0;
        using var reader = new StreamReader(ms);
        var header = await reader.ReadLineAsync().ConfigureAwait(false);
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
                await context.Response.WriteAsync("Invalid DSN JSON supplied").ConfigureAwait(false);
                return;
            }
            if (headerJson.TryGetValue("dsn", out var dsnString) && Uri.TryCreate(dsnString.ToString(), UriKind.Absolute, out var dsn) && _allowedHosts.Contains(dsn.Host))
            {
                var projectId = dsn.AbsolutePath.Trim('/');
                ms.Position = 0;
                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri($"https://{dsn.Host}/api/{projectId}/envelope/"),
                    Method = HttpMethod.Post,
                    Content = new StreamContent(ms),
                };
                var clientIp = context.Connection?.RemoteIpAddress?.ToString();
                if (clientIp != null)
                {
                    request.Headers.Add("X-Forwarded-For", context.Connection?.RemoteIpAddress?.ToString());
                }
                var responseMessage = await client.SendAsync(request).ConfigureAwait(false);
                // We send the response back to the client, whatever it was
                context.Response.Headers["content-type"] = "application/json";
                context.Response.StatusCode = (int)responseMessage.StatusCode;
                await responseMessage.Content.CopyToAsync(context.Response.Body).ConfigureAwait(false);
            }
        }
        catch (JsonException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid DSN JSON supplied").ConfigureAwait(false);
        }
        catch (ArgumentNullException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Received empty body").ConfigureAwait(false);
        }
    }
}
