using Microsoft.Extensions.Logging;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;

namespace Sentry.Extensions.Logging;

internal sealed class SentryLogger : ILogger
{
    private readonly IHub _hub;
    private readonly ISystemClock _clock;
    private readonly SentryLoggingOptions _options;
    private readonly UninitializedSdkWarning _uninitializedSdkWarning;

    internal string CategoryName { get; }

    internal const string UninitializedSdkMessage =
        "Sentry: the Microsoft.Extensions.Logging integration dropped a log event because Sentry is not " +
        "initialized, but a DSN was found in the environment or in an assembly attribute. The logging " +
        "integration no longer initializes the SDK: call SentrySdk.Init (or UseSentry via one of the " +
        "integrations) at startup. See https://docs.sentry.io/platforms/dotnet/guides/extensions-logging/";

    internal SentryLogger(
        string categoryName,
        SentryLoggingOptions options,
        ISystemClock clock,
        IHub hub,
        UninitializedSdkWarning? uninitializedSdkWarning = null)
    {
        CategoryName = categoryName;
        _options = options;
        _clock = clock;
        _hub = hub;
        _uninitializedSdkWarning = uninitializedSdkWarning ?? new UninitializedSdkWarning();
    }

#if NET8_0_OR_GREATER
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
        => _hub.PushScope(state);
#else
    public IDisposable BeginScope<TState>(TState state) => _hub.PushScope(state);
#endif

    public bool IsEnabled(LogLevel logLevel) => _hub.IsEnabled && IsEnabledForLevel(logLevel);

    private bool IsEnabledForLevel(LogLevel logLevel)
        => logLevel != LogLevel.None
           && (logLevel >= _options.MinimumBreadcrumbLevel
               || logLevel >= _options.MinimumEventLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string>? formatter)
    {
        if (!_hub.IsEnabled)
        {
            if (WouldCaptureEvent(logLevel, eventId, exception))
            {
                _uninitializedSdkWarning.WarnOnce(UninitializedSdkMessage);
            }

            return;
        }

        if (!IsEnabledForLevel(logLevel))
        {
            return;
        }

        var message = formatter?.Invoke(state, exception);

        if (IsFromSentry() || IsEfExceptionMessage(eventId) || IsFiltered(logLevel, eventId, exception))
        {
            return;
        }

        if (ShouldCaptureEvent(logLevel))
        {
            var @event = CreateEvent(logLevel, eventId, state, exception, message, CategoryName);

            _ = _hub.CaptureEvent(@event);

            // Capturing exception events adds a breadcrumb automatically... we don't want to add another one
            if (exception != null)
            {
                return;
            }
        }

        if (ShouldAddBreadcrumb(logLevel))
        {
            var data = eventId.ToDictionaryOrNull();

            if (exception != null && message != null)
            {
                // Exception.Message won't be used as Breadcrumb message
                // Avoid losing it by adding as data:
                data ??= new Dictionary<string, string>();
                data.Add("exception_message", exception.Message);
            }

            _hub.AddBreadcrumb(
                _clock,
                (message ?? exception?.Message)!,
                CategoryName,
                null,
                data,
                logLevel.ToBreadcrumbLevel(),
                exception.ToHint());
        }
    }

    internal static SentryEvent CreateEvent<TState>(
        LogLevel logLevel,
        EventId id,
        TState state,
        Exception? exception,
        string? message,
        string category)
    {
        exception?.SetSentryMechanism("SentryLogger", handled: !IsUnhandledWasmException(id));
        var @event = new SentryEvent(exception)
        {
            Logger = category,
            Message = message,
            Level = logLevel.ToSentryLevel(),
        };

        if (state is IEnumerable<KeyValuePair<string, object>> pairs)
        {
            foreach (var property in pairs)
            {
                if (property.Key == "{OriginalFormat}" && property.Value is string template)
                {
                    // Original format found, use Sentry logEntry interface
                    @event.Message = new SentryMessage
                    {
                        Formatted = message,
                        Message = template
                    };
                    continue;
                }

                switch (property.Value)
                {
                    case string stringTagValue:
                        @event.SetTag(property.Key, stringTagValue);
                        break;

                    case Guid guidTagValue when guidTagValue != Guid.Empty:
                        @event.SetTag(property.Key, guidTagValue.ToString());
                        break;

                    case Enum enumValue:
                        @event.SetTag(property.Key, enumValue.ToString());
                        break;

                    default:
                        {
                            if (property.Value?.GetType().IsPrimitive == true)
                            {
                                @event.SetTag(property.Key, Convert.ToString(property.Value, CultureInfo.InvariantCulture)!);
                            }
                            break;
                        }
                }
            }
        }

        var tuple = id.ToTupleOrNull();
        if (tuple.HasValue)
        {
            @event.SetTag(tuple.Value.name, tuple.Value.value);
        }

        return @event;
    }

    private bool ShouldCaptureEvent(LogLevel logLevel)
        => _options.MinimumEventLevel != LogLevel.None
           && logLevel >= _options.MinimumEventLevel;

    private bool WouldCaptureEvent(LogLevel logLevel, EventId eventId, Exception? exception)
        => logLevel != LogLevel.None
           && ShouldCaptureEvent(logLevel)
           && !IsFromSentry()
           && !IsEfExceptionMessage(eventId)
           && !IsFiltered(logLevel, eventId, exception);

    private bool ShouldAddBreadcrumb(LogLevel logLevel)
        => _options.MinimumBreadcrumbLevel != LogLevel.None
           && logLevel >= _options.MinimumBreadcrumbLevel;

    private bool IsFiltered(
        LogLevel logLevel,
        EventId eventId,
        Exception? exception)
        => _options.Filters.Any(f => IsFiltered(f, logLevel, eventId, exception));

    private bool IsFiltered(
        ILogEntryFilter filter,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception)
    {
        try
        {
            return filter.Filter(CategoryName, logLevel, eventId, exception);
        }
        catch (Exception e)
        {
            _hub.GetSentryOptions()?.LogError(e, "The {0} log filter callback failed. The log entry will be filtered out.", filter.GetType().Name);
            return true;
        }
    }


    private bool IsFromSentry() => SentrySdkNamespaces.IsSentrySdk(CategoryName);

    internal static bool IsEfExceptionMessage(EventId eventId)
    {
        return eventId.Name is
                "Microsoft.EntityFrameworkCore.Update.SaveChangesFailed" or
                "Microsoft.EntityFrameworkCore.Query.QueryIterationFailed" or
                "Microsoft.EntityFrameworkCore.Query.InvalidIncludePathError" or
                "Microsoft.EntityFrameworkCore.Update.OptimisticConcurrencyException";
    }

    internal static bool IsUnhandledWasmException(EventId eventId)
    {
        return eventId.Name is
                "ExceptionRenderingComponent" or
                "NavigationFailed";
    }
}
