using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Sentry.Tunnel;

/// <summary>
/// Extension methods to add Sentry ingestion tunnel.
/// </summary>
[Obsolete(_obsolete)]
public static class SentryTunnelingApplicationBuilderExtensions
{
    private const string _obsolete = @"The functionality from Sentry.Tunnel has been moved into Sentry.AspNetCore.
Remove the Sentry.Tunnel NuGet and ensure the Sentry.AspNetCore NuGet is referenced.";
    /// <summary>
    /// Adds and configures the Sentry tunneling middleware.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="hostnames">The extra hostnames to be allowed for the tunneling. sentry.io is allowed by default; add your own Sentry domain if you use a self-hosted Sentry or Relay.</param>
    [Obsolete($@"{_obsolete}
Replaced by {nameof(SentryWebHostBuilderExtensions)}.{nameof(AddSentryTunneling)}")]
    public static void AddSentryTunneling(this IServiceCollection services, params string[] hostnames) =>
        SentryWebHostBuilderExtensions.AddSentryTunneling(services, hostnames);

    /// <summary>
    /// Adds the <see cref="SentryTunnelMiddleware"/> to the pipeline.
    /// </summary>
    /// <param name="builder">The app builder.</param>
    /// <param name="path">The path to listen for Sentry envelopes.</param>
    [Obsolete($@"{_obsolete}
Replaced by {nameof(SentryWebHostBuilderExtensions)}.{nameof(UseSentryTunneling)}")]
    public static void UseSentryTunneling(this IApplicationBuilder builder, string path = "/tunnel") =>
        SentryWebHostBuilderExtensions.UseSentryTunneling(builder, path);
}
