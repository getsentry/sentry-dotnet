using Microsoft.Extensions.DependencyInjection;

namespace Sentry.Extensions.Logging.Internal;

internal static class SentryBuilderExtensions
{
    public static ISentryBuilder AddSentryOptions<TOptions>(this ISentryBuilder builder,
        Action<TOptions>? configureOptions)
        where TOptions: SentryOptions
    {
        if (configureOptions != null)
        {
            builder.Services.Configure(configureOptions);
        }

        return builder;
    }
}
