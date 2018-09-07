using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;

namespace Sentry.AspNetCore
{
    [ProviderAlias("Sentry")]
    internal class SentryAspNetCoreLoggerProvider : SentryLoggerProvider
    {
        public SentryAspNetCoreLoggerProvider(IOptions<SentryAspNetCoreOptions> options, IHub hub)
            : base(options, hub)
        {
        }
    }
}
