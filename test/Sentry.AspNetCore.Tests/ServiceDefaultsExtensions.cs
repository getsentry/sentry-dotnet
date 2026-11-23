#nullable enable
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.Hosting;

internal static class ServiceDefaultsExtensions
{
    public static TBuilder UseSentryLikeServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        if (builder is WebApplicationBuilder web)
        {
            SentryWebApplicationBuilderExtensions.UseSentry(web);
        }
        else
        {
            builder.UseSentry();
        }

        return builder;
    }
}
