using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging.Extensions.DependencyInjection;

namespace Sentry.Azure.Functions.Worker;

/// <summary>
/// Sentry extension methods for Azure Functions with Isolated Worker SDK
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryFunctionsWorkerApplicationBuilderExtensions
{
    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    public static IFunctionsWorkerApplicationBuilder UseSentry(this IFunctionsWorkerApplicationBuilder builder, HostBuilderContext context)
        => UseSentry(builder, context, (Action<SentryAzureFunctionsOptions>?)null);

    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    public static IFunctionsWorkerApplicationBuilder UseSentry(this IFunctionsWorkerApplicationBuilder builder, HostBuilderContext context, string dsn)
        => builder.UseSentry(context, o => o.Dsn = dsn);

    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    public static IFunctionsWorkerApplicationBuilder UseSentry(
        this IFunctionsWorkerApplicationBuilder builder,
        HostBuilderContext context,
        Action<SentryAzureFunctionsOptions>? optionsConfiguration)
    {
        builder.UseMiddleware<SentryFunctionsWorkerMiddleware>();

        var services = builder.Services;
        var section = context.Configuration.GetSection("Sentry");
#if NET8_0_OR_GREATER
        services.AddSingleton<IConfigureOptions<SentryAzureFunctionsOptions>>(_ =>
            new SentryAzureFunctionsOptionsSetup(section)
        );
#else
        services.Configure<SentryAzureFunctionsOptions>(options =>
            section.Bind(options));
#endif

        if (optionsConfiguration != null)
        {
            services.Configure(optionsConfiguration);
        }

        services.AddLogging();
        services.AddSingleton<ILoggerProvider, SentryAzureFunctionsLoggerProvider>();
        services.AddSingleton<IConfigureOptions<SentryAzureFunctionsOptions>, SentryAzureFunctionsOptionsSetup>();

        services.AddSentry<SentryAzureFunctionsOptions>();

        return builder;
    }
}
