using Microsoft.Extensions.DependencyInjection;
using Sentry.Extensibility;
using Sentry.Reflection;

namespace Sentry.AspNetCore.Grpc;

/// <summary>
/// Extension methods for <see cref="ISentryBuilder"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryBuilderExtensions
{
    private static readonly SdkVersion NameAndVersion = typeof(SentryGrpcInterceptor).Assembly.GetNameAndVersion();

    /// <summary>
    /// Adds gRPC integration to Sentry
    /// </summary>
    /// <param name="builder"></param>
    public static ISentryBuilder AddGrpc(this ISentryBuilder builder)
    {
        _ = builder.Services
            .AddSingleton<IProtobufRequestPayloadExtractor, DefaultProtobufRequestPayloadExtractor>()
            .Configure<SentryAspNetCoreOptions>(options => options.SetSdk(Constants.SdkName, NameAndVersion));

        _ = builder.Services.AddGrpc(options =>
        {
            options.Interceptors.Add<SentryGrpcInterceptor>();
        });

        return builder;
    }
}
