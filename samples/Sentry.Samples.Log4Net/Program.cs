using System;
using log4net;
using log4net.Config;

[assembly: XmlConfigurator(Watch = true)]

internal class Program
{
    private static void Main()
    {
        var log = LogManager.GetLogger(typeof(Program));

        try
        {
            DoWork();
        }
        catch (Exception e)
        {
            log.Debug("Debug: with exception", e);
            log.DebugFormat("DebugFormat: An error with message '{0}' has occurred", e.Message);
            log.Info("Info: with exception", e);
            log.InfoFormat("InfoFormat: An error with message '{0}' has occurred", e.Message);
            log.Warn("Warn: with exception", e);
            log.WarnFormat("WarnFormat: An error with message '{0}' has occurred", e.Message);
            log.Error("Error: with exception", e);
            log.ErrorFormat("ErrorFormat: An error with message '{0}' has occurred", e.Message);
            log.Fatal("Fatal: with exception", e);
            log.FatalFormat("FatalFormat: An error with message '{0}' has occurred", e.Message);
        }
    }

    private static void DoWork()
    {
        throw new NotImplementedException();
    }
}
