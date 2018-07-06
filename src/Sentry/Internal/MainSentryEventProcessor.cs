using System;
using System.Diagnostics;
using Sentry.Extensibility;

namespace Sentry.Internal
{
    internal class MainSentryEventProcessor : ISentryEventProcessor
    {
        internal static readonly Lazy<string> Release = new Lazy<string>(ReleaseLocator.GetCurrent);

        private readonly SentryOptions _options;

        public MainSentryEventProcessor(SentryOptions options)
        {
            Debug.Assert(options != null);
            _options = options;
        }

        public void Process(SentryEvent @event)
        {
            if (@event.Release == null)
            {
                @event.Release = _options.Release ?? Release.Value;
            }

            if (@event.Exception != null)
            {
                // Depends on Options instead of the processors to allow application adding new processors
                // after the SDK is initialized. Useful for example once a DI container is up
                foreach (var processor in _options.GetExceptionProcessors())
                {
                    processor.Process(@event.Exception, @event);
                }
            }
        }
    }
}
