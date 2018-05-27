using System;
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
                    o.MinimumBreadcrumbLevel = LogLevel.Information;

                    // TODO: Assess: Support LogLevel.None which means collect breadcrumbs but don't send event on Error
                    // This allows other integrations like ASP.NET Core use the crumbs collected via the logging integration
                    // But the calls to the Logger.Log itself won't be sending anything to Sentry
                    o.MinimumEventLevel = LogLevel.Error;
                    o.MaxLogBreadcrumbs = 100;
                })
                .AddConsole())
            {
                var logger = loggerFactory.CreateLogger<Program>();

                logger.LogTrace("By default this log level is no-op");

                logger.LogInformation("This should only store a Breadcrumb");

                logger.LogError("This generates an event captured by sentry which includes the message above.");

            } // Disposing the logger won't affect Sentry: The lifetime is managed externally (call CloseAndFlush)
        }
    }
}
