namespace Sentry.Serilog;

/// <summary>
/// Extensions for <see cref="SentryOptions"/> to add Serilog specific configuration.
/// </summary>
public static class SentryOptionExtensions
{
    private static readonly object Sync = new();

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
    public static void UseSerilog(this SentryOptions options) => options.TryUseSerilog();

    // Sinks sharing one set of options can reach this concurrently, so the check and the add have to be atomic.
    internal static bool TryUseSerilog(this SentryOptions options)
    {
        lock (Sync)
        {
            if (options.HasSerilogScopeEventProcessor())
            {
                return false;
            }

            options.AddEventProcessor(new SerilogScopeEventProcessor(options));
            return true;
        }
    }

    internal static bool HasSerilogScopeEventProcessor(this SentryOptions options)
        => options.EventProcessors.Any(processor => processor.Type == typeof(SerilogScopeEventProcessor));

}
