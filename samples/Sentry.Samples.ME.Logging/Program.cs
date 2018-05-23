using Microsoft.Extensions.Logging;

namespace Sentry.Samples.ME.Logging
{
    class Program
    {
        static void Main()
        {
            var loggerFactory = new LoggerFactory()
                .AddSentry()
                .AddConsole();

            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogError("This is a test of the emergency broadcast system.");

            loggerFactory.Dispose();
        }
    }
}
