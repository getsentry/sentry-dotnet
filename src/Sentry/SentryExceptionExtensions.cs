using Sentry;
using Sentry.Internal;
using Sentry.Protocol;

/// <summary>
/// Extends Exception with formatted data that can be used by Sentry SDK.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryExceptionExtensions
{
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
    /// <param name="handled">An optional flag indicating whether the exception was handled by the mechanism.</param>
    public static void SetSentryMechanism(this Exception ex, string type, string? description = null,
        bool? handled = null)
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

        if (handled == null)
        {
            ex.Data.Remove(Mechanism.HandledKey);
        }
        else
        {
            ex.Data[Mechanism.HandledKey] = handled;
        }
    }

    /// <summary>
    /// Recursively enumerates all <see cref="AggregateException.InnerExceptions"/> and <see cref="Exception.InnerException"/>
    /// Not for public use.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IEnumerable<Exception> EnumerateChainedExceptions(this Exception exception, SentryOptions options)
    {
        if (exception is AggregateException aggregateException)
        {
            foreach (var inner in EnumerateInner(options, aggregateException))
            {
                yield return inner;
            }

            if (!options.KeepAggregateException)
            {
                yield break;
            }
        }
        else if (exception.InnerException != null)
        {
            foreach (var inner in exception.InnerException.EnumerateChainedExceptions(options))
            {
                yield return inner;
            }
        }

        yield return exception;
    }

    private static IEnumerable<Exception> EnumerateInner(SentryOptions options, AggregateException aggregateException)
    {
        return aggregateException.InnerExceptions
            .SelectMany(exception => exception.EnumerateChainedExceptions(options));
    }
}
