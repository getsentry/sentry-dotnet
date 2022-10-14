using Sentry.Android.Extensions;

namespace Sentry.Android.Callbacks;

internal class TracesSamplerCallback : JavaObject, JavaSdk.SentryOptions.ITracesSamplerCallback
{
    private readonly Func<TransactionSamplingContext, double?> _tracesSampler;

    public TracesSamplerCallback(Func<TransactionSamplingContext, double?> tracesSampler)
    {
        _tracesSampler = tracesSampler;
    }

    public JavaDouble? Sample(JavaSdk.SamplingContext c)
    {
        var context = c.ToTransactionSamplingContext();
        return (JavaDouble?)_tracesSampler.Invoke(context);
    }
}
