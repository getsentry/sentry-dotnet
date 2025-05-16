using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;

namespace Sentry.Maui.Internal;

[ProviderAlias("SentryLogs")]
[Experimental(Infrastructure.DiagnosticId.ExperimentalFeature)]
internal sealed class SentryMauiStructuredLoggerProvider : SentryStructuredLoggerProvider
{
    public SentryMauiStructuredLoggerProvider(IOptions<SentryMauiOptions> options, IHub hub)
        : base(options, hub)
    {
    }
}
