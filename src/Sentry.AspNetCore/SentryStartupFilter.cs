using Microsoft.AspNetCore.Builder;

namespace Sentry.AspNetCore;

/// <summary>
/// Starts Sentry integration.
/// </summary>
public class SentryStartupFilter : IStartupFilter
{
    /// <summary>
    /// Adds Sentry to the startup pipeline.
    /// </summary>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        builder =>
        {
            builder.UseSentry();

            next(builder);
        };
}
