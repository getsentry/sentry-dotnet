using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Sentry.Extensibility;

namespace Sentry.Extensions.Logging.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Sentry's services to the <see cref="IServiceCollection"/>, initializing Sentry with
    /// <typeparamref name="TOptions"/> when the hub is first resolved.
    /// </summary>
    /// <param name="services">The services.</param>
    public static IServiceCollection AddSentry<TOptions>(this IServiceCollection services)
        where TOptions : SentryHostOptions, new()
        => services.AddSentry<TOptions>(initializeSdk: true);

    internal static IServiceCollection AddSentry<TOptions>(this IServiceCollection services, bool initializeSdk)
        where TOptions : SentryHostOptions, new()
    {
        // Replaces rather than TryAdd, so the integration registered last owns the options and the hub accessor:
        // a generic host UseSentry can run before the ASP.NET Core or MAUI one.
        services.RemoveAll<SentryOptions>();
        services.AddSingleton<SentryOptions>(c => c.GetRequiredService<IOptions<TOptions>>().Value);

        services.RemoveAll<Func<IHub>>();
        if (initializeSdk)
        {
            services.AddSingleton<Func<IHub>>(c =>
            {
                var options = c.GetRequiredService<IOptions<TOptions>>().Value;
                var hub = SentrySdk.InitHub(options);
                SentrySdk.UseHub(hub);
                options.ApplyConfigureScopeCallbacks(hub);

                return () => HubAdapter.Instance;
            });
        }

        return services.AddSentryHub();
    }

    private static IServiceCollection AddSentryHub(this IServiceCollection services)
    {
        // Must not capture a hub instance: https://github.com/getsentry/sentry-dotnet/issues/103
        services.TryAddTransient<ISentryClient>(c => c.GetRequiredService<IHub>());
        services.TryAddTransient(c => c.GetRequiredService<Func<IHub>>()());
        services.TryAddSingleton<Func<IHub>>(_ => () => HubAdapter.Instance);

        // Custom handler for HttpClientFactory.
        // Must be singleton: https://github.com/getsentry/sentry-dotnet/issues/785
        // Must use AddSingleton: https://github.com/getsentry/sentry-dotnet/issues/2373
        services.AddSingleton<IHttpMessageHandlerBuilderFilter, SentryHttpMessageHandlerBuilderFilter>();

        return services;
    }
}
