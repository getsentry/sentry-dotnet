namespace Sentry.Serilog;

/// <summary>
/// Extensions for <see cref="SentryOptions"/> to add Serilog specific configuration.
/// </summary>
public static class SentryOptionExtensions
{
    private static readonly Lock Sync = new();

    /// <summary>
    /// Enables the Serilog integration, so that properties from the Serilog <c>LogContext</c> get applied to all Sentry
    /// events.
    /// </summary>
    /// <remarks>
    /// Optional: the Sentry sink does this for you once it starts logging,
    /// so only events captured before then miss the tags.
    /// Call this in the options callback when initializing Sentry to include them from the first event.
    /// Calling it more than once has no additional effect.
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
