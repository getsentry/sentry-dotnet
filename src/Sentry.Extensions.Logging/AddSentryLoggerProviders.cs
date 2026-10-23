using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging;

/// <summary>
/// The logger providers that Logging.AddSentry() adds. While they're registered, calling Logging.AddSentry() again
/// doesn't add them a second time, so each log entry is sent once. When the app removed them, for example with
/// ClearProviders(), the next call adds them again.
/// </summary>
internal sealed class AddSentryLoggerProviders
{
    private readonly ServiceDescriptor _events;
    private readonly ServiceDescriptor _structuredLogs;

    private AddSentryLoggerProviders(ServiceDescriptor events, ServiceDescriptor structuredLogs)
    {
        _events = events;
        _structuredLogs = structuredLogs;
    }

    public static void TryAdd(
        IServiceCollection services,
        Func<IServiceProvider, ILoggerProvider> events,
        Func<IServiceProvider, ILoggerProvider> structuredLogs)
    {
        if (AreRegistered(services))
        {
            return;
        }

        var providers = new AddSentryLoggerProviders(
            ServiceDescriptor.Singleton(events),
            ServiceDescriptor.Singleton(structuredLogs));
        services.Add(providers._events);
        services.Add(providers._structuredLogs);
        services.AddSingleton(providers);
    }

    private static bool AreRegistered(IServiceCollection services)
        => services
            .Where(descriptor => descriptor.ServiceType == typeof(AddSentryLoggerProviders))
            .Select(descriptor => (AddSentryLoggerProviders)descriptor.ImplementationInstance!)
            .Any(providers => services.Contains(providers._events) || services.Contains(providers._structuredLogs));
}
