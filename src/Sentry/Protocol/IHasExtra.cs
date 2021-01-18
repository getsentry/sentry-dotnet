using System.Collections.Generic;

namespace Sentry.Protocol
{
    /// <summary>
    /// Implemented by objects that contain a map of untyped extra data.
    /// </summary>
    public interface IHasExtra
    {
        /// <summary>
        /// An arbitrary mapping of additional metadata to store with the event.
        /// </summary>
        IReadOnlyDictionary<string, object?> Extra { get; }
    }
}
