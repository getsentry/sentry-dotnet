using System;
using System.Runtime.CompilerServices;
using Sentry.Extensibility;

namespace Sentry.Internal
{
    internal class DuplicateEventDetectionEventProcessor : ISentryEventProcessor
    {
        private readonly SentryOptions _options;
        private readonly ConditionalWeakTable<object, object> _capturedEvent = new ConditionalWeakTable<object, object>();

        public DuplicateEventDetectionEventProcessor(SentryOptions options) => _options = options;

        public SentryEvent Process(SentryEvent @event)
        {
            if (_capturedEvent.TryGetValue(@event, out _))
            {
                _options.DiagnosticLogger?.LogDebug("Same event instance detected and discarded. EventId: {0}", @event.EventId);
                return null;
            }
            _capturedEvent.Add(@event, null);

            if (@event.Exception == null)
            {
                return @event;
            }

            if (IsDuplicate(@event.Exception))
            {
                _options.DiagnosticLogger?.LogDebug("Duplicate Exception detected. Event {0} will be discarded.", @event.EventId);
                return null;
            }

            return @event;
        }

        private bool IsDuplicate(Exception ex)
        {
            if (ex == null)
            {
                return false;
            }

            while (true)
            {
                if (_capturedEvent.TryGetValue(ex, out _))
                {
                    return true;
                }

                _capturedEvent.Add(ex, null);

                if (ex is AggregateException aex)
                {
                    foreach (var aexInnerException in aex.InnerExceptions)
                    {
                        if (IsDuplicate(aexInnerException))
                        {
                            return true;
                        }
                    }
                }
                else if (ex.InnerException != null)
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
}
