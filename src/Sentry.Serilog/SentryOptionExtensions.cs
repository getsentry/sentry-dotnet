namespace Sentry.Serilog;

/// <summary>
/// Extensions for <see cref="SentryOptions"/> to add Serilog specific configuration.
/// </summary>
public static class SentryOptionExtensions
{
    /// <summary>
    /// Enables the Serilog integration, so that properties from the Serilog <c>LogContext</c> get applied to all Sentry
    /// events.
    /// </summary>
    /// <remarks>
    /// Call this in the options callback of whichever method you use to initialise Sentry (for example
    /// <c>SentrySdk.Init</c> or <c>UseSentry</c>). The Sentry sink for Serilog does not initialise Sentry, so it cannot
    /// do this for you. Calling this more than once has no additional effect.
    /// </remarks>
    /// <param name="options">The options used to initialise Sentry.</param>
    public static void UseSerilog(this SentryOptions options)
    {
        if (options.HasSerilogScopeEventProcessor())
        {
            return;
        }

        options.AddEventProcessor(new SerilogScopeEventProcessor(options));
    }

    internal static bool HasSerilogScopeEventProcessor(this SentryOptions options)
        => options.EventProcessors.Any(processor => processor.Type == typeof(SerilogScopeEventProcessor));
}
