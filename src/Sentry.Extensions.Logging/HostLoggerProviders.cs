using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sentry.Extensions.Logging;

/// <summary>
/// The logger providers that a host integration adds, such as UseSentry from Sentry.Extensions.Hosting or
/// Sentry.AspNetCore. While they're registered, Logging.AddSentry() disables its own providers, so each log entry is
/// sent once. When the app removed them, for example with ClearProviders(), Logging.AddSentry() adds them back.
/// </summary>
internal sealed class HostLoggerProviders
{
    private readonly ServiceDescriptor _events;
    private readonly ServiceDescriptor _structuredLogs;

    private HostLoggerProviders(ServiceDescriptor events, ServiceDescriptor structuredLogs)
    {
        _events = events;
        _structuredLogs = structuredLogs;
    }

    public static void Add(
        IServiceCollection services,
        Func<IServiceProvider, ILoggerProvider> events,
        Func<IServiceProvider, ILoggerProvider> structuredLogs)
    {
        var providers = new HostLoggerProviders(
            ServiceDescriptor.Singleton(events),
            ServiceDescriptor.Singleton(structuredLogs));
        services.Add(providers._events);
        services.Add(providers._structuredLogs);
        services.AddSingleton(providers);
    }

    public static bool WereAdded(IServiceProvider provider) => provider.GetService<HostLoggerProviders>() is not null;

    public static bool AreRegistered(IServiceProvider provider, IServiceCollection services)
        => provider.GetServices<HostLoggerProviders>()
            .Any(providers => services.Contains(providers._events) || services.Contains(providers._structuredLogs));

    public static ILoggerProvider CreateEvents(IServiceProvider provider)
        => Create(provider, providers => providers._events);

    public static ILoggerProvider CreateStructuredLogs(IServiceProvider provider)
        => Create(provider, providers => providers._structuredLogs);

    // The generic host's providers stand down when ASP.NET Core or MAUI adds its own
    private static ILoggerProvider Create(
        IServiceProvider provider,
        Func<HostLoggerProviders, ServiceDescriptor> select)
        => provider.GetServices<HostLoggerProviders>()
            .Select(providers => (ILoggerProvider)select(providers).ImplementationFactory!(provider))
            .FirstOrDefault(created => created is not NullLoggerProvider) ?? NullLoggerProvider.Instance;
}
