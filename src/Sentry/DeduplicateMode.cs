using System;

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
