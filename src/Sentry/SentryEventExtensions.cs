using Sentry.Protocol;

namespace Sentry;

/// <summary>
/// Extension methods for <see cref="SentryEvent"/>.
/// </summary>
public static class SentryEventExtensions
{
    /// <summary>
    /// Determines whether this event was created from an unhandled exception.
    /// </summary>
    /// <param name="event">The Sentry event.</param>
    /// <returns>
    /// <c>true</c> if the event was created from an unhandled exception; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// An unhandled exception is one that was not caught by application code and was instead
    /// captured by the Sentry SDK through hooks like UnhandledExceptionHandler, ASP.NET Core
    /// middleware, or other integration points. By default, Sentry marks exceptions as handled
    /// unless explicitly captured through one of these unhandled exception hooks.
    ///
    /// This is useful for filtering events in callbacks like BeforeSend, where you may want to
    /// treat unhandled exceptions differently from handled ones (e.g., always send unhandled
    /// exceptions even if they match a filter that would normally exclude them).
    /// </remarks>
    /// <example>
    /// <code>
    /// options.SetBeforeSend((@event, hint) =>
    /// {
    ///     // Always send unhandled exceptions, even if they're network timeouts
    ///     if (@event.IsFromUnhandledException())
    ///     {
    ///         return @event;
    ///     }
    ///
    ///     // Filter out handled network timeout exceptions
    ///     if (@event.Exception is HttpRequestException)
    ///     {
    ///         return null;
    ///     }
    ///
    ///     return @event;
    /// });
    /// </code>
    /// </example>
    public static bool IsFromUnhandledException(this SentryEvent @event)
    {
        // Check if the original exception was marked as unhandled
        if (@event.Exception?.Data[Mechanism.HandledKey] is false)
        {
            return true;
        }

        // Check if any of the Sentry exceptions have an unhandled mechanism
        return @event.SentryExceptions?.Any(e => e.Mechanism is { Handled: false }) ?? false;
    }

    /// <summary>
    /// Determines whether this event was created from a terminal exception.
    /// </summary>
    /// <param name="event">The Sentry event.</param>
    /// <returns>
    /// <c>true</c> if the event was created from a terminal exception; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// A terminal exception is an unhandled exception that caused the application to crash or
    /// terminate. This excludes unhandled exceptions that were explicitly marked as non-terminal,
    /// such as those captured by UnobservedTaskException handlers or certain Unity SDK integrations.
    ///
    /// In most cases, an unhandled exception is terminal. However, some integrations may capture
    /// unhandled exceptions that don't actually crash the app (e.g., unobserved task exceptions)
    /// and mark them with Terminal = false.
    /// </remarks>
    /// <example>
    /// <code>
    /// options.SetBeforeSend((@event, hint) =>
    /// {
    ///     // Only send terminal exceptions for certain exception types
    ///     if (@event.Exception is NetworkException &amp;&amp; !@event.IsFromTerminalException())
    ///     {
    ///         return null; // Don't send non-terminal network exceptions
    ///     }
    ///
    ///     return @event;
    /// });
    /// </code>
    /// </example>
    public static bool IsFromTerminalException(this SentryEvent @event)
    {
        // Check if the original exception was unhandled and not explicitly marked as non-terminal
        if (@event.Exception?.Data[Mechanism.HandledKey] is false)
        {
            // If it's unhandled but explicitly marked as non-terminal, return false
            if (@event.Exception.Data[Mechanism.TerminalKey] is false)
            {
                return false;
            }
            // Otherwise, unhandled exceptions are terminal by default
            return true;
        }

        // Check if any Sentry exceptions are unhandled and terminal
        // (handled: false and terminal: not explicitly false)
        return @event.SentryExceptions?.Any(e =>
            e.Mechanism is { Handled: false } &&
            e.Mechanism.Terminal != false
        ) ?? false;
    }
}
