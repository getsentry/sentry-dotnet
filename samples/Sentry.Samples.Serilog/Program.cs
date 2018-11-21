using System;
using Sentry.Serilog;
using Serilog;
using Serilog.Context;
using Serilog.Events;

internal class Program
{
    private static void Main()
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.Sentry("https://5fd7a6cda8444965bade9ccfd3df9882@sentry.io/1188141", restrictedToMinimumLevel: LogEventLevel.Information)
            .CreateLogger();

        try
        {
            // The following anonymous object gets serialized and sent with log messages
            using (LogContext.PushProperty("inventory", new
            {
                SmallPotion = 3,
                BigPotion = 0,
                CheeseWheels = 512
            }))
            {
                // Logger config enables the Sink only for level INFO or higher so the Debug
                // Does not result in an event in Sentry
                Log.Debug("Debug message which is not sent.");

                try
                {
                    DoWork();
                }
                catch (Exception e)
                {
                    e.Data.Add("details", "Do work always throws.");
                    Log.Error(e, "Error: with exception");
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
