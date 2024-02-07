using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Extensibility;
using Sentry.Extensions.Logging;
using Sentry.Infrastructure;

// ReSharper disable once CheckNamespace -- Discoverability
namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Use Sentry integration
    /// </summary>
    /// <param name="app">The application.</param>
    public static IApplicationBuilder UseSentry(this IApplicationBuilder app)
    {
        // Container is built so resolve a logger and modify the SDK internal logger
        var options = app.ApplicationServices.GetService<IOptions<SentryAspNetCoreOptions>>();
        if (options?.Value is { } o)
        {
            if (o.Debug && (o.DiagnosticLogger is null or ConsoleDiagnosticLogger ||
                            o.DiagnosticLogger.GetType().Name == "TestOutputDiagnosticLogger"))
            {
                var logger = app.ApplicationServices.GetRequiredService<ILogger<ISentryClient>>();
                o.DiagnosticLogger = new MelDiagnosticLogger(logger, o.DiagnosticLevel);
            }

            var stackTraceFactory = app.ApplicationServices.GetService<ISentryStackTraceFactory>();
            if (stackTraceFactory != null)
            {
                o.UseStackTraceFactory(stackTraceFactory);
            }

        }

        var lifetime = app.ApplicationServices.GetService<IHostApplicationLifetime>();
        lifetime?.ApplicationStopped.Register(SentrySdk.Close);

        return app.UseMiddleware<SentryMiddleware>();
    }
}
