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
                // Container is built so resolve a logger and modify the SDK internal logger
                var options = e.ApplicationServices.GetRequiredService<IOptions<SentryAspNetCoreOptions>>().Value;
                if (options.Debug && (options.DiagnosticLogger == null || options.DiagnosticLogger.GetType() == typeof(ConsoleDiagnosticLogger)))
                {
                    var logger = e.ApplicationServices.GetRequiredService<ILogger<ISentryClient>>();
                    options.DiagnosticLogger = new MelDiagnosticLogger(logger, options.DiagnosticsLevel);
                }


                var lifetime = e.ApplicationServices.GetService<IApplicationLifetime>();
                lifetime?.ApplicationStopped.Register(SentrySdk.Close);

                e.UseSentry();

                next(e);
            };
    }
}
