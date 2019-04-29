using System;

using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Targets;

// ReSharper disable ConvertToConstant.Local

namespace Sentry.Samples.NLog
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                // You can configure your logger using a configuration file:
                //UsingNLogConfigFile();

                // Or you can configure it with code:
                UsingCodeConfiguration();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                LogManager.Shutdown();
            }
        }

        private static void DemoLogger(ILogger logger)
        {
            // Minimum Breadcrumb and Event log levels are set to levels higher than Verbose In this case,
            // Verbose messages are ignored
            logger.Trace("Verbose message which is not sent.");

            // Minimum Breadcrumb level is set to Debug so the following message is stored in memory and sent
            // with following events of the same Scope
            logger.Debug("Debug message stored as breadcrumb.");

            logger.Info("Informational message is also stored as a breadcrumb, here's some data: {name} {age} {species}", "Mr. Cuddles", 3, "bunny");

            logger.Warn("Warning also stored as a breadcrumb here mood={mood}", "nervous");

            // Sends an event and stores the message as a breadcrumb too, to be sent with any upcoming events.
            logger.Error("Some event that includes the previous breadcrumbs. mood = {mood}", "happy that my error is reported");

            try
            {
                DoWork(logger);
            }
            catch (Exception e)
            {
                e.Data.Add("details", "This method always throws.");
                logger.Fatal(e, "Error: with exception. {data}", new { title = "compound data object", wowFactor = 11, errorReported = true });
                LogManager.Flush();
            }
        }

        private static void DoWork(ILogger logger)
        {
            logger.Info("About to throw an exception");
            throw new IndexOutOfRangeException();
        }

        private static void UsingNLogConfigFile()
        {
            // If using an NLog.config xml file, NLog will load the configuration automatically Or, if using a
            // different file, you can call the following to load it for you: LogManager.Configuration = LogManager.LoadConfiguration("NLog.config").Configuration;

            var logger = LogManager.GetCurrentClassLogger();
            DemoLogger(logger);
        }

        private static void UsingCodeConfiguration()
        {
            // Other overloads exist, for example, configure the SDK with only the DSN or no parameters at all.
            var config = (LogManager.Configuration = new LoggingConfiguration());
            config
                .AddSentryTarget(o =>
                {
                    o.EnableDiagnosticConsoleLogging = true;
                    o.SendEventPropertiesAsData = true;
                    o.SendContextPropertiesAsData = true;

                    o.MinimumBreadcrumbLevel = LogLevel.Debug; // Debug and higher are stored as breadcrumbs (default os Information)
                    o.MinimumEventLevel = LogLevel.Error; // Error and higher is sent as event (default is Error)

                    // If DSN is not set, the SDK will look for an environment variable called SENTRY_DSN. If
                    // nothing is found, SDK is disabled.
                    o.Dsn = new Dsn("https://5fd7a6cda8444965bade9ccfd3df9882@sentry.io/1188141");
                    o.AttachStacktrace = true;
                    o.SendDefaultPii = true; // send Personal Identifiable information like the username of the user logged in to the device

                    o.ShutdownTimeoutSeconds = 5;

                    o.AddTag("Logger", "${logger}");  // Send the logger name as a tag

                    // Other configuration
                });

            config.AddTarget(new DebuggerTarget("debugger"));

            config.AddTarget(new ColoredConsoleTarget("console"));
            config.AddRuleForAllLevels("console");
            config.AddRuleForAllLevels("debugger");
            LogManager.Configuration = config;

            var Log = LogManager.GetCurrentClassLogger();
            DemoLogger(Log);
        }
    }

}
