using System;
using Sentry.Extensibility;

namespace Sentry.Internal;

internal class DelegateTransactionProcessor : ISentryTransactionProcessor
{
    private readonly Func<Transaction, Transaction?> _func;

    public DelegateTransactionProcessor(Func<Transaction, Transaction?> func)
    {
        _func = func;
    }

    public Transaction? Process(Transaction transaction)
    {
        return _func(transaction);
    }
}