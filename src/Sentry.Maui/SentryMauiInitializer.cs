using Microsoft.Maui.Hosting;

namespace Sentry.Maui;

public class SentryMauiInitializer : IMauiInitializeService
{
    private readonly Func<IHub> _hubFactory;

    public SentryMauiInitializer(Func<IHub> hubFactory)
    {
        _hubFactory = hubFactory;
    }

    public void Initialize(IServiceProvider services)
    {
        // This will ensure we initialize the SDK.
        // TODO: there's probably a better approach
        _ = _hubFactory.Invoke();
    }
}
