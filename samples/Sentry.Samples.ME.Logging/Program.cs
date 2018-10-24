using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Sentry.Extensions.Logging;
using Sentry.Protocol;

internal class Program
{
    private static void Main()
    {
        using (var loggerFactory = new LoggerFactory()
            .AddConsole(LogLevel.Trace)
            .AddSentry(o =>
            {
                // Set to true to SDK debugging to see the internal messages through the logging library.
                o.Debug = false;
                // Configure the level of Sentry internal logging
                o.DiagnosticsLevel = SentryLevel.Debug;

                o.Dsn = "https://5fd7a6cda8444965bade9ccfd3df9882@sentry.io/1188141";
                o.MaxBreadcrumbs = 150; // Increasing from default 100
                o.Release = "e386dfd"; // If not set here, SDK looks for it on main assembly's AssemblyInformationalVersion and AssemblyVersion

                // Optionally configure options: The default values are:
                o.MinimumBreadcrumbLevel = LogLevel.Information; // It requires at least this level to store breadcrumb
                o.MinimumEventLevel = LogLevel.Error; // This level or above will result in event sent to Sentry

                // Don't keep as a breadcrumb or send events for messages of level less than Critical with exception of type DivideByZeroException
                o.AddLogEntryFilter((category, level, eventId, exception)
                    => level < LogLevel.Critical && exception?.GetType() == typeof(DivideByZeroException));
            }))
        {
            var logger = loggerFactory.CreateLogger<Program>();

            logger.LogTrace("1 - By *default* this log level is ignored by Sentry.");

            logger.LogInformation("2 - Information messages are stored as Breadcrumb, sent with the next event.");

            // Log messages with variables are grouped together.
            // This way a log message like: 'User {userId} logged in' doesn't generate 1 issue in Sentry for each user you have.
            // When visualizing this issue in Sentry, you can press Next and Back to see the individual log entries:
            logger.LogError("3 - This generates an event {id}, captured by sentry and includes breadcrumbs (2) tracked in this transaction.",
                100);
            logger.LogError("3 - This generates an event {id}, captured by sentry and includes breadcrumbs (2) tracked in this transaction.",
                999);

            using (logger.BeginScope(new Dictionary<string, string>
                {
                    {"A", "some value"},
                    {"B", "more value"},
                }))
            {
                logger.LogWarning("4 - Breadcrumb that only exists inside this scope");

                logger.LogError("5 - An event that includes the scoped key-value (A, B) above and also the breadcrumbs: (2, 4) and event (3)");

                using (logger.BeginScope("C - Inner most scope, with single string state"))
                {
                    logger.LogInformation("6 - Inner most breadcrumb");

                    try
                    {
                        Dependency.Work("some work");
                    }
                    catch (Exception e)
                    {
                        // Handle an exception and log it:
                        logger.LogError(e, "7 - An event that includes the scope key-value (A, B, C) and also the breadcrumbs: (2, 4, 6) and events (3, 5)");
                    }

                } // Dispose scope C, drops state C and breadcrumb 6

                // An exception that will go unhandled and crash the app:
                // Even though it's not caught nor logged, this error is captured by Sentry!
                // It will include all the scope data available up to this point
                Dependency.Work("8 - This unhandled exception is captured and includes Scope (A, B) and crumbs: (2, 4, 5) and event (3) ");
            }
        }
        // Disposing the LoggerFactory will close the SDK since it was initialized through
        // the integration while calling .Init()
    }
}

internal class Dependency
{
    private static int _counter;

    public static void Work(string message)
    {
        if (_counter == 10)
        {
            throw new InvalidOperationException(message);
        }

        _counter++;
        Work(message);
    }
}
