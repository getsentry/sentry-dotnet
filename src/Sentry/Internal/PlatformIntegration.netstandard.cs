#if NETSTANDARD
using System;

namespace Sentry.Internal
{
    internal class PlatformIntegration : IInternalSdkIntegration
    {
        public void Register(IHub hub, SentryOptions options)
        {
        }

        public void Unregister(IHub hub)
        {
        }
    }
}
#endif
