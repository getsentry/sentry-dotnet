using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sentry.Extensibility;

namespace Sentry.Extensions.Logging;

/// <summary>
/// The logger providers that a host integration adds, such as UseSentry from Sentry.Extensions.Hosting or
/// Sentry.AspNetCore. While they're registered, Logging.AddSentry() disables its own providers, so each log entry is
/// sent once. When the app removed them, for example with ClearProviders(), Logging.AddSentry() adds them back, and
/// without it the SDK warns that log entries don't reach Sentry.
/// </summary>
internal sealed class HostLoggerProviders
{
    private readonly IServiceCollection _services;
    private readonly ServiceDescriptor _events;
    private readonly ServiceDescriptor _structuredLogs;

    private HostLoggerProviders(IServiceCollection services, ServiceDescriptor events, ServiceDescriptor structuredLogs)
    {
        _services = services;
        _events = events;
        _structuredLogs = structuredLogs;
    }

    public static void Add(
        IServiceCollection services,
        Func<IServiceProvider, ILoggerProvider> events,
        Func<IServiceProvider, ILoggerProvider> structuredLogs)
    {
        var providers = new HostLoggerProviders(
            services,
            ServiceDescriptor.Singleton(events),
            ServiceDescriptor.Singleton(structuredLogs));
        services.Add(providers._events);
        services.Add(providers._structuredLogs);
        services.AddSingleton(providers);
    }

    public static bool WereAdded(IServiceProvider provider) => provider.GetService<HostLoggerProviders>() is not null;

    public static bool AreRegistered(IServiceProvider provider, IServiceCollection services)
        => provider.GetServices<HostLoggerProviders>().Any(providers => providers.AreIn(services));

    public static void WarnIfRemoved(IServiceProvider provider)
    {
        if (WereAdded(provider)
            && !provider.GetServices<HostLoggerProviders>().Any(providers => providers.AreIn(providers._services))
            && !provider.GetServices<ILoggerProvider>().Any(created => created is SentryLoggerProvider))
        {
            provider.GetService<IHub>()?.GetSentryOptions()?.LogWarning(SentryLoggingOptions.HostLoggerProvidersRemoved);
        }
    }

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

    private bool AreIn(IServiceCollection services) => services.Contains(_events) || services.Contains(_structuredLogs);
}
