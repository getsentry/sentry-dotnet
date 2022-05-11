using Microsoft.Extensions.DependencyInjection;

namespace Sentry.Extensions.Logging.Internal;

internal static class ServiceCollectionExtensions
{
    public static void TryAddExactSingleton<TService, TImplementation>(this IServiceCollection collection)
        where TService : class
        where TImplementation : class, TService =>
        collection.TryAddExactSingleton(typeof(TService), typeof(TImplementation));

    public static void TryAddExactTransient<TService, TImplementation>(this IServiceCollection collection)
        where TService : class
        where TImplementation : class, TService =>
        collection.TryAddExactTransient(typeof(TService), typeof(TImplementation));

    public static void TryAddExactSingleton(this IServiceCollection collection,
        Type serviceType, Type implementationType)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        if (!collection.IsRegistered(serviceType, implementationType))
        {
            collection.AddSingleton(serviceType, implementationType);
        }
    }

    public static void TryAddExactTransient(this IServiceCollection collection,
        Type serviceType, Type implementationType)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        if (!collection.IsRegistered(serviceType, implementationType))
        {
            collection.AddTransient(serviceType, implementationType);
        }
    }

    private static bool IsRegistered(this IServiceCollection collection, Type serviceType, Type implementationType) =>
        collection.Any(x => x.ServiceType == serviceType && x.ImplementationType == implementationType);
}
