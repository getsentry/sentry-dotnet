using Sentry.Internal;

namespace Sentry.Extensibility;

/// <summary>
/// Default factory to <see cref="SentryStackTrace" /> from an <see cref="Exception" />.
/// </summary>
public sealed class SentryStackTraceFactory : ISentryStackTraceFactory
{
    private readonly SentryOptions _options;

    /// <summary>
    /// Creates an instance of <see cref="SentryStackTraceFactory"/>.
    /// </summary>
    public SentryStackTraceFactory(SentryOptions options) => _options = options;

    /// <summary>
    /// Creates a <see cref="SentryStackTrace" /> from the optional <see cref="Exception" />.
    /// </summary>
    /// <param name="exception">The exception to create the stacktrace from.</param>
    /// <returns>A Sentry stack trace.</returns>
    public SentryStackTrace? Create(Exception? exception = null)
    {
        if (exception == null && !_options.AttachStacktrace)
        {
            _options.LogDebug("No Exception and AttachStacktrace is off. No stack trace will be collected.");
            return null;
        }

        var isCurrentStackTrace = exception == null && _options.AttachStacktrace;
        _options.LogDebug("Creating SentryStackTrace. isCurrentStackTrace: {0}.", isCurrentStackTrace);

        var stackTrace = exception is null ? new StackTrace(true) : new StackTrace(exception, true);
        var result = DebugStackTrace.Create(_options, stackTrace, isCurrentStackTrace);
        // TODO: Bruno had this but I think he was just mucking around to test how it would look in Sentry
        // var result = DebugStackTrace.Create(_options, stackTrace, isCurrentStackTrace, 3);
        _options.LogDebug("Created {0} with {1} frames.", nameof(DebugStackTrace), result.Frames.Count);
        return result.Frames.Count != 0 ? result : null;
    }
}
