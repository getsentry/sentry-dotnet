using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Sentry.AspNetCore;

/// <summary>
/// Delegates/wraps the inner builder, so that we can intercept calls to <see cref="Use"/> and add our middleware
/// </summary>
internal class SentryTracingBuilder : IApplicationBuilder
{
    private const string EndpointRouteBuilder = "__EndpointRouteBuilder";
    public SentryTracingBuilder(IApplicationBuilder inner)
    {
        InnerBuilder = inner;
    }

    private IApplicationBuilder InnerBuilder { get; }

    /// <inheritdoc />
    public IServiceProvider ApplicationServices
    {
        get => InnerBuilder.ApplicationServices;
        set => InnerBuilder.ApplicationServices = value;
    }

    /// <inheritdoc />
    public IDictionary<string, object?> Properties => InnerBuilder.Properties;

    /// <inheritdoc />
    public IFeatureCollection ServerFeatures => InnerBuilder.ServerFeatures;

    /// <inheritdoc />
    public RequestDelegate Build() => InnerBuilder.Build();

    /// <inheritdoc />
    public IApplicationBuilder New() => new SentryTracingBuilder(InnerBuilder.New());

    /// <inheritdoc />
    public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
    {
        var builder = InnerBuilder.Use(middleware);
        return (Properties.ContainsKey(EndpointRouteBuilder) && !this.IsSentryTracingRegistered())
            // UseRouting sets a property on the builder that we use to detect when UseRouting has been added and we
            // register SentryTracing immediately afterwards
            // https://github.com/dotnet/aspnetcore/blob/8eaf4b51f73ae2b0ed079e4f8253afb53e96b703/src/Http/Routing/src/Builder/EndpointRoutingApplicationBuilderExtensions.cs#L58-L62
            ? builder.UseSentryTracing()
            : builder;
    }
}
