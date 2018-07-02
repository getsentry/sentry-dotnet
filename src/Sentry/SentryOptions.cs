using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Sentry.Http;
using Sentry.Integrations;
using static Sentry.Internal.Constants;

namespace Sentry
{
    /// <summary>
    /// Sentry SDK options
    /// </summary>
    public class SentryOptions : IScopeOptions
    {
        internal string ClientVersion { get; } = SdkName;

        internal int SentryVersion { get; } = ProtocolVersion;

        internal Action<BackgroundWorkerOptions> ConfigureBackgroundWorkerOptions { get; private set; }

        internal List<Action<HttpOptions>> ConfigureHttpTransportOptions { get; private set; }

        /// <summary>
        /// Gets or sets the maximum breadcrumbs.
        /// </summary>
        /// <remarks>
        /// When the number of events reach this configuration value,
        /// older breadcrumbs start dropping to make room for new ones.
        /// </remarks>
        /// <value>
        /// The maximum breadcrumbs per scope.
        /// </value>
        public int MaxBreadcrumbs { get; set; } = DefaultMaxBreadcrumbs;

        /// <summary>
        /// The Data Source Name of a given project in Sentry.
        /// </summary>
        public Dsn Dsn { get; set; }

        /// <summary>
        /// A callback to invoke before sending an event to Sentry
        /// </summary>
        /// <remarks>
        /// The return of this event will be sent to Sentry. This allows the application
        /// a chance to inspect and/or modify the event before it's sent. If the event
        /// should not be sent at all, return null from the callback.
        /// </remarks>
        public Func<SentryEvent, SentryEvent> BeforeSend { get; set; }

        /// <summary>
        /// A list of integrations to be added when the SDK is initialized
        /// </summary>
        /// <remarks>
        /// Default integrations are defined out of the box. It's possible to disable these
        /// integrations by means of modifying this list before initializing the SDK.
        /// </remarks>
        public ImmutableList<ISdkIntegration> Integrations { get; set; }
            = new ISdkIntegration[] { new AppDomainUnhandledExceptionIntegration() }.ToImmutableList();

        /// <summary>
        /// Configure the background worker options
        /// </summary>
        /// <param name="configure">The callback to configure background worker options</param>
        public void Worker(Action<BackgroundWorkerOptions> configure) => ConfigureBackgroundWorkerOptions = configure;

        /// <summary>
        /// Configure HTTP related options
        /// </summary>
        /// <param name="configure"></param>
        public void Http(Action<HttpOptions> configure)
        {
            if (ConfigureHttpTransportOptions == null)
            {
                ConfigureHttpTransportOptions = new List<Action<HttpOptions>>(1);
            }
            ConfigureHttpTransportOptions.Add(configure);
        }
    }
}
