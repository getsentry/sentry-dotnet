using System.Collections.Generic;

namespace Sentry.Protocol
{
    public interface IEventLike
    {
        /// <summary>
        /// Sentry level.
        /// </summary>
        SentryLevel? Level { get; set; }

        /// <summary>
        /// Gets or sets the HTTP.
        /// </summary>
        /// <value>
        /// The HTTP.
        /// </value>
        Request Request { get; set; }

        /// <summary>
        /// Gets the structured Sentry context.
        /// </summary>
        /// <value>
        /// The contexts.
        /// </value>
        Contexts Contexts { get; set; }

        /// <summary>
        /// Gets the user information.
        /// </summary>
        /// <value>
        /// The user.
        /// </value>
        User User { get; set; }

        /// <summary>
        /// The environment name, such as 'production' or 'staging'.
        /// </summary>
        /// <remarks>Requires Sentry 8.0 or higher.</remarks>
        string? Environment { get; set; }

        /// <summary>
        /// The name of the transaction in which there was an event.
        /// </summary>
        /// <remarks>
        /// A transaction should only be defined when it can be well defined.
        /// On a Web framework, for example, a transaction is the route template
        /// rather than the actual request path. That is so GET /user/10 and /user/20
        /// (which have route template /user/{id}) are identified as the same transaction.
        /// </remarks>
        string? TransactionName { get; set; }

        /// <summary>
        /// SDK information.
        /// </summary>
        /// <remarks>New in Sentry version: 8.4</remarks>
        SdkVersion Sdk { get; }

        /// <summary>
        /// A list of strings used to dictate the deduplication of this event.
        /// </summary>
        /// <seealso href="https://docs.sentry.io/platforms/dotnet/data-management/event-grouping/grouping-enhancements/"/>
        /// <remarks>
        /// A value of {{ default }} will be replaced with the built-in behavior, thus allowing you to extend it, or completely replace it.
        /// New in version Protocol: version '7'
        /// </remarks>
        /// <example> { "fingerprint": ["myrpc", "POST", "/foo.bar"] } </example>
        /// <example> { "fingerprint": ["{{ default }}", "http://example.com/my.url"] } </example>
        IReadOnlyList<string> Fingerprint { get; set; }

        /// <summary>
        /// A trail of events which happened prior to an issue.
        /// </summary>
        /// <seealso href="https://docs.sentry.io/platforms/dotnet/enriching-events/breadcrumbs/"/>
        IReadOnlyCollection<Breadcrumb> Breadcrumbs { get; }

        /// <summary>
        /// An arbitrary mapping of additional metadata to store with the event.
        /// </summary>
        IReadOnlyDictionary<string, object?> Extra { get; }

        /// <summary>
        /// Arbitrary key-value for this event
        /// </summary>
        IReadOnlyDictionary<string, string> Tags { get; }

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
    }
}
