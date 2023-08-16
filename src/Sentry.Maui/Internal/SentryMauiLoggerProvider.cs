using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;

namespace Sentry.Maui.Internal;

[ProviderAlias("Sentry")]
internal class SentryMauiLoggerProvider : SentryLoggerProvider
{
    public SentryMauiLoggerProvider(IOptions<SentryMauiOptions> options, IHub hub)
        : base(options, hub)
    {
    }
}
