using Microsoft.Extensions.DependencyInjection;

namespace Sentry.Extensions.Logging;

/// <summary>
/// Allows for further customization of Sentry integration
/// </summary>
internal class SentryBuilder : ISentryBuilder
{
    public IServiceCollection Services { get; }

    public SentryBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }
}
