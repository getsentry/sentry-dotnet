using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Sentry.AspNetCore
{
    /// <inheritdoc />
    internal class SentryStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => e =>
            {
                // Initialize the SDK: This will not run if SentryLoggingProvider is enabled (i.e: other logging library didn't replace it)
                var options = e.ApplicationServices.GetService<SentryAspNetCoreOptions>();
                if (options?.InitializeSdk == true && !SentrySdk.IsEnabled)
                {
                    var lifetime = e.ApplicationServices.GetRequiredService<IApplicationLifetime>();

                    var sdk = SentrySdk.Init(o =>
                    {
                        // Invoke all Init calls to AspNetCoreOptions object
                        options.ConfigureOptionsActions.ForEach(a => a(o));
                    });

                    lifetime.ApplicationStopped.Register(() => sdk.Dispose());
                }

                e.UseSentry();

                next(e);
            };
    }
}
