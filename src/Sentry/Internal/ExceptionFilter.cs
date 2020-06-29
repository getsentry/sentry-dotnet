using System;

namespace Sentry.Internal
{
    /// <summary>
    /// A filter to be applied to an exception instance.
    /// </summary>
    public interface IExceptionFilter
    {
        /// <summary>
        /// Whether to filter out or not the exception.
        /// </summary>
        /// <param name="ex">The exception about to be captured.</param>
        /// <returns><c>true</c> if [the event should be filtered out]; otherwise, <c>false</c></returns>.
        bool Filter(Exception ex);
    }

    internal class ExceptionTypeFilter<TException> : IExceptionFilter where TException : Exception
    {
        private readonly Type _filteredType = typeof(TException);
        public bool Filter(Exception ex) => _filteredType.IsInstanceOfType(ex);
    }
}
