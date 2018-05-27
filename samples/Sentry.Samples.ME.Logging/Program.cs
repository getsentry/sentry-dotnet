using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Sentry.Samples.ME.Logging
{
    class Program
    {
        static void Main()
        {
            SentryCore.Init(); // Initialize SDK

            try
            {
                App();
            }
            catch (Exception e)
            {
                SentryCore.CaptureException(e);
            }
            finally
            {
                SentryCore.CloseAndFlush();
            }
        }

        static void App()
        {
            using (var loggerFactory = new LoggerFactory()
                .AddSentry(o =>
                {
                    // The default values are:
                    o.MinimumBreadcrumbLevel = LogLevel.Information; // It requires at least this level to log store breadcrumb

                    // TODO: Assess: Support LogLevel.None which means collect breadcrumbs but don't send event on Error
                    // This allows other integrations like ASP.NET Core use the crumbs collected via the logging integration
                    // But the calls to the Logger.Log itself won't be sending anything to Sentry
                    o.MinimumEventLevel = LogLevel.Error; // This level or above will result in event sent to Sentry
                    o.MaxLogBreadcrumbs = 100; // How many breadcrumbs to store.
                })
                .AddConsole())
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
                    }

                    logger.LogError("8 - Includes Scope (A, B) and breadcrumbs: (2, 4)");
                }

                logger.LogError("9 - No scope data, breadcrumb: 2");

            } // Disposing the logger won't affect Sentry: The lifetime is managed externally (call CloseAndFlush)
        }
    }
}
