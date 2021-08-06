using System.Collections.Generic;

namespace Sentry
{
    /// <summary>
    /// Observer for the sync. of Scopes across SDKs.
    /// </summary>
    public interface IScopeObserver
    {
        /// <summary>
        /// A trail of events which happened prior to an issue.
        /// </summary>
        /// <seealso href="https://docs.sentry.io/platforms/dotnet/enriching-events/breadcrumbs/"/>
        IReadOnlyCollection<Breadcrumb> Breadcrumbs { get; }

        /// <summary>
        /// Arbitrary key-value for this event.
        /// </summary>
        IReadOnlyDictionary<string, string> Tags { get; }

        /// <summary>
        /// An arbitrary mapping of additional metadata to store with the event.
        /// </summary>
        IReadOnlyDictionary<string, object?> Extra { get; }

        /// <summary>
        /// Adds a breadcrumb.
        /// </summary>
        void AddBreadcrumb(Breadcrumb breadcrumb);

        /// <summary>
        /// Sets an extra.
        /// </summary>
        void SetExtra(string key, object? value);

        /// <summary>
        /// Sets a tag.
        /// </summary>
        void SetTag(string key, string value);

        /// <summary>
        /// Removes a tag.
        /// </summary>
        void UnsetTag(string key);

        /// <summary>
        /// Gets the user information.
        /// </summary>
        /// <value>
        /// The user.
        /// </value>
        User User { get; set; }
    }
}
