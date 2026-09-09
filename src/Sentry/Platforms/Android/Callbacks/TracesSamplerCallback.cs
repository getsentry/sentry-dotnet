using Sentry.Android.Extensions;
using Sentry.Extensibility;

namespace Sentry.Android.Callbacks;

internal class TracesSamplerCallback : JavaObject, JavaSdk.SentryOptions.ITracesSamplerCallback
{
    private readonly Func<TransactionSamplingContext, double?> _tracesSampler;
    private readonly SentryOptions _options;

    public TracesSamplerCallback(
        Func<TransactionSamplingContext, double?> tracesSampler,
        SentryOptions options)
    {
        _tracesSampler = tracesSampler;
        _options = options;
    }

    public JavaDouble? Sample(JavaSdk.SamplingContext c)
    {
        try
        {
            var context = c.ToTransactionSamplingContext();
            return (JavaDouble?)_tracesSampler.Invoke(context);
        }
        catch (Exception exception)
        {
            _options.LogError(exception, "TracesSampler callback failed.");
            return null;
        }
    }
}
