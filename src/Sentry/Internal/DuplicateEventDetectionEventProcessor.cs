using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Sentry.Extensibility;

namespace Sentry.Internal
{
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
                    _options.DiagnosticLogger?.LogDebug("Same event instance detected and discarded. EventId: {0}", @event.EventId);
                    return null;
                }
                _capturedObjects.Add(@event, null);
            }

            if (@event.Exception == null
                || !IsDuplicate(@event.Exception))
            {
                return @event;
            }

            _options.DiagnosticLogger?.LogDebug("Duplicate Exception detected. Event {0} will be discarded.", @event.EventId);
            return null;
        }

        private bool IsDuplicate(Exception ex)
        {
            if (_options.DeduplicateMode.HasFlag(DeduplicateMode.SameExceptionInstance))
            {
                if (_capturedObjects.TryGetValue(ex, out _))
                {
                    return true;
                }

                _capturedObjects.Add(ex, null);
            }

            if (_options.DeduplicateMode.HasFlag(DeduplicateMode.AggregateException)
                && ex is AggregateException aex)
            {
                return aex.InnerExceptions.Any(IsDuplicate);
            }

            if (_options.DeduplicateMode.HasFlag(DeduplicateMode.InnerException)
                && ex.InnerException != null)
            {
                if (IsDuplicate(ex.InnerException))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
