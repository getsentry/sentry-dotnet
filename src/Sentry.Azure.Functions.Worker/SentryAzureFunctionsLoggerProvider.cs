using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;

namespace Sentry.Azure.Functions.Worker;

[ProviderAlias("Sentry")]
internal class SentryAzure.FunctionsLoggerProvider : SentryLoggerProvider
{
    public SentryAzure.FunctionsLoggerProvider(IOptions<SentryAzureFunctionsOptions> options, IHub hub)
        : base(options, hub)
    {
    }
}
