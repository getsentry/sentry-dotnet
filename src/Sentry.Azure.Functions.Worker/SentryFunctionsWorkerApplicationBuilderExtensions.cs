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
        => UseSentry(builder, context, (Action<SentryAzure.FunctionsOptions>?)null);

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
        Action<SentryAzure.FunctionsOptions>? optionsConfiguration)
    {
        builder.UseMiddleware<SentryFunctionsWorkerMiddleware>();

        var services = builder.Services;
        services.Configure<SentryAzure.FunctionsOptions>(options =>
            context.Configuration.GetSection("Sentry").Bind(options));

        if (optionsConfiguration != null)
        {
            services.Configure(optionsConfiguration);
        }

        services.AddLogging();
        services.AddSingleton<ILoggerProvider, SentryAzure.FunctionsLoggerProvider>();
        services.AddSingleton<IConfigureOptions<SentryAzure.FunctionsOptions>, SentryAzure.FunctionsOptionsSetup>();

        services.AddSentry<SentryAzure.FunctionsOptions>();

        return builder;
    }
}
