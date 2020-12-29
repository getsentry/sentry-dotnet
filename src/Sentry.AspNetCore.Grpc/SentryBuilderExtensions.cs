using Microsoft.Extensions.DependencyInjection;
using Sentry.Extensibility;
using Sentry.Protocol;
using Sentry.Reflection;

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
                .AddSingleton<IProtobufRequestPayloadExtractor, DefaultProtobufRequestPayloadExtractor>()
                .AddSingleton<ISentryEventProcessor, SentryGrpcEventProcessor>();

            _ = builder.Services.AddGrpc(options =>
            {
                options.Interceptors.Add<SentryGrpcInterceptor>();
            });

            return builder;
        }


        private class SentryGrpcEventProcessor : ISentryEventProcessor
        {
            private static readonly SdkVersion NameAndVersion
                = typeof(SentryGrpcInterceptor).Assembly.GetNameAndVersion();

            private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;
            private const string SdkName = "sentry.dotnet.aspnetcore.grpc";

            public SentryEvent Process(SentryEvent @event)
            {
                // Take over the SDK name since this wraps ASP.NET Core
                @event.Sdk.Name = SdkName;
                @event.Sdk.Version = NameAndVersion.Version;

                if (NameAndVersion.Version != null)
                {
                    @event.Sdk.AddPackage(ProtocolPackageName, NameAndVersion.Version);
                }

                return @event;
            }
        }
    }
}
