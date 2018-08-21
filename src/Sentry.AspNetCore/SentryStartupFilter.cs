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
                // Initialize the SDK: This will not run if SentryLoggingProvider is enabled (i.e: other logging library didn't replace it)
                var options = e.ApplicationServices.GetService<IOptions<SentryAspNetCoreOptions>>()?.Value;
                if (options?.InitializeSdk == true)
                {
                    var logger = e.ApplicationServices.GetService<ILogger<ISentryClient>>();
                    if (options.DiagnosticLogger == null || options.DiagnosticLogger.GetType() == typeof(ConsoleDiagnosticLogger))
                    {
                        options.DiagnosticLogger = new MelDiagnosticLogger(logger, options.DiagnosticsLevel);
                    }

                    var lifetime = e.ApplicationServices.GetRequiredService<IApplicationLifetime>();

                    var sdk = SentrySdk.Init(options);

                    lifetime.ApplicationStopped.Register(() => sdk.Dispose());
                }

                e.UseSentry();

                next(e);
            };
    }
}
