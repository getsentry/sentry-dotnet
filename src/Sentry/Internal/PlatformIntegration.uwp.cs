#if WINDOWS_UWP
using System;

namespace Sentry.Internal
{
    internal class PlatformIntegration : IInternalSdkIntegration
    {
        public void Register(IHub hub, SentryOptions options)
        {
            options.AddEventProcessor(new PlatformEventProcessor(options));
        }

        public void Unregister(IHub hub)
        {
            throw new NotImplementedException();
        }
    }
}
#endif
