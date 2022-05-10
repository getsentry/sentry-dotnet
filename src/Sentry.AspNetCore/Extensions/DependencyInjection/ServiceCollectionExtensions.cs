using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sentry.AspNetCore;
using Sentry.Extensibility;
using Sentry.Extensions.Logging.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace -- Discoverability
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Sentry's services to the <see cref="IServiceCollection"/>
    /// </summary>
    /// <param name="services">The services.</param>
    public static ISentryBuilder AddSentry(this IServiceCollection services)
    {
        if (!services.IsRegistered<ISentryEventProcessor, AspNetCoreEventProcessor>())
        {
            services.AddSingleton<ISentryEventProcessor, AspNetCoreEventProcessor>();
        }

        if (!services.IsRegistered<ISentryEventExceptionProcessor, AspNetCoreExceptionProcessor>())
        {
            services.AddSingleton<ISentryEventExceptionProcessor, AspNetCoreExceptionProcessor>();
        }

        services.TryAddSingleton<IUserFactory, DefaultUserFactory>();

        if (!services.IsRegistered<IRequestPayloadExtractor, FormRequestPayloadExtractor>())
        {
            services.AddSingleton<IRequestPayloadExtractor, FormRequestPayloadExtractor>();
        }

        // Last
        if (!services.IsRegistered<IRequestPayloadExtractor, DefaultRequestPayloadExtractor>())
        {
            services.AddSingleton<IRequestPayloadExtractor, DefaultRequestPayloadExtractor>();
        }

        services.AddSentry<SentryAspNetCoreOptions>();

        return new SentryAspNetCoreBuilder(services);
    }

    /// <summary>
    /// Checks if a specific ServiceDescriptor was previously registered in the <see cref="IServiceCollection"/>
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TU"></typeparam>
    /// <returns></returns>
    internal static bool IsRegistered<T, TU>(this IServiceCollection serviceCollection)
    {
        return serviceCollection.Any(x => x.ImplementationType == typeof(T) && x.ServiceType == typeof(TU));
    }

    /// <summary>
    /// Adds Sentry's StartupFilter to the <see cref="IServiceCollection"/>
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <returns></returns>
    internal static IServiceCollection AddSentryStartupFilter(this IServiceCollection serviceCollection)
    {
        if (!serviceCollection.IsRegistered<IStartupFilter, SentryStartupFilter>())
        {
            _ = serviceCollection.AddTransient<IStartupFilter, SentryStartupFilter>();
        }

        return serviceCollection;
    }
}
