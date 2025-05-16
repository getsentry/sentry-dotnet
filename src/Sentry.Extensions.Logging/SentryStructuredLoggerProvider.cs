using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Logging;

/// <summary>
/// Sentry Structured Logger Provider.
/// </summary>
[ProviderAlias("SentryLogs")]
[Experimental(Infrastructure.DiagnosticId.ExperimentalFeature)]
internal sealed class SentryStructuredLoggerProvider : ILoggerProvider
{
    private readonly IOptions<SentryLoggingOptions> _options;
    private readonly IHub _hub;

    // TODO: convert this comment into an automated test
    // Constructor must be public for Microsoft.Extensions.DependencyInjection
    public SentryStructuredLoggerProvider(IOptions<SentryLoggingOptions> options, IHub hub)
    {
        _options = options;
        _hub = hub;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new SentryStructuredLogger(categoryName, _options.Value, _hub);
    }

    public void Dispose()
    {
    }
}
