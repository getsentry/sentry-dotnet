using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Extensibility;

// ReSharper disable once CheckNamespace -- Discoverability
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Sentry
        /// </summary>
        /// <remarks>
        /// Either the SDK has already been initialized or a DSN was already set:
        /// The DSN could have been set, for example, via
        /// Environment variable SENTRY_DSN or assembly attribute <see cref="DsnAttribute"/>
        /// </remarks>
        public static IServiceCollection AddSentry(this IServiceCollection services)
            => services.AddSentry((Action<SentryOptions>)null);

        /// <summary>
        /// Adds the Sentry with the specified DSN.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="dsn">The DSN.</param>
        /// <returns></returns>
        public static IServiceCollection AddSentry(
                    this IServiceCollection services,
                    string dsn)
            => services.AddSentry(o => o.Dsn = new Dsn(dsn));

        /// <summary>
        /// Adds Sentry's services to the <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configureOptions">The configure options.</param>
        /// <returns></returns>
        public static IServiceCollection AddSentry(
                    this IServiceCollection services,
                    Action<SentryOptions> configureOptions)
        {
            services
                .AddSingleton<IRequestPayloadExtractor, FormRequestPayloadExtractor>()
                // Last
                .AddSingleton<IRequestPayloadExtractor, DefaultRequestPayloadExtractor>()
                .TryAddSingleton(p =>
                {
                    if (configureOptions != null && !SentryCore.IsEnabled)
                    {
                        var aspnetOptions = p.GetService<SentryAspNetCoreOptions>();
                        var options = p.GetService<SentryOptions>();
                        options = options ?? new SentryOptions();

                        aspnetOptions.InitSdk?.Invoke(options);
                        configureOptions?.Invoke(options);

                        var lifetime = p.GetRequiredService<IApplicationLifetime>();
                        var disposable = SentryCore.Init(options);
                        lifetime.ApplicationStopped.Register(() => disposable.Dispose());
                    }

                    // TODO: SDK interface not accepting 'Integrations'
                    // SentryCore.ConfigureScope(s => s.Sdk.AddIntegration(Constants.IntegrationName));

                    // TODO: Need to fetch the created client and hub now to register in DI
                    return HubAdapter.Instance as IHub;
                });

            return services;
        }
    }
}
