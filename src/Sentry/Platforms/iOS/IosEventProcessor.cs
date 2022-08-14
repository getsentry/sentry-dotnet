using Sentry.Extensibility;

namespace Sentry.iOS;

internal class IosEventProcessor : ISentryEventProcessor, IDisposable
{
    private readonly SentryCocoaOptions _options;

    public IosEventProcessor(SentryCocoaOptions options)
    {
        _options = options;
    }

    public SentryEvent Process(SentryEvent @event)
    {
        // TODO: Apply device/os context info

        return @event;
    }

    public void Dispose()
    {
    }
}
