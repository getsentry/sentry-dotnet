#if WINRT
using System;

namespace Sentry.Internal
{
    internal class PlatformIntegration : IInternalSdkIntegration
    {
        public void Register(IHub hub, SentryOptions options)
        {
            throw new NotImplementedException();
        }

        public void Unregister(IHub hub)
        {
            throw new NotImplementedException();
        }
    }
}
#endif
