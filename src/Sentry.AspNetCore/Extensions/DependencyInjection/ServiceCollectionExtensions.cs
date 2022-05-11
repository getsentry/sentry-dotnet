using System.ComponentModel;
using Sentry.AspNetCore;
using Sentry.Extensibility;
using Sentry.Extensions.Logging.Extensions.DependencyInjection;
using Sentry.Extensions.Logging.Internal;
using ISentryBuilder = Sentry.AspNetCore.ISentryBuilder;

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
        services.TryAddExactSingleton<ISentryEventProcessor, AspNetCoreEventProcessor>();
        services.TryAddExactSingleton<ISentryEventExceptionProcessor, AspNetCoreExceptionProcessor>();
        services.TryAddExactSingleton<IUserFactory, DefaultUserFactory>();
        services.TryAddExactSingleton<IRequestPayloadExtractor, FormRequestPayloadExtractor>();
        services.TryAddExactSingleton<IRequestPayloadExtractor, DefaultRequestPayloadExtractor>();
        services.AddSentry<SentryAspNetCoreOptions>();

        return new SentryAspNetCoreBuilder(services);
    }

    /// <summary>
    /// Adds a <see cref="SentryStartupFilter"/> to the <paramref name="serviceCollection"/>.
    /// </summary>
    internal static IServiceCollection AddSentryStartupFilter(this IServiceCollection serviceCollection)
    {
        serviceCollection.TryAddExactTransient<IStartupFilter, SentryStartupFilter>();

        return serviceCollection;
    }
}
