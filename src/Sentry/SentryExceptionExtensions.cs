using Sentry.Internal;
using Sentry.Protocol;

/// <summary>
/// Extends Exception with formatted data that can be used by Sentry SDK.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryExceptionExtensions
{
    /// <summary>
    /// The Handled State
    /// </summary>
    public enum ExceptionHandledState
    {
        /// <summary>
        /// The mechanism did not specify the handled state
        /// </summary>
        None,

        /// <summary>
        /// The exception was handled
        /// </summary>
        Handled,

        /// <summary>
        /// The exception was unhandled and terminal
        /// </summary>
        UnhandledTerminal,

        /// <summary>
        /// The exception was unhandled but non-terminal
        /// </summary>
        UnhandledNonTerminal
    }

    /// <summary>
    /// Set a tag that will be added to the event when the exception is captured.
    /// </summary>
    /// <param name="ex">The exception.</param>
    /// <param name="name">The name of the tag.</param>
    /// <param name="value">The value of the key.</param>
    public static void AddSentryTag(this Exception ex, string name, string value)
        => ex.Data.Add($"{MainExceptionProcessor.ExceptionDataTagKey}{name}", value);

    /// <summary>
    /// Set context data that will be added to the event when the exception is captured.
    /// </summary>
    /// <param name="ex">The exception.</param>
    /// <param name="name">The context name.</param>
    /// <param name="data">The context data.</param>
    public static void AddSentryContext(this Exception ex, string name, IReadOnlyDictionary<string, object> data)
        => ex.Data.Add($"{MainExceptionProcessor.ExceptionDataContextKey}{name}", data);

    /// <summary>
    /// Set mechanism information that will be included with the exception when it is captured.
    /// </summary>
    /// <param name="ex">The exception.</param>
    /// <param name="type">A required short string that identifies the mechanism.</param>
    /// <param name="description">An optional human-readable description of the mechanism.</param>
    /// <param name="handledState">An optional flag indicating whether the exception was handled by the mechanism.</param>
    public static void SetSentryMechanism(this Exception ex, string type, string? description = null,
        ExceptionHandledState handledState = ExceptionHandledState.None)
    {
        ex.Data[Mechanism.MechanismKey] = type;

        if (string.IsNullOrWhiteSpace(description))
        {
            ex.Data.Remove(Mechanism.DescriptionKey);
        }
        else
        {
            ex.Data[Mechanism.DescriptionKey] = description;
        }

        switch (handledState)
        {
            case ExceptionHandledState.None:
                ex.Data.Remove(Mechanism.HandledKey);
                break;
            case ExceptionHandledState.Handled:
                ex.Data[Mechanism.HandledKey] = true;
                break;
            case ExceptionHandledState.UnhandledTerminal:
                ex.Data[Mechanism.HandledKey] = false;
                ex.Data[Mechanism.TerminalKey] = true;
                break;
            case ExceptionHandledState.UnhandledNonTerminal:
                ex.Data[Mechanism.HandledKey] = false;
                ex.Data[Mechanism.TerminalKey] = false;
                break;
        }
    }
}
