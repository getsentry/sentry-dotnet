using System.Collections.Generic;

namespace Sentry.Extensibility;

/// <summary>
/// Factory to <see cref="SentryStackTrace" /> from an <see cref="Exception" />.
/// </summary>
public interface ISentryStackTraceFactory
{
    /// <summary>
    /// Creates a <see cref="SentryStackTrace" /> from the optional <see cref="Exception" />.
    /// </summary>
    /// <param name="exception">The exception to create the stacktrace from.</param>
    /// <returns>A Sentry stack trace.</returns>
    SentryStackTrace? Create(Exception? exception = null);

    /// <summary>
    /// Returns a list of <see cref="DebugImage" />s referenced from the previously processed <see cref="Exception" />s.
    /// </summary>
    /// <returns>A list of referenced debug images.</returns>
    List<DebugImage>? DebugImages() { return null; }
}
