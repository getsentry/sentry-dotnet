using System.ComponentModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Extensibility;
using Sentry.Internal;

// ReSharper disable once CheckNamespace -- Discoverability
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Sentry's services to the <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns></returns>
        public static IServiceCollection AddSentry(this IServiceCollection services)
        {
            services.AddSingleton<ISentryEventProcessor, AspNetCoreEventProcessor>();
            services.TryAddSingleton<IUserFactory, DefaultUserFactory>();

            services
                .AddSingleton<IRequestPayloadExtractor, FormRequestPayloadExtractor>()
                // Last
                .AddSingleton<IRequestPayloadExtractor, DefaultRequestPayloadExtractor>();

            services.TryAddSingleton<SentryOptions>(
                c => c.GetRequiredService<IOptions<SentryAspNetCoreOptions>>().Value);

            services.TryAddSingleton<HubWrapper>();

            services.TryAddSingleton<IHub>(c =>
            {
                var options = c.GetRequiredService<IOptions<SentryAspNetCoreOptions>>().Value;

                if (options.InitializeSdk)
                {
                    var hub = c.GetRequiredService<HubWrapper>();
                    var disposable = SentrySdk.UseHub(hub);
                    var lifetime = c.GetService<IApplicationLifetime>();
                    lifetime?.ApplicationStopped.Register(() => disposable.Dispose());
                    return hub;
                }

                // Access to whatever the static Hub points to (disabled or initialized via SentrySdk.Init)
                return HubAdapter.Instance;
            });

            services.TryAddSingleton<ISentryClient>(c => c.GetService<IHub>());

            return services;
        }
    }
}
