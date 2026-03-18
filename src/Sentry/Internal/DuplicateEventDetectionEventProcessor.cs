using Sentry.Extensibility;

namespace Sentry.Internal;

internal class DuplicateEventDetectionEventProcessor : ISentryEventProcessor
{
    private readonly SentryOptions _options;
    private readonly ConditionalWeakTable<object, object?> _capturedObjects = new();

    public DuplicateEventDetectionEventProcessor(SentryOptions options) => _options = options;

    public SentryEvent? Process(SentryEvent @event)
    {
        if (_options.DeduplicateMode.HasFlag(DeduplicateMode.SameEvent))
        {
            if (_capturedObjects.TryGetValue(@event, out _))
            {
                _options.LogDebug("Same event instance detected and discarded. EventId: '{0}'", @event.EventId);
                return null;
            }
            _capturedObjects.Add(@event, null);
        }

        if (@event.Exception == null
            || !IsDuplicate(@event.Exception, @event.EventId, true))
        {
            return @event;
        }

        return null;
    }

    private bool IsDuplicate(Exception ex, SentryId eventId, bool debugLog)
    {
        if (_options.DeduplicateMode.HasFlag(DeduplicateMode.SameExceptionInstance))
        {
            if (_capturedObjects.TryGetValue(ex, out _))
            {
                if (debugLog)
                {
                    _options.LogDebug("Duplicate Exception: 'SameExceptionInstance'. Event '{0}' will be discarded.", eventId);
                }
                return true;
            }

            _capturedObjects.Add(ex, null);
        }

        if (_options.DeduplicateMode.HasFlag(DeduplicateMode.AggregateException)
            && ex is AggregateException aex)
        {
            var result = aex.InnerExceptions.Any(e => IsDuplicate(e, eventId, false));
            if (result)
            {
                _options.LogDebug("Duplicate Exception: 'AggregateException'. Event '{0}' will be discarded.", eventId);
            }

            return result;
        }

        if (_options.DeduplicateMode.HasFlag(DeduplicateMode.InnerException)
            && ex.InnerException != null)
        {
            if (IsDuplicate(ex.InnerException, eventId, false))
            {
                _options.LogDebug("Duplicate Exception: 'SameExceptionInstance'. Event '{0}' will be discarded.", eventId);
                return true;
            }
        }

        return false;
    }
}
