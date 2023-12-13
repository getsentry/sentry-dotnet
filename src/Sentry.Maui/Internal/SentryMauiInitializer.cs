using Microsoft.Extensions.Options;

namespace Sentry.Maui.Internal;

internal class SentryMauiInitializer : IMauiInitializeService
{
    // This method is invoked automatically within MauiAppBuilder.Build, just after it registers all the services.
    // That makes it an ideal place to initialize the Sentry SDK.
    public void Initialize(IServiceProvider services)
    {
        // Get dependencies from the service provider.
        var options = services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;
        var disposer = services.GetRequiredService<Disposer>();

        // Initialize the Sentry SDK.
        var disposable = SentrySdk.Init(options);

        // Register the return value from initializing the SDK with the disposer.
        // This will ensure that it gets disposed when the service provider is disposed.
        disposer.Register(disposable);
    }
}
