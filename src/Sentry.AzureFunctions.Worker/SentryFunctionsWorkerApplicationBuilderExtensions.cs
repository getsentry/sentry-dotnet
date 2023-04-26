using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;

namespace Sentry.AzureFunctions.Worker;

public static class SentryFunctionsWorkerApplicationBuilderExtensions
{
    public static IFunctionsWorkerApplicationBuilder UseSentry(this IFunctionsWorkerApplicationBuilder builder, Action<SentryOptions>? optionsConfiguration)
    {
        builder.UseMiddleware<SentryFunctionsWorkerMiddleware>();

        // TODO: do we need to abstract/hide ILoggingBuilder.AddSentry(options => ...) and implement it here or not?

        return builder;
    }
}
