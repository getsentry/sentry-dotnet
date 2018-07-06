using System;
using System.Collections.Generic;
using System.Text;

namespace Sentry.Extensibility
{
    public interface ISentryEventProcessor
    {
        void Process(SentryEvent @event);
    }
}
