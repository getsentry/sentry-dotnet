using System.Diagnostics;
using System.Runtime.CompilerServices;
using Sentry.Extensibility;

namespace Sentry.Internal
{
    internal class DuplicateEventDetectionEventProcessor : ISentryEventProcessor
    {
        private readonly ConditionalWeakTable<object, object> _capturedEvent = new ConditionalWeakTable<object, object>();

        public SentryEvent Process(SentryEvent @event)
        {
            if (_capturedEvent.TryGetValue(@event, out _))
            {
                Debug.WriteLine("Same event instance detected and discarded");
                return null;
            }
            _capturedEvent.Add(@event, null);

            if (@event.Exception == null)
            {
                return @event;
            }

            if (_capturedEvent.TryGetValue(@event.Exception, out _))
            {
                // TODO: replace Debug with internal logging
                Debug.WriteLine("Duplicate Exception detected and discarded");
                return null;
            }

            _capturedEvent.Add(@event.Exception, null);
            return @event;
        }
    }
}
