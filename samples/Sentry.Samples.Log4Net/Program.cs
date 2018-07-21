using System;
using log4net;
using log4net.Config;

[assembly: XmlConfigurator(Watch = true)]

internal class Program
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

    private static void Main()
    {
        // The following anonymous object gets serialized and sent with log messages
        ThreadContext.Properties["inventory"] = new
        {
            SmallPotion = 3,
            BigPotion = 0,
            CheeseWheels = 512
        };

        // app.config enables SentryAppender only for level INFO or higher so the Debug
        // Does not result in an event in Sentry
        Log.Debug("Debug message which is not sent.");

        try
        {
            DoWork();
        }
        catch (Exception e)
        {
            e.Data.Add("details", "Do work always throws.");
            Log.Error("Error: with exception", e);
        }
    }

    private static void DoWork()
    {
        Log.InfoFormat("InfoFormat: About to throw {0} type of exception.", nameof(NotImplementedException));

        throw new NotImplementedException();
    }
}
