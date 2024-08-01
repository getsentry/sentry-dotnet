using NLog;
using NLog.Config;
using NLog.Targets;
using Sentry.NLog;

// ReSharper disable ConvertToConstant.Local
namespace Sentry.Samples.NLog;

public static class Program
{
    // Modify the configuration file NLog.config to affect 'UsingNLogConfigFile'

    // DSN used by the example: 'UsingCodeConfiguration'.
    // #### ADD YOUR DSN HERE:
    private static readonly string DsnSample = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";

    private static void Main()
    {
        try
        {
            // You can configure your logger using a configuration file:
            UsingNLogConfigFile();

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
        // Here is an example of how you can set user properties in code via NLog. The layout is configured to use these values for the sentry user
        var properties = new Dictionary<string, string>
        {
            {"id", "myId"},
            {"username", "userNumberOne"},
            {"email", "theCoolest@sample.com"},
            {"ipAddress", "127.0.0.1"}
        };

        using (ScopeContext.PushProperties(properties))
        {
            // Minimum Breadcrumb and Event log levels are set to levels higher than Trace.
            // In this case, Trace messages are ignored
            logger.Trace("Verbose message which is not sent.");

            // Minimum Breadcrumb level is set to Debug so the following message is stored in memory and sent
            // with following events of the same Scope
            logger.Debug("Debug message stored as breadcrumb.");

            // Sends an event and stores the message as a breadcrumb too, to be sent with any upcoming events.
            logger.Error("Some event that includes the previous breadcrumbs. mood = {mood}", "happy that my error is reported");

            try
            {
                DoWork(logger);
            }
            catch (Exception e)
            {
                e.Data.Add("details", "DoWork always throws.");
                logger.Fatal(e,
                    "Error: with exception. {data}",
                    new
                    {
                        title = "compound data object",
                        wowFactor = 11,
                        errorReported = true
                    });
                LogManager.Flush();
            }
        }
    }

    private static void DoWork(ILogger logger)
    {
        int a = 0, b = 10;

        logger.Info("Dividing {b} by {a}", b, a);

        logger.Warn("a is 0");

        _ = b / a;
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
        var config = LogManager.Configuration = new LoggingConfiguration();
        _ = config
            .AddSentry(options =>
            {
                // If DSN is not set, the SDK will look for an environment variable called SENTRY_DSN. If
                // nothing is found, SDK is disabled.
                options.Dsn = DsnSample;

                options.Layout = "${message}";
                options.BreadcrumbLayout = "${logger}: ${message}"; // Optionally specify a separate format for breadcrumbs

                options.MinimumBreadcrumbLevel = LogLevel.Debug; // Debug and higher are stored as breadcrumbs (default is Info)
                options.MinimumEventLevel = LogLevel.Error; // Error and higher is sent as event (default is Error)

                options.AttachStacktrace = true;
                options.SendDefaultPii = true; // Send Personal Identifiable information like the username of the user logged in to the device

                options.IncludeEventDataOnBreadcrumbs = true; // Optionally include event properties with breadcrumbs
                options.ShutdownTimeoutSeconds = 5;

                //Optionally specify user properties via NLog (here using MappedDiagnosticsLogicalContext as an example)
                options.User = new SentryNLogUser
                {
                    Id = "${scopeproperty:item=id}",
                    Username = "${scopeproperty:item=username}",
                    Email = "${scopeproperty:item=email}",
                    IpAddress = "${scopeproperty:item=ipAddress}",
                    Other =
                    {
                        new TargetPropertyWithContext("mood", "joyous")
                    },
                };

                options.AddTag("logger", "${logger}");  // Send the logger name as a tag

                // Other configuration
            });

        config.AddTarget(new DebuggerTarget("debugger"));

        config.AddTarget(new ColoredConsoleTarget("console"));
        config.AddRuleForAllLevels("console");
        config.AddRuleForAllLevels("debugger");

        LogManager.Configuration = config;

        var log = LogManager.GetCurrentClassLogger();
        DemoLogger(log);
    }
}
