using Microsoft.Extensions.DependencyInjection;

namespace Sentry.AspNetCore;

/// <summary>
/// Allows for further customization of Sentry integration
/// </summary>
public interface ISentryBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> where Sentry services are configured.
    /// </summary>
    public IServiceCollection Services { get; }
}
