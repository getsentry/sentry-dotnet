using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Sentry.Samples.ME.Logging
{
    class Program
    {
        static void Main()
        {
            using (var loggerFactory = new LoggerFactory()
                .AddConsole(LogLevel.Trace)
                .AddSentry(o =>
                {
                    // Initialize the SDK, alternative to relying on previously called: `using(SentryCore.Init)`:
                    // this is useful when Logging is the first or is the only integration enabled:
                    o.Init(i =>
                    {
                        i.Dsn = new Dsn("https://key@sentry.io/id");
                        i.MaxBreadcrumbs = 150; // Increasing from default 100
                    });

                    // Logging settings: The default values are:
                    o.MinimumBreadcrumbLevel = LogLevel.Information; // It requires at least this level to store breadcrumb
                    o.MinimumEventLevel = LogLevel.Error; // This level or above will result in event sent to Sentry
                }))
            {
                var logger = loggerFactory.CreateLogger<Program>();

                logger.LogTrace("1 - By *default* this log level is ignored by Sentry.");

                logger.LogInformation("2 -Information messages are stored as Breadcrumb, sent with the next event.");

                logger.LogError("3 - This generates an event, captured by sentry and includes breadcrumbs (2) tracked in this transaction.");

                using (logger.BeginScope(new Dictionary<string, string>
                {
                    {"A - some context", "some value"},
                    {"B - more info on this", "more value"},
                }))
                {
                    logger.LogWarning("4 - Breadcrumb that only exists inside this scope");

                    logger.LogError("5 - An event that includes the scoped key-value (A, B) above and also the breadcrumbs: (2, 4)");

                    using (logger.BeginScope("C - Inner most scope, with single string state"))
                    {
                        logger.LogInformation("6 - Inner most breadcrumb");

                        logger.LogError("7 - An event that includes the scope key-value (A, B, C) and also the breadcrumbs: (2, 4, 6)");

                    } // Dispose scope C, drops state C and breadcrumb 6

                    // An exception that will go unhandled and crash the app:
                    // Even though it's not caught nor logged, this error is captured by Sentry
                    // It will include all the scope data available up to this point
                    throw new Exception("8 - This unhandled exception is captured and includes Scope (A, B) and crumbs: (2, 4)");
                }
            }
            // Disposing the LoggerFactory will close the SDK since it was initialized through
            // the integration while calling .Init()
        }
    }
}
