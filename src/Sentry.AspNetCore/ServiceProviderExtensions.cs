using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Sentry.AspNetCore;

internal static class ServiceProviderExtensions
{
    public static IEnumerable<T> GetServices<T>(this IServiceProvider serviceProvider, ServiceLifetime[] lifetimes) =>
        serviceProvider
            .GetRequiredService<LifetimeServiceResolver>()
            .GetServices<T>(serviceProvider, lifetimes);

    public static IEnumerable<T> GetNonScoped<T>(this IServiceProvider serviceProvider) =>
        serviceProvider
            .GetRequiredService<LifetimeServiceResolver>()
            .GetServices<T>(serviceProvider, [ServiceLifetime.Singleton, ServiceLifetime.Transient]);
}
