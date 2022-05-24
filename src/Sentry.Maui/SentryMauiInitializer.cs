using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Maui.Hosting;

namespace Sentry.Maui;

internal class SentryMauiInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;
        SentrySdk.Init(options);
    }
}
