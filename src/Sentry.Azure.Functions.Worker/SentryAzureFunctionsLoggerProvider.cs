using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;

namespace Sentry.Azure.Functions.Worker;

[ProviderAlias("Sentry")]
internal class SentryAzureFunctionsLoggerProvider : SentryLoggerProvider
{
    public SentryAzureFunctionsLoggerProvider(IOptions<SentryAzureFunctionsOptions> options, IHub hub)
        : base(options, hub)
    {
    }
}
