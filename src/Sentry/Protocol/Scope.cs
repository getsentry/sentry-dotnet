using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.Serialization;
using Sentry.Internal;

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
    [DebuggerDisplay("Breadcrumbs: {InternalBreadcrumbs?.Count ?? 0}")]
    public class Scope
    {
        internal IScopeOptions Options { get; }

        internal bool Locked { get; set; }

        // Default values are null so no serialization of empty objects or arrays
        [DataMember(Name = "user", EmitDefaultValue = false)]
        internal User InternalUser { get; private set; }

        // TODO: Still has to support key-value
        [DataMember(Name = "contexts", EmitDefaultValue = false)]
        internal Contexts InternalContexts { get; private set; }

        [DataMember(Name = "request", EmitDefaultValue = false)]
        internal Request InternalRequest { get; private set; }

        [DataMember(Name = "fingerprint", EmitDefaultValue = false)]
        internal IImmutableList<string> InternalFingerprint { get; set; }

        [DataMember(Name = "breadcrumbs", EmitDefaultValue = false)]
        internal IImmutableList<Breadcrumb> InternalBreadcrumbs { get; set; }

        [DataMember(Name = "extra", EmitDefaultValue = false)]
        internal IImmutableDictionary<string, object> InternalExtra { get; set; }

        [DataMember(Name = "tags", EmitDefaultValue = false)]
        internal IImmutableDictionary<string, string> InternalTags { get; set; }

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
        public IReadOnlyList<string> Fingerprint => InternalFingerprint ?? ImmutableList<string>.Empty;

        /// <summary>
        /// A trail of events which happened prior to an issue.
        /// </summary>
        /// <seealso href="https://docs.sentry.io/learn/breadcrumbs/"/>
        public IReadOnlyList<Breadcrumb> Breadcrumbs => InternalBreadcrumbs ?? ImmutableList<Breadcrumb>.Empty;

        /// <summary>
        /// An arbitrary mapping of additional metadata to store with the event.
        /// </summary>
        public IReadOnlyDictionary<string, object> Extra => InternalExtra ?? ImmutableDictionary<string, object>.Empty;

        /// <summary>
        /// Arbitrary key-value for this event
        /// </summary>
        public IReadOnlyDictionary<string, string> Tags => InternalTags ?? ImmutableDictionary<string, string>.Empty;

        /// <summary>
        /// An event that fires when the scope evaluates
        /// </summary>
        /// <remarks>
        /// This allows registering an event handler that is invoked in case
        /// an event is about to be sent to Sentry. If an event is never sent,
        /// this event is never fired and the resources spared.
        /// It also allows registration at an early stage of the processing
        /// but execution at a later time, when more data is available.
        /// </remarks>
        /// <see cref="Evaluate"/>
        public event EventHandler OnEvaluating;

        /// <summary>
        /// Creates a scope with the specified options
        /// </summary>
        /// <param name="options"></param>
        public Scope(IScopeOptions options)
            : this(options ?? new SentryOptions(), true)
        {
        }

        private Scope(IScopeOptions options, bool introspect)
        {
            Debug.Assert(options != null);

            Options = options;

            if (introspect)
            {
                try
                {
                    Contexts.Introspect();
                }
                catch (Exception e)
                {
                    // TODO: Log or callback handler here!
                    //Options.HandleError
                    Console.WriteLine(e);
                }
            }
        }

        /// <summary>
        /// Creates a new scope with default options
        /// </summary>
        protected internal Scope()
            : this(null)
        { }

        internal void Evaluate()
        {
            try
            {
                OnEvaluating?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                this.AddBreadcrumb("Failed invoking event handler: " + e,
                    level: BreadcrumbLevel.Error);
            }
        }

        // TODO: test with reflection to ensure Clone doesn't go out of sync with members
        internal Scope Clone()
        {
            Debug.Assert(!Locked);

            var scope = new Scope(Options, false);
            this.CopyTo(scope);
            return scope;
        }
    }
}
