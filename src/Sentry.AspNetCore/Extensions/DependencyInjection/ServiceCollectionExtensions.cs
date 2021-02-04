using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sentry.AspNetCore;
using Sentry.Extensibility;
using Sentry.Extensions.Logging.Extensions.DependencyInjection;

#if !NETSTANDARD
using Microsoft.Extensions.Http;
#endif

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
        public static ISentryBuilder AddSentry(this IServiceCollection services)
        {
            services.AddSingleton<ISentryEventProcessor, AspNetCoreEventProcessor>();
            services.TryAddSingleton<IUserFactory, DefaultUserFactory>();

            services
                    .AddSingleton<IRequestPayloadExtractor, FormRequestPayloadExtractor>()
                    // Last
                    .AddSingleton<IRequestPayloadExtractor, DefaultRequestPayloadExtractor>();

            services.AddSentry<SentryAspNetCoreOptions>();

#if !NETSTANDARD2_0
            // Custom handler for HttpClientFactory.
            // Must be singleton: https://github.com/getsentry/sentry-dotnet/issues/785
            services.AddSingleton<IHttpMessageHandlerBuilderFilter, SentryHttpMessageHandlerBuilderFilter>();
#endif

            return new SentryAspNetCoreBuilder(services);
        }
    }
}
