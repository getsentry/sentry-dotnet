using System;
using Sentry.Extensibility;

namespace Sentry.Internal
{
    internal class DelegateEventProcessor : ISentryEventProcessor
    {
        private readonly Func<SentryEvent, SentryEvent> _func;

        public DelegateEventProcessor(Func<SentryEvent, SentryEvent> func)
        {
            _func = func;
        }

        public SentryEvent Process(SentryEvent @event) => _func(@event);
    }
}
