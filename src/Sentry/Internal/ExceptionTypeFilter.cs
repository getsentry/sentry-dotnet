using System;
using Sentry.Extensibility;

namespace Sentry.Internal
{
    internal class ExceptionTypeFilter<TException> : IExceptionFilter where TException : Exception
    {
        private readonly Type _filteredType = typeof(TException);
        public bool Filter(Exception ex) => _filteredType.IsInstanceOfType(ex);
    }
}
