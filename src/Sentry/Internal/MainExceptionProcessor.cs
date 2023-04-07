using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Protocol;

namespace Sentry.Internal;

internal class MainExceptionProcessor : ISentryEventExceptionProcessor
{
    private const string ExceptionDataKeyPrefix = "sentry:";
    internal const string ExceptionDataTagKey = ExceptionDataKeyPrefix + "tag:";
    internal const string ExceptionDataContextKey = ExceptionDataKeyPrefix + "context:";

    private readonly SentryOptions _options;
    internal Func<ISentryStackTraceFactory> SentryStackTraceFactoryAccessor { get; }

    public MainExceptionProcessor(SentryOptions options, Func<ISentryStackTraceFactory> sentryStackTraceFactoryAccessor)
    {
        _options = options;
        SentryStackTraceFactoryAccessor = sentryStackTraceFactoryAccessor;
    }

    public void Process(Exception exception, SentryEvent sentryEvent)
    {
        _options.LogDebug("Running processor on exception: {0}", exception.Message);

        var sentryExceptions = CreateSentryExceptions(exception);

        MoveExceptionDataToEvent(sentryEvent, sentryExceptions);

        sentryEvent.SentryExceptions = sentryExceptions;
    }

    private static void MoveExceptionDataToEvent(SentryEvent sentryEvent, IEnumerable<SentryException> sentryExceptions)
    {
        var keysToRemove = new List<string>();

        var i = 0;
        foreach (var sentryException in sentryExceptions)
        {
            var data = sentryException.Mechanism?.Data;
            if (data is null || data.Count == 0)
            {
                i++;
                continue;
            }

            foreach (var (key, value) in data)
            {
                if (key.Length > ExceptionDataTagKey.Length &&
                    value is string stringValue &&
                    key.StartsWith(ExceptionDataTagKey, StringComparison.OrdinalIgnoreCase))
                {
                    sentryEvent.SetTag(key[ExceptionDataTagKey.Length..], stringValue);
                    keysToRemove.Add(key);
                }
                else if (key.Length > ExceptionDataContextKey.Length &&
                         !value.IsNull() &&
                         key.StartsWith(ExceptionDataContextKey, StringComparison.OrdinalIgnoreCase))
                {
                    sentryEvent.Contexts[key[ExceptionDataContextKey.Length..]] = value;
                    keysToRemove.Add(key);
                }
                else if (key.StartsWith(ExceptionDataKeyPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    sentryEvent.SetExtra($"Exception[{i}][{key}]", value);
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                data.Remove(key);
            }

            keysToRemove.Clear();
            i++;
        }
    }

    internal List<SentryException> CreateSentryExceptions(Exception exception)
    {
        var exceptions = exception
            .EnumerateChainedExceptions(_options)
            .Select(BuildSentryException)
            .ToList();

        // If we've filtered out the aggregate exception, we'll need to copy over details from it.
        if (exception is AggregateException && !_options.KeepAggregateException)
        {
            var original = BuildSentryException(exception);

            // Exceptions are sent from oldest to newest, so the details belong on the LAST exception.
            var last = exceptions.Last();
            last.Mechanism = original.Mechanism;

            // In some cases the stack trace is already positioned on the inner exception.
            // Only copy it over when it is missing.
            last.Stacktrace ??= original.Stacktrace;
        }

        return exceptions;
    }

    private SentryException BuildSentryException(Exception exception)
    {
        var sentryEx = new SentryException
        {
            Type = exception.GetType().FullName,
            Module = exception.GetType().Assembly.FullName,
            Value = exception.Message,
            ThreadId = Environment.CurrentManagedThreadId
        };

        var mechanism = GetMechanism(exception);
        if (!mechanism.IsDefaultOrEmpty())
        {
            sentryEx.Mechanism = mechanism;
        }

        sentryEx.Stacktrace = SentryStackTraceFactoryAccessor().Create(exception);
        return sentryEx;
    }

    private static Mechanism GetMechanism(Exception exception)
    {
        var mechanism = new Mechanism();

        if (exception.HelpLink != null)
        {
            mechanism.HelpLink = exception.HelpLink;
        }

        if (exception.Data[Mechanism.HandledKey] is bool handled)
        {
            // The mechanism handled flag was set by an integration.
            mechanism.Handled = handled;
            exception.Data.Remove(Mechanism.HandledKey);
        }
        else if (exception.StackTrace != null)
        {
            // The exception was thrown, but it was caught by the user, not an integration.
            // Thus, we can mark it as handled.
            mechanism.Handled = true;
        }
        else
        {
            // The exception was never thrown.  It was just constructed and then captured.
            // Thus, it is neither handled nor unhandled.
            mechanism.Handled = null;
        }

        if (exception.Data[Mechanism.MechanismKey] is string mechanismType)
        {
            mechanism.Type = mechanismType;
            exception.Data.Remove(Mechanism.MechanismKey);
        }

        if (exception.Data[Mechanism.DescriptionKey] is string mechanismDescription)
        {
            mechanism.Description = mechanismDescription;
            exception.Data.Remove(Mechanism.DescriptionKey);
        }

        // Copy remaining exception data to mechanism data.
        foreach (var key in exception.Data.Keys.OfType<string>())
        {
            mechanism.Data[key] = exception.Data[key]!;
        }

        return mechanism;
    }
}
