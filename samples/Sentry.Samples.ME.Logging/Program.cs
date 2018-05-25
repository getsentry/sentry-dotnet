using System;
using Microsoft.Extensions.Logging;

namespace Sentry.Samples.ME.Logging
{
    class Program
    {
        static void Main()
        {
            SentryCore.Init();
            try
            {
                var loggerFactory = new LoggerFactory()
                    .AddSentry()
                    .AddConsole();

                var logger = loggerFactory.CreateLogger<Program>();

                logger.LogTrace("By default this is no-op");
                logger.LogInformation("By default this should only store a Breadcrumb");
                logger.LogError("This generates an event captured by sentry");

                // Disposing the logger won't affect Sentry.
                // The lifetime is managed externally (call CloseAndFlush below)
                loggerFactory.Dispose();
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
    }
}
