using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;

namespace Sentry.AspNetCore;

/// <summary>
/// Structured Logger Provider for Sentry.
/// </summary>
[ProviderAlias("SentryLogs")]
[Experimental(Infrastructure.DiagnosticId.ExperimentalFeature)]
internal sealed class SentryAspNetCoreStructuredLoggerProvider : SentryStructuredLoggerProvider
{
    public SentryAspNetCoreStructuredLoggerProvider(IOptions<SentryAspNetCoreOptions> options, IHub hub)
        : base(options, hub)
    {
    }
}
