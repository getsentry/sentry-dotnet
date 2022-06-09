using Sentry.Java;

namespace Sentry.Android
{
    internal class TracesSamplerCallback : JavaObject, Java.SentryOptions.ITracesSamplerCallback
    {
        private readonly Func<TransactionSamplingContext, double?> _tracesSampler;

        public TracesSamplerCallback(Func<TransactionSamplingContext, double?> tracesSampler)
        {
            _tracesSampler = tracesSampler;
        }

        public JavaDouble? Sample(SamplingContext c)
        {
            var context = c.ToTransactionSamplingContext();
            var result = _tracesSampler.Invoke(context);
            return (JavaDouble?)result;
        }
    }
}
