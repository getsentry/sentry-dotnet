using Microsoft.Extensions.DependencyInjection;

namespace Sentry.AspNetCore.Grpc
{
    /// <summary>
    /// Extension methods for <see cref="ISentryBuilder"/>
    /// </summary>
    public static class SentryBuilderExtensions
    {
        /// <summary>
        /// Adds gRPC integration to Sentry
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ISentryBuilder AddGrpc(this ISentryBuilder builder)
        {
            _ = builder.Services
                .AddSingleton<IProtobufRequestPayloadExtractor, DefaultProtobufRequestPayloadExtractor>();

            _ = builder.Services.AddGrpc(options =>
            {
                options.Interceptors.Add<SentryGrpcInterceptor>();
            });

            return builder;
        }
    }
}
