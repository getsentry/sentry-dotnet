using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sentry.AspNetCore;
using Sentry.AspNetCore.Grpc;
using Sentry.Extensibility;
using Sentry.Extensions.Logging.Extensions.DependencyInjection;
using Sentry.Extensions.Protobuf;

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
        public static IServiceCollection AddSentryGrpc(this IServiceCollection services)
        {
            _ = services.AddSingleton<ISentryEventProcessor, AspNetCoreEventProcessor>();
            services.TryAddSingleton<IUserFactory, DefaultUserFactory>();

            _ = services.AddSingleton<IProtobufRequestPayloadExtractor, DefaultProtobufRequestPayloadExtractor>();

            _ = services.AddSentry<SentryAspNetCoreGrpcOptions>();

            services.AddGrpc(options =>
            {
                options.Interceptors.Add<SentryGrpcInterceptor>();
            });

            return services;
        }
    }
}
