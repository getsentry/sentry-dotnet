using Microsoft.Extensions.DependencyInjection;

namespace Sentry.AspNetCore;

internal class LifetimeServiceResolver(IServiceCollection services)
{
    public IEnumerable<T> GetServices<T>(IServiceProvider provider,
        params ServiceLifetime[] lifetimes)
    {
        return Factories<T>(lifetimes)
            .Select(factory => factory(provider));
    }

    private IEnumerable<Func<IServiceProvider, T>> Factories<T>(ServiceLifetime[] lifetimes)
    {
        foreach (var descriptor in services)
        {
            if (descriptor.ServiceType != typeof(T) || !lifetimes.Contains(descriptor.Lifetime))
            {
                continue;
            }
            if (descriptor.ImplementationInstance is not null)
            {
                yield return _ => (T)descriptor.ImplementationInstance;
            }
            if (descriptor.ImplementationFactory is not null)
            {
                yield return provider => (T)descriptor.ImplementationFactory(provider);
            }
            if (descriptor.ImplementationType is not null)
            {
                yield return provider => (T)ActivatorUtilities.GetServiceOrCreateInstance(provider, descriptor.ImplementationType);
            }
        }
    }
}
