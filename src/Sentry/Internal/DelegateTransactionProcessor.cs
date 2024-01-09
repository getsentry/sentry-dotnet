using Sentry.Extensibility;

namespace Sentry.Internal;

internal class DelegateTransactionProcessor : ISentryTransactionProcessor
{
    private readonly Func<SentryTransaction, SentryTransaction?> _func;

    public DelegateTransactionProcessor(Func<SentryTransaction, SentryTransaction?> func)
    {
        _func = func;
    }

    public SentryTransaction? Process(SentryTransaction transaction)
    {
        return _func(transaction);
    }
}
