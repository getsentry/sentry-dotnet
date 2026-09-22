using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Infrastructure;

namespace Sentry.Extensions.Logging;

/// <summary>
/// Sentry Logger Provider for <see cref="Breadcrumb"/> and <see cref="SentryEvent"/>.
/// </summary>
[ProviderAlias("Sentry")]
internal class SentryLoggerProvider : ILoggerProvider
{
    private readonly ISystemClock _clock;
    private readonly SentryLoggingOptions _options;

    internal IHub Hub { get; }

    /// <summary>
    /// Creates a new instance of <see cref="SentryLoggerProvider"/>.
    /// </summary>
    /// <param name="options">The Options.</param>
    /// <param name="hub">The Hub.</param>
    public SentryLoggerProvider(IOptions<SentryLoggingOptions> options, IHub hub)
        : this(hub,
            SystemClock.Clock,
            options.Value)
    { }

    internal SentryLoggerProvider(
        IHub hub,
        ISystemClock clock,
        SentryLoggingOptions options)
    {
        Hub = hub;
        _clock = clock;
        _options = options;
    }

    /// <summary>
    /// Creates a logger for the category.
    /// </summary>
    /// <param name="categoryName">Category name.</param>
    /// <returns>A logger.</returns>
    public ILogger CreateLogger(string categoryName) => new SentryLogger(categoryName, _options, _clock, Hub);

    public void Dispose()
    {
    }
}
