using Sentry.Extensibility;

namespace Sentry.iOS;

internal class IosEventProcessor : ISentryEventProcessor, IDisposable
{
    private readonly SentryCocoa.SentryOptions _options;

    public IosEventProcessor(SentryCocoa.SentryOptions options)
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
