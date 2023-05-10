using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sentry.AzureFunctions.Worker;

/// <summary>
/// Sentry extension methods for Azure Functions with Isolated Worker SDK
/// </summary>
public static class SentryFunctionsWorkerApplicationBuilderExtensions
{
    /// <summary>
    /// Configure Azure Functions to use Sentry
    /// </summary>
    public static IFunctionsWorkerApplicationBuilder UseSentry(this IFunctionsWorkerApplicationBuilder builder, Action<SentryLoggingOptions>? optionsConfiguration)
    {
        builder.UseMiddleware<SentryFunctionsWorkerMiddleware>();

        builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSentry(optionsConfiguration));

        return builder;
    }
}
