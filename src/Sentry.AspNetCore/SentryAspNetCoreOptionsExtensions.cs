using Microsoft.Extensions.Logging;
using Sentry.Extensions.Logging;

namespace Sentry.AspNetCore
{
    internal static class SentryAspNetCoreOptionsExtensions
    {
        public static void Apply(
            this SentryAspNetCoreOptions aspnetOptions,
            SentryLoggingOptions loggingOptions)
        {
            if (loggingOptions == null)
            {
                return;
            }

            var log = aspnetOptions.Logging;
            if (log != null)
            {
                if (log.MinimumBreadcrumbLevel is LogLevel crumbLevel)
                {
                    loggingOptions.MinimumBreadcrumbLevel = crumbLevel;
                }
                if (log.MinimumEventLevel is LogLevel eventLevel)
                {
                    loggingOptions.MinimumEventLevel = eventLevel;
                }
            }

            if (aspnetOptions.InitializeSdk)
            {
                loggingOptions.InitializeSdk = true;
                loggingOptions.Init(o =>
                {
                    if (!string.IsNullOrWhiteSpace(aspnetOptions.Dsn))
                    {
                        o.Dsn = new Dsn(aspnetOptions.Dsn);
                    }

                    aspnetOptions.InitSdk?.Invoke(o);
                });
            }
        }
    }
}
