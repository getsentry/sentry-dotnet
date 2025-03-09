namespace Sentry.Serilog;

/// <summary>
/// Extensions for <see cref="SentryOptions"/> to add Serilog specific configuration.
/// </summary>
public static class SentryOptionExtensions
{
    /// <summary>
    /// Ensures Serilog scope properties get applied to Sentry events. If you are not initialising Sentry when
    /// configuring the Sentry sink for Serilog then you should call this method in the options callback for whichever
    /// Sentry integration you are using to initialise Sentry.
    /// </summary>
    /// <param name="options"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T ApplySerilogScopeToEvents<T>(this T options) where T : SentryOptions
    {
        options.AddEventProcessor(new SerilogScopeEventProcessor(options));
        return options;
    }
}
