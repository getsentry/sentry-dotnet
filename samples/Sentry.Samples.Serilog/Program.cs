using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Formatting.Display;

internal static class Program
{
    private static void Main()
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            // Other overloads exist, for example, configure the SDK with only the DSN or no parameters at all.
            .WriteTo.Sentry(options =>
            {
                // You can set here in code, or you can set it in the SENTRY_DSN environment variable.
                // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
                options.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";

                // Debug and higher are stored as breadcrumbs (default os Information)
                options.MinimumBreadcrumbLevel = LogEventLevel.Debug;
                // Error and higher is sent as event (default is Error)
                options.MinimumEventLevel = LogEventLevel.Error;
                options.AttachStacktrace = true;
                // send PII like the username of the user logged in to the device
                options.SendDefaultPii = true;
                // Optional Serilog text formatter used to format LogEvent to string. If TextFormatter is set, FormatProvider is ignored.
                options.TextFormatter = new MessageTemplateTextFormatter("[{MyTaskId}] {Message}");
                // Other configuration
            })
            .CreateLogger();

        try
        {
            // The following property is used in the TextFormatter to format the log message.
            using (LogContext.PushProperty("MyTaskId", 42))
            // The following anonymous object gets serialized and sent with log messages
            using (LogContext.PushProperty("inventory", new
            {
                SmallPotion = 3,
                BigPotion = 0,
                CheeseWheels = 512
            }))
            {
                // Minimum Breadcrumb and Event log levels are set to levels higher than Verbose
                // In this case, Verbose messages are ignored
                Log.Verbose("Verbose message which is not sent.");

                // Minimum Breadcrumb level is set to Debug so the following message is stored in memory
                // and sent with following events of the same Scope
                Log.Debug("Debug message stored as breadcrumb.");

                // Breadcrumb with a different context property value will be formatted with a different prefix.
                Log.ForContext("MyTaskId", 65).Debug("Message with a different MyTaskId");

                // Sends an event and stores the message as a breadcrumb too, to be sent with any upcoming events.
                Log.Error("Some event that includes the previous breadcrumbs");

                try
                {
                    DoWork();
                }
                catch (Exception e)
                {
                    e.Data.Add("details", "Do work always throws.");
                    Log.Fatal(e, "Error: with exception");
                    throw;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void DoWork()
    {
        Log.Information("About to throw {ExceptionType} type of exception.", nameof(NotImplementedException));

        throw new NotImplementedException();
    }
}
