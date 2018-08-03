using System;
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

            if (IsDuplicate(@event.Exception))
            {
                // TODO: replace Debug with internal logging
                Debug.WriteLine("Duplicate Exception detected and discarded");
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
