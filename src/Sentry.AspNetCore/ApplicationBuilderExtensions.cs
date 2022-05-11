using Microsoft.AspNetCore.Builder;
using Sentry.Extensions.Logging.Internal;

namespace Sentry.AspNetCore;

internal static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSentry(this IApplicationBuilder app)
    {
        var serviceProvider = app.ApplicationServices;
        serviceProvider.ConfigureSentryOptions<SentryAspNetCoreOptions>();
        serviceProvider.RegisterSentrySdkClose();

        return app.UseMiddleware<SentryMiddleware>();
    }
}
