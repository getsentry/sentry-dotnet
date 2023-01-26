namespace Sentry.Android.AssemblyReader;

/// <summary>
/// Lightweight logger interface for use by this library.
/// </summary>
public interface IAndroidAssemblyReaderLogger
{
    /// <summary>
    /// Logs a message.
    /// </summary>
    /// <param name="message">The message string.</param>
    /// <param name="args">Arguments to be formatted with the message string.</param>
    void Log(string message, params object?[] args);
}
