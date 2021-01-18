using System.Collections.Generic;

namespace Sentry.Protocol
{
    /// <summary>
    /// Implemented by objects that contain a map of tags.
    /// </summary>
    public interface IHasTags
    {
        /// <summary>
        /// Arbitrary key-value for this event.
        /// </summary>
        IReadOnlyDictionary<string, string> Tags { get; }
    }
}
