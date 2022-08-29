using Microsoft.Extensions.Options;

namespace Sentry.Maui.Internal;

internal class SentryMauiInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;
        var disposer = services.GetRequiredService<Disposer>();

        var disposable = SentrySdk.Init(options);

        // Register the return value from initializing the SDK with the disposer.
        // This will ensure that it gets disposed when the service provider is disposed.
        // TODO: re-evaluate this with respect to MAUI app lifecycle events
        disposer.Register(disposable);

        // Bind MAUI events
        var binder = services.GetRequiredService<MauiEventsBinder>();
        binder.BindMauiEvents();
    }
}
