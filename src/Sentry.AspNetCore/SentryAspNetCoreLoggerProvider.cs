using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;

namespace Sentry.AspNetCore
{
    internal class SentryAspNetCoreLoggerProvider : SentryLoggerProvider
    {
        public SentryAspNetCoreLoggerProvider(IOptions<SentryAspNetCoreOptions> options)
            : base(options)
        {
        }
    }
}
