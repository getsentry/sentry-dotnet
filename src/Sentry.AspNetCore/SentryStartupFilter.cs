using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RequestDecompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sentry.AspNetCore.RequestDecompression;

namespace Sentry.AspNetCore;

/// <summary>
/// Starts Sentry integration.
/// </summary>
public class SentryStartupFilter : IStartupFilter
{
    /// <summary>
    /// Adds Sentry to the pipeline.
    /// </summary>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        // If we are capturing request bodies and the user has configured request body decompression, we need to
        // ensure that the RequestDecompression middleware gets called before Sentry's middleware.
        var options = app.ApplicationServices.GetService<IOptions<SentryAspNetCoreOptions>>();
        if (options?.Value is { CollectsRequestBody: true }
            && app.ApplicationServices.GetService<IRequestDecompressionProvider>() is not null)
        {
            app.UseMiddleware<RequestDecompressionMiddleware>();
        }

        app.UseSentry();

        next(app);
    };
}
