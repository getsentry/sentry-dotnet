using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;
using Sentry.Infrastructure;

namespace Sentry.AspNetCore
{
    /// <inheritdoc />
    internal class SentryStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => e =>
            {
                var options = e.ApplicationServices.GetService<IOptions<SentryAspNetCoreOptions>>()?.Value;
                if (options?.InitializeSdk == true)
                {
                    var logger = e.ApplicationServices.GetService<ILogger<ISentryClient>>();
                    if (options.Debug && (options.DiagnosticLogger == null || options.DiagnosticLogger.GetType() == typeof(ConsoleDiagnosticLogger)))
                    {
                        options.DiagnosticLogger = new MelDiagnosticLogger(logger, options.DiagnosticsLevel);
                    }

                    var hub = e.ApplicationServices.GetRequiredService<IHub>();
                    var lifetime = e.ApplicationServices.GetRequiredService<IApplicationLifetime>();
                    var disposable = SentrySdk.UseHub(hub);
                    lifetime.ApplicationStopped.Register(() => disposable.Dispose());
                }

                e.UseSentry();

                next(e);
            };
    }
}
