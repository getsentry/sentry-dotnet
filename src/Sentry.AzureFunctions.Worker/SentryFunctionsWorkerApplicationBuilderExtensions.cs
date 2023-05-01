using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sentry.AzureFunctions.Worker;

public static class SentryFunctionsWorkerApplicationBuilderExtensions
{
    public static IFunctionsWorkerApplicationBuilder UseSentry(this IFunctionsWorkerApplicationBuilder builder, Action<SentryOptions>? optionsConfiguration)
    {
        builder.UseMiddleware<SentryFunctionsWorkerMiddleware>();

        builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSentry(optionsConfiguration));

        return builder;
    }
}
