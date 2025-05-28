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

    // Sentry exceptions are sorted oldest to newest.
    // See https://develop.sentry.dev/sdk/event-payloads/exception
    internal IReadOnlyList<SentryException> CreateSentryExceptions(Exception exception)
    {
        var exceptions = WalkExceptions(exception).Reverse().ToList();

        // In the case of only one exception, ExceptionId and ParentId are useless.
        if (exceptions.Count == 1 && exceptions[0].Mechanism is { } mechanism)
        {
            mechanism.ExceptionId = null;
            mechanism.ParentId = null;
            if (mechanism.IsDefaultOrEmpty())
            {
                // No need to convey an empty mechanism.
                exceptions[0].Mechanism = null;
            }
        }

        return exceptions;
    }

    private class Counter
    {
        private int _value;

        public int GetNextValue() => _value++;
    }

    private IEnumerable<SentryException> WalkExceptions(Exception exception) =>
        WalkExceptions(exception, new Counter(), null, null);

    private IEnumerable<SentryException> WalkExceptions(Exception exception, Counter counter, int? parentId, string? source)
    {
        var ex = exception;
        while (ex is not null)
        {
            var id = counter.GetNextValue();

            yield return BuildSentryException(ex, id, parentId, source);

            if (ex is AggregateException aex)
            {
                for (var i = 0; i < aex.InnerExceptions.Count; i++)
                {
                    ex = aex.InnerExceptions[i];
                    source = $"{nameof(AggregateException.InnerExceptions)}[{i}]";
                    var sentryExceptions = WalkExceptions(ex, counter, id, source);
                    foreach (var sentryException in sentryExceptions)
                    {
                        yield return sentryException;
                    }
                }

                break;
            }

            ex = ex.InnerException;
            parentId = id;
            source = nameof(AggregateException.InnerException);
        }
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

    private SentryException BuildSentryException(Exception exception, int id, int? parentId, string? source)
    {
        var sentryEx = new SentryException
        {
            Type = exception.GetType().FullName,
            Module = exception.GetType().Assembly.FullName,
            Value = exception is AggregateException agg ? agg.GetRawMessage() : exception.Message,
            ThreadId = Environment.CurrentManagedThreadId
        };

        var mechanism = GetMechanism(exception, id, parentId, source);
        if (!mechanism.IsDefaultOrEmpty())
        {
            sentryEx.Mechanism = mechanism;
        }

        sentryEx.Stacktrace ??= SentryStackTraceFactoryAccessor().Create(exception);
        return sentryEx;
    }

    private static Mechanism GetMechanism(Exception exception, int id, int? parentId, string? source)
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

        // Add HResult to mechanism data before adding exception data, so that it can be overridden.
        mechanism.Data["HResult"] = $"0x{exception.HResult:X8}";

        // Copy remaining exception data to mechanism data.
        foreach (var key in exception.Data.Keys.OfType<string>())
        {
            mechanism.Data[key] = exception.Data[key]!;
        }

        mechanism.ExceptionId = id;
        mechanism.ParentId = parentId;
        mechanism.Source = source;
        mechanism.IsExceptionGroup = exception is AggregateException;

        if (source != null)
        {
            mechanism.Type = "chained";
        }

        return mechanism;
    }
}
