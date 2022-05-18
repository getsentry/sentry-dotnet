using System.ComponentModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Sentry.Tunnel;

/// <summary>
/// Extension methods to add Sentry ingestion tunnel.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryTunnelingApplicationBuilderExtensions
{
    /// <summary>
    /// Adds and configures the Sentry tunneling middleware.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="hostnames">The extra hostnames to be allowed for the tunneling. sentry.io is allowed by default; add your own Sentry domain if you use a self-hosted Sentry or Relay.</param>
    public static void AddSentryTunneling(this IServiceCollection services, params string[] hostnames) =>
        services.AddScoped(_ => new SentryTunnelMiddleware(hostnames));

    /// <summary>
    /// Adds the <see cref="SentryTunnelMiddleware"/> to the pipeline.
    /// </summary>
    /// <param name="builder">The app builder.</param>
    /// <param name="path">The path to listen for Sentry envelopes.</param>
    public static void UseSentryTunneling(this IApplicationBuilder builder, string path = "/tunnel") =>
        builder.Map(path, applicationBuilder =>
            applicationBuilder.UseMiddleware<SentryTunnelMiddleware>());
}
