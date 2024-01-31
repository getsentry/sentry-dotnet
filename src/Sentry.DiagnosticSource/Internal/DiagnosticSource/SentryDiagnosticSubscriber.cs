using Sentry.Extensibility;

namespace Sentry.Internal.DiagnosticSource;

/// <summary>
/// Class that subscribes to specific listeners from DiagnosticListener.
/// </summary>
internal class SentryDiagnosticSubscriber : IObserver<DiagnosticListener>
{
    // We intentionally do not dispose subscriptions, so that we can get as much information as possible.
    // Also, the <see cref="OnNext"/> method may fire more than once with the same listener name.
    // We need to subscribe each time it does. However, we only need one instance of each of our listeners.
    // Thus, we will create the instances lazily.

    private readonly Lazy<SentryEFCoreListener> _efListener;
    private readonly Lazy<SentrySqlListener> _sqlListener;

    public SentryDiagnosticSubscriber(IHub hub, SentryOptions options)
    {
        _efListener = new Lazy<SentryEFCoreListener>(() =>
        {
            options.Log(SentryLevel.Debug, "Registering EF Core integration");
            return new SentryEFCoreListener(hub, options);
        });

        _sqlListener = new Lazy<SentrySqlListener>(() =>
        {
            options.Log(SentryLevel.Debug, "Registering SQL Client integration.");
            return new SentrySqlListener(hub, options);
        });
    }

    public void OnCompleted() { }

    public void OnError(Exception error) { }

    public void OnNext(DiagnosticListener listener)
    {
        switch (listener.Name)
        {
            case "Microsoft.EntityFrameworkCore":
                {
                    listener.Subscribe(_efListener.Value);
                    break;
                }

            case "SqlClientDiagnosticListener":
                {
                    listener.Subscribe(_sqlListener.Value);
                    break;
                }
        }

        // By default, the EF listener will duplicate spans already given by the SQL Client listener.
        // Thus, we should disable those parts of the EF listener when they are both registered.
        if (_efListener.IsValueCreated && _sqlListener.IsValueCreated)
        {
            var efListener = _efListener.Value;
            efListener.DisableConnectionSpan();
            efListener.DisableQuerySpan();
        }
    }
}
