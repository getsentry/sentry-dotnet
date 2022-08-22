﻿using System;
using Sentry.Extensibility;

namespace Sentry.Internal
{
    internal class DelegateTransactionProcessor : ISentryTransactionProcessor
    {
        private readonly Action<Transaction> _action;

        public DelegateTransactionProcessor(Action<Transaction> action)
        {
            _action = action;
        }

        public Transaction Process(Transaction transaction)
        {
            _action(transaction);
            return transaction;
        }
    }
}
