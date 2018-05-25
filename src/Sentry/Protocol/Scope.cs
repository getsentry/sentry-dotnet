using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Sentry.Protocol
{
    /// <summary>
    /// Scope data to be sent with the event
    /// </summary>
    /// <remarks>
    /// Scope data is sent together with any event captured
    /// during the lifetime of the scope.
    /// </remarks>
    public class Scope
    {
        [DataMember(Name = "user", EmitDefaultValue = false)]
        internal User InternalUser { get; private set; }

        // TODO: Still has to support key-value
        [DataMember(Name = "contexts", EmitDefaultValue = false)]
        internal Contexts InternalContexts { get; private set; }

        [DataMember(Name = "fingerprint", EmitDefaultValue = false)]
        internal IImmutableList<string> InternalFingerprint { get; private set; }

        [DataMember(Name = "breadcrumbs", EmitDefaultValue = false)]
        internal IImmutableList<Breadcrumb> InternalBreadcrumbs { get; private set; }

        [DataMember(Name = "extra", EmitDefaultValue = false)]
        internal IImmutableDictionary<string, string> InternalExtra { get; private set; }

        [DataMember(Name = "tags", EmitDefaultValue = false)]
        internal IImmutableDictionary<string, string> InternalTags { get; private set; }

        /// <summary>
        /// Gets the structured Sentry context
        /// </summary>
        /// <value>
        /// The contexts.
        /// </value>
        public Contexts Contexts => InternalContexts ?? (InternalContexts = new Contexts());

        /// <summary>
        /// Gets the user information
        /// </summary>
        /// <value>
        /// The user.
        /// </value>
        public User User => InternalUser ?? (InternalUser = new User());

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
        public IImmutableList<string> Fingerprint
        {
            get => InternalFingerprint ?? (InternalFingerprint = ImmutableList<string>.Empty);
            internal set => InternalFingerprint = value;
        }

        /// <summary>
        /// A trail of events which happened prior to an issue.
        /// </summary>
        /// <seealso href="https://docs.sentry.io/learn/breadcrumbs/"/>
        public IImmutableList<Breadcrumb> Breadcrumbs
        {
            get => InternalBreadcrumbs ?? (InternalBreadcrumbs = ImmutableList<Breadcrumb>.Empty);
            internal set => InternalBreadcrumbs = value;
        }

        /// <summary>
        /// An arbitrary mapping of additional metadata to store with the event.
        /// </summary>
        public IImmutableDictionary<string, string> Extra
        {
            get => InternalExtra ?? (InternalExtra = ImmutableDictionary<string, string>.Empty);
            internal set => InternalExtra = value;
        }

        /// <summary>
        /// Arbitrary key-value for this event
        /// </summary>
        public IImmutableDictionary<string, string> Tags
        {
            get => InternalTags ?? (InternalTags = ImmutableDictionary<string, string>.Empty);
            internal set => InternalTags = value;
        }

        public void AddBreadcrumb(Breadcrumb breadcrumb) => Breadcrumbs = Breadcrumbs.Add(breadcrumb);
        public void AddFingerprint(string fingerprint) => Fingerprint = Fingerprint.Add(fingerprint);
        public void AddExtra(string key, string value) => Extra = Extra.Add(key, value);
        public void AddTag(string key, string value) => Tags = Tags.Add(key, value);

        internal Scope Clone()
        {
            // TODO: test with reflection to ensure Clone doesn't go out of sync with members
            return new Scope
            {
                InternalUser = InternalUser,
                InternalContexts = InternalContexts,
                InternalFingerprint = InternalFingerprint,
                InternalBreadcrumbs = InternalBreadcrumbs,
                InternalExtra = InternalExtra,
                InternalTags = InternalTags,
            };
        }
    }
}
