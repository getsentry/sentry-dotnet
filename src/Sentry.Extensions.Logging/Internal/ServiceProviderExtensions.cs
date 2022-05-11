using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensibility;
using Sentry.Infrastructure;

#if NETSTANDARD2_0
using IHostApplicationLifetime = Microsoft.Extensions.Hosting.IApplicationLifetime;
#else
using IHostApplicationLifetime = Microsoft.Extensions.Hosting.IHostApplicationLifetime;
#endif

namespace Sentry.Extensions.Logging.Internal;

internal static class ServiceProviderExtensions
{
    public static void RegisterSentrySdkClose(this IServiceProvider serviceProvider)
    {
        var lifetime = serviceProvider.GetService<IHostApplicationLifetime>();
        lifetime?.ApplicationStopped.Register(SentrySdk.Close);
    }

    public static void ConfigureSentryOptions<TOptions>(this IServiceProvider serviceProvider)
        where TOptions : SentryOptions, new()
    {
        var options = serviceProvider.GetService<IOptions<TOptions>>()?.Value;
        if (options == null)
        {
            return;
        }

        if (options.Debug && options.DiagnosticLogger is null or ConsoleDiagnosticLogger)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ISentryClient>>();
            options.DiagnosticLogger = new MelDiagnosticLogger(logger, options.DiagnosticLevel);
        }

        var stackTraceFactory = serviceProvider.GetService<ISentryStackTraceFactory>();
        if (stackTraceFactory != null)
        {
            options.UseStackTraceFactory(stackTraceFactory);
        }

        if (serviceProvider.GetService<IEnumerable<ISentryEventProcessor>>()?.Any() == true)
        {
            options.AddEventProcessorProvider(serviceProvider.GetServices<ISentryEventProcessor>);
        }

        if (serviceProvider.GetService<IEnumerable<ISentryEventExceptionProcessor>>()?.Any() == true)
        {
            options.AddExceptionProcessorProvider(serviceProvider.GetServices<ISentryEventExceptionProcessor>);
        }
    }
}
