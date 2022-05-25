using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Maui.Hosting;

namespace Sentry.Maui;

internal class SentryMauiInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

#if ANDROID
        var context = global::Android.App.Application.Context;
        SentrySdk.Init(context, options);
#else
        SentrySdk.Init(options);
#endif
    }
}
