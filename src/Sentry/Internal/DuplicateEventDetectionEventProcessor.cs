using System;
using System.Runtime.CompilerServices;
using Sentry.Extensibility;

namespace Sentry
{
    /// <summary>
    /// Possible modes of dropping events that are detected to be duplicates.
    /// </summary>
    [Flags]
    public enum DeduplicateMode
    {
        /// <summary>
        /// Same event instance. Assumes no object reuse/pooling.
        /// </summary>
        SameEvent = 1,
        /// <summary>
        /// An exception that was captured twice.
        /// </summary>
        SameExceptionInstance = 2,
        /// <summary>
        /// An exception already captured exists as an inner exception.
        /// </summary>
        InnerException = 4,
        /// <summary>
        /// An exception already captured is part of the aggregate exception.
        /// </summary>
        AggregateException = 8,
        /// <summary>
        /// All modes combined.
        /// </summary>
        All = int.MaxValue
    }
}

namespace Sentry.Internal
{
    internal class DuplicateEventDetectionEventProcessor : ISentryEventProcessor
    {
        private readonly SentryOptions _options;
        private readonly ConditionalWeakTable<object, object> _capturedObjects = new ConditionalWeakTable<object, object>();

        public DuplicateEventDetectionEventProcessor(SentryOptions options) => _options = options;

        public SentryEvent Process(SentryEvent @event)
        {
            if (_options.DeduplicateMode.HasFlag(DeduplicateMode.SameEvent))
            {
                if (_capturedObjects.TryGetValue(@event, out _))
                {
                    _options.DiagnosticLogger?.LogDebug("Same event instance detected and discarded. EventId: {0}", @event.EventId);
                    return null;
                }
                _capturedObjects.Add(@event, null);
            }

            if (@event.Exception == null
                || !IsDuplicate(@event.Exception))
            {
                return @event;
            }

            _options.DiagnosticLogger?.LogDebug("Duplicate Exception detected. Event {0} will be discarded.", @event.EventId);
            return null;
        }

        private bool IsDuplicate(Exception ex)
        {
            if (ex == null)
            {
                return false;
            }

            if (_options.DeduplicateMode.HasFlag(DeduplicateMode.SameExceptionInstance))
            {
                if (_capturedObjects.TryGetValue(ex, out _))
                {
                    return true;
                }

                _capturedObjects.Add(ex, null);
            }

            if (_options.DeduplicateMode.HasFlag(DeduplicateMode.AggregateException)
                && ex is AggregateException aex)
            {
                foreach (var aexInnerException in aex.InnerExceptions)
                {
                    if (IsDuplicate(aexInnerException))
                    {
                        return true;
                    }
                }
            }
            else if (_options.DeduplicateMode.HasFlag(DeduplicateMode.InnerException)
                     && ex.InnerException != null)
            {
                if (IsDuplicate(ex.InnerException))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
