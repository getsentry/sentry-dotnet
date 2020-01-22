using System;
using System.Collections.Generic;
using System.Linq;

namespace Sentry.Extensibility
{
    /// <summary>
    /// Process filter an exception type and augments the event with its data
    /// </summary>
    /// <inheritdoc />
    public class SentryEventExceptionFilterProcessor : ISentryEventProcessor
    {
        private readonly HashSet<Type> ignoredTypes;

        /// <inheritdoc />
        public SentryEventExceptionFilterProcessor(params Type[] types)
            => ignoredTypes = new HashSet<Type>(types);

        /// <inheritdoc />
        public SentryEvent Process(SentryEvent @event)
        {
            var exception = @event.Exception;

            if (exception is AggregateException ae)
            {
                var hasIgnoredTypes = ae.InnerExceptions
                                        .Select(inner => inner.GetType())
                                        .Any(innerType => ignoredTypes.Contains(innerType));

                return hasIgnoredTypes ? null : @event;
            }
            else if (ignoredTypes.Contains(exception?.GetType()) || ignoredTypes.Contains(exception?.InnerException?.GetType()))
            {
                return null;
            }

            return @event;
        }
    }
}
