namespace Microsoft.Extensions.Hosting;

internal static class ServiceDefaultsExtensions
{
    public static TBuilder UseSentryLikeServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        if (builder is MauiAppBuilder maui)
        {
            SentryMauiAppBuilderExtensions.UseSentry(maui);
        }
        else
        {
            builder.UseSentry();
        }

        return builder;
    }
}
