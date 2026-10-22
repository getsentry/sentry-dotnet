using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Hosting;

/// <summary>
/// Starts the SDK while the host is built. The host's constructor reads <c>IOptions&lt;HostOptions&gt;</c>, which runs
/// this, so the SDK starts even when the app removes or replaces Sentry's logger providers.
/// </summary>
internal sealed class SentrySdkInitializer(IServiceProvider services) : IConfigureOptions<HostOptions>
{
    public void Configure(HostOptions options) => _ = services.GetRequiredService<Func<IHub>>();
}
