using System;
using System.Diagnostics;
using Sentry.Extensibility;

namespace Sentry.Internal
{
    internal class DelegateEventProcessor : ISentryEventProcessor
    {
        private readonly Func<SentryEvent, SentryEvent> _func;

        public DelegateEventProcessor(Func<SentryEvent, SentryEvent> func)
        {
            Debug.Assert(func != null);
            _func = func;
        }

        public SentryEvent Process(SentryEvent @event) => _func(@event);
    }
}
