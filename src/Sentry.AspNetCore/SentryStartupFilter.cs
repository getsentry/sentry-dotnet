using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Sentry.AspNetCore;

/// <summary>
/// Starts Sentry integration.
/// </summary>
public class SentryStartupFilter : IStartupFilter
{
    /// <summary>
    /// Adds Sentry to the pipeline.
    /// </summary>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => e =>
    {
        e.UseSentry();

        next(e);
    };
}
