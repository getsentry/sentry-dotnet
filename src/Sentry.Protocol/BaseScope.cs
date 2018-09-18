using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace Sentry.Protocol
{
    /// <summary>
    /// The Scoped part of the protocol
    /// </summary>
    /// <remarks>
    /// Members are included in the event but often modified as part
    /// of a scope manipulation which could affect multiple outgoing events.
    /// </remarks>
    [DataContract]
    [DebuggerDisplay("Breadcrumbs: {InternalBreadcrumbs?.Count ?? 0}")]
    public class BaseScope
    {
        // Default values are null so no serialization of empty objects or arrays
        [DataMember(Name = "user", EmitDefaultValue = false)]
        internal User InternalUser { get; private set; }

        [DataMember(Name = "contexts", EmitDefaultValue = false)]
        internal Contexts InternalContexts { get; private set; }

        [DataMember(Name = "request", EmitDefaultValue = false)]
        internal Request InternalRequest { get; private set; }

        [DataMember(Name = "fingerprint", EmitDefaultValue = false)]
        internal IEnumerable<string> InternalFingerprint { get; set; }

        [DataMember(Name = "breadcrumbs", EmitDefaultValue = false)]
        internal ConcurrentQueue<Breadcrumb> InternalBreadcrumbs { get; set; }

        [DataMember(Name = "extra", EmitDefaultValue = false)]
        internal ConcurrentDictionary<string, object> InternalExtra { get; set; }

        [DataMember(Name = "tags", EmitDefaultValue = false)]
        internal ConcurrentDictionary<string, string> InternalTags { get; set; }

        /// <summary>
        /// An optional scope option
        /// </summary>
        /// <remarks>
        /// Options are not mandatory. it allows defining callback for deciding
        /// on adding breadcrumbs and the max breadcrumbs allowed
        /// </remarks>
        /// <returns>
        /// The options or null, if no options were defined.
        /// </returns>
        public IScopeOptions ScopeOptions { get; }

        /// <summary>
        /// Sentry level
        /// </summary>
        [DataMember(Name = "level", EmitDefaultValue = false)]
        public SentryLevel? Level { get; set; }

        /// <summary>
        /// The name of the transaction in which there was an event.
        /// </summary>
        /// <remarks>
        /// A transaction should only be defined when it can be well defined
        /// On a Web framework, for example, a transaction is the route template
        /// rather than the actual request path. That is so GET /user/10 and /user/20
        /// (which have route template /user/{id}) are identified as the same transaction.
        /// </remarks>
        [DataMember(Name = "transaction", EmitDefaultValue = false)]
        public string Transaction { get; set; }

        /// <summary>
        /// Gets or sets the HTTP.
        /// </summary>
        /// <value>
        /// The HTTP.
        /// </value>
        public Request Request
        {
            get => InternalRequest ?? (InternalRequest = new Request());
            set => InternalRequest = value;
        }

        /// <summary>
        /// Gets the structured Sentry context
        /// </summary>
        /// <value>
        /// The contexts.
        /// </value>
        public Contexts Contexts
        {
            get => InternalContexts ?? (InternalContexts = new Contexts());
            set => InternalContexts = value;
        }

        /// <summary>
        /// Gets the user information
        /// </summary>
        /// <value>
        /// The user.
        /// </value>
        public User User
        {
            get => InternalUser ?? (InternalUser = new User());
            set => InternalUser = value;
        }

        /// <summary>
        /// The environment name, such as 'production' or 'staging'.
        /// </summary>
        /// <remarks>Requires Sentry 8.0 or higher</remarks>
        [DataMember(Name = "environment", EmitDefaultValue = false)]
        public string Environment { get; set; }

        /// <summary>
        /// SDK information
        /// </summary>
        /// <remarks>New in Sentry version: 8.4</remarks>
        [DataMember(Name = "sdk", EmitDefaultValue = false)]
        public SdkVersion Sdk { get; internal set; } = new SdkVersion();

        /// <summary>
        /// A list of strings used to dictate the deduplication of this event.
        /// </summary>
        /// <seealso href="https://docs.sentry.io/learn/rollups/#custom-grouping"/>
        /// <remarks>
        /// A value of {{ default }} will be replaced with the built-in behavior, thus allowing you to extend it, or completely replace it.
        /// New in version Protocol: version '7'
        /// </remarks>
        /// <example> { "fingerprint": ["myrpc", "POST", "/foo.bar"] } </example>
        /// <example> { "fingerprint": ["{{ default }}", "http://example.com/my.url"] } </example>
        public IEnumerable<string> Fingerprint => InternalFingerprint ?? Enumerable.Empty<string>();

        /// <summary>
        /// A trail of events which happened prior to an issue.
        /// </summary>
        /// <seealso href="https://docs.sentry.io/learn/breadcrumbs/"/>
        public IEnumerable<Breadcrumb> Breadcrumbs => InternalBreadcrumbs ?? (InternalBreadcrumbs = new ConcurrentQueue<Breadcrumb>());

        /// <summary>
        /// An arbitrary mapping of additional metadata to store with the event.
        /// </summary>
        public
#if NET45
            IDictionary<string, object>
#else
            IReadOnlyDictionary<string, object>
#endif
            Extra => InternalExtra ?? (InternalExtra = new ConcurrentDictionary<string, object>());

        /// <summary>
        /// Arbitrary key-value for this event
        /// </summary>
        public
#if NET45
            IDictionary<string, string>
#else
            IReadOnlyDictionary<string, string>
#endif
            Tags => InternalTags ?? (InternalTags = new ConcurrentDictionary<string, string>());

        /// <summary>
        /// Creates a scope with the specified options
        /// </summary>
        public BaseScope(IScopeOptions options) => ScopeOptions = options;
    }
}
