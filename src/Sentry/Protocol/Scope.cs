using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
    [DataContract]
    [DebuggerDisplay("Breadcrumbs: {InternalBreadcrumbs?.Count}")]
    public class Scope
    {
        internal IScopeOptions Options { get; }

        public event EventHandler OnEvaluating;

        // Default values are null so no serialization of empty objects or arrays
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
            get => InternalUser ?? User.Empty;
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
        public IImmutableList<string> Fingerprint
        {
            get => InternalFingerprint ?? ImmutableList<string>.Empty;
            internal set => InternalFingerprint = value;
        }

        /// <summary>
        /// A trail of events which happened prior to an issue.
        /// </summary>
        /// <seealso href="https://docs.sentry.io/learn/breadcrumbs/"/>
        public IImmutableList<Breadcrumb> Breadcrumbs
        {
            get => InternalBreadcrumbs ?? ImmutableList<Breadcrumb>.Empty;
            internal set => InternalBreadcrumbs = value;
        }

        /// <summary>
        /// An arbitrary mapping of additional metadata to store with the event.
        /// </summary>
        public IImmutableDictionary<string, string> Extra
        {
            get => InternalExtra ?? ImmutableDictionary<string, string>.Empty;
            internal set => InternalExtra = value;
        }

        /// <summary>
        /// Arbitrary key-value for this event
        /// </summary>
        public IImmutableDictionary<string, string> Tags
        {
            get => InternalTags ?? ImmutableDictionary<string, string>.Empty;
            internal set => InternalTags = value;
        }

        public Scope(IScopeOptions options) => Options = options;
        protected Scope() { } // NOTE: derived types (think Event) don't need to enforce scope semantics

        /// <summary>
        /// Sets the fingerprint to the <see cref="Scope"/>
        /// </summary>
        /// <param name="fingerprint">The fingerprint.</param>
        public void SetFingerprint(IReadOnlyCollection<string> fingerprint) => Fingerprint = fingerprint.ToImmutableList();
        /// <summary>
        /// Sets the extra key-value to the <see cref="Scope"/>
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void SetExtra(string key, string value) => Extra = Extra.Add(key, value);
        /// <summary>
        /// Sets the tag to the <see cref="Scope"/>
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void SetTag(string key, string value) => Tags = Tags.Add(key, value);

        // TODO: make extension methods instead of members
        public void SetTag(in KeyValuePair<string, string> keyValue) => Tags = Tags.Add(keyValue.Key, keyValue.Value);
        public void SetTag(in KeyValuePair<string, object> keyValue) => Tags = Tags.Add(keyValue.Key, keyValue.Value.ToString());
        public void SetTags(IEnumerable<KeyValuePair<string, string>> tags) => Tags = Tags.AddRange(tags);

        internal void Evaluate() => OnEvaluating?.Invoke(this, EventArgs.Empty);

        // TODO: test with reflection to ensure Clone doesn't go out of sync with members
        internal Scope Clone()
        {
            var scope = new Scope(Options);
            this.CopyTo(scope);
            return scope;
        }
    }
}
