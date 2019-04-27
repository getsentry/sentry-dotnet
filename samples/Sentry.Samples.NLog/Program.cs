using System;

using NLog;
using NLog.Config;
using NLog.Targets;

using Sentry.Infrastructure;

// ReSharper disable ConvertToConstant.Local

namespace Sentry.Samples.NLog
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            LogManager.ThrowConfigExceptions = true;
            LogManager.ThrowExceptions = true;
            try
            {
                // Other overloads exist, for example, configure the SDK with only the DSN or no parameters at all.
                var config = new LoggingConfiguration();
                config.AddTarget(new ColoredConsoleTarget("console"));
                config.AddRuleForAllLevels("console");
                config
                    .AddSentryTarget(o =>
                    {
                        //o.DiagnosticLogger = new ConsoleDiagnosticLogger(Protocol.SentryLevel.Debug);
                        o.MinimumBreadcrumbLevel = LogLevel.Debug; // Debug and higher are stored as breadcrumbs (default os Information)
                        o.MinimumEventLevel = LogLevel.Error; // Error and higher is sent as event (default is Error)

                        // If DSN is not set, the SDK will look for an environment variable called SENTRY_DSN.
                        // If nothing is found, SDK is disabled.
                        o.Dsn = new Dsn("https://5fd7a6cda8444965bade9ccfd3df9882@sentry.io/1188141");
                        o.AttachStacktrace = true;
                        o.SendDefaultPii = true; // send Personal Identifiable information like the username of the user logged in to the device

                        // Other configuration
                    });

                LogManager.Configuration = config;

                var Log = LogManager.GetCurrentClassLogger();

                // Minimum Breadcrumb and Event log levels are set to levels higher than Verbose In this case,
                // Verbose messages are ignored
                Log.Trace("Verbose message which is not sent.");

                // Minimum Breadcrumb level is set to Debug so the following message is stored in memory and
                // sent with following events of the same Scope
                Log.Debug("Debug message stored as breadcrumb.");

                Log.Info("Informational message is also stored as a breadcrumb");

                Log.Warn("Warning also stored as a breadcrumb here");

                // Sends an event and stores the message as a breadcrumb too, to be sent with any upcoming events.
                Log.Error("Some event that includes the previous breadcrumbs");

                try
                {
                    throw new NotImplementedException();
                }
                catch (Exception e)
                {
                    e.Data.Add("details", "Do work always throws.");
                    Log.Fatal(e, "Error: with exception");
                    throw;
                }
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
    }
}
