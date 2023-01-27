namespace Sentry.Android.AssemblyReader;

/// <summary>
/// Writes a log message for debugging.
/// </summary>
/// <param name="message">The message string to write.</param>
/// <param name="args">Arguments for the formatted message string.</param>
public delegate void DebugLogger(string message, params object?[] args);
