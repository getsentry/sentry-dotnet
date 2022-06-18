using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Sentry;
using Sentry.Internal;

/// <summary>
/// Extends Exception with formatted data that can be used by Sentry SDK.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryExceptionExtensions
{
    /// <summary>
    /// Set a Sentry's Tag to the Exception.
    /// </summary>
    /// <param name="ex">The exception.</param>
    /// <param name="name">The name of the tag.</param>
    /// <param name="value">The value of the key.</param>
    public static void AddSentryTag(this Exception ex, string name, string value)
        => ex.Data.Add($"{MainExceptionProcessor.ExceptionDataTagKey}{name}", value);

    /// <summary>
    /// Recursively enumerates all <see cref="AggregateException.InnerExceptions"/> and <see cref="Exception.InnerException"/>
    /// Not for public use.
    /// </summary>
    public static IEnumerable<Exception> EnumerateChainedExceptions(this Exception exception, SentryOptions options)
    {
        if (exception is AggregateException aggregateException)
        {
            foreach (var inner in aggregateException.InnerExceptions
                         .SelectMany(_ => _.EnumerateChainedExceptions(options)))
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

    /// <summary>
    /// Set a Sentry's structured Context to the Exception.
    /// </summary>
    /// <param name="ex">The exception.</param>
    /// <param name="name">The context name.</param>
    /// <param name="data">The context data.</param>
    public static void AddSentryContext(this Exception ex, string name, IReadOnlyDictionary<string, object> data)
        => ex.Data.Add($"{MainExceptionProcessor.ExceptionDataContextKey}{name}", data);
}
