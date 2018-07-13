using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Sentry.Extensibility;
using Sentry.Http;
using Sentry.Integrations;
using Sentry.Internal;
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
        /// A list of exception processors
        /// </summary>
        internal ImmutableList<ISentryEventExceptionProcessor> ExceptionProcessors { get; set; }

        /// <summary>
        /// A list of event processors
        /// </summary>
        internal ImmutableList<ISentryEventProcessor> EventProcessors { get; set; }

        /// <summary>
        /// A list of providers of <see cref="ISentryEventProcessor"/>
        /// </summary>
        internal ImmutableList<Func<IEnumerable<ISentryEventProcessor>>> EventProcessorsProviders { get; set; }

        /// <summary>
        /// A list of providers of <see cref="ISentryEventExceptionProcessor"/>
        /// </summary>
        internal ImmutableList<Func<IEnumerable<ISentryEventExceptionProcessor>>> ExceptionProcessorsProviders { get; set; }

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
        /// The release version of the application.
        /// </summary>
        /// <example>
        /// 721e41770371db95eee98ca2707686226b993eda
        /// </example>
        /// <remarks>
        /// This value will generally be something along the lines of the git SHA for the given project.
        /// If not explicitly defined via configuration. It will attempt o read it from:
        /// <see cref="System.Reflection.AssemblyInformationalVersionAttribute"/>
        /// </remarks>
        /// <seealso href="https://docs.sentry.io/learn/releases/"/>
        public string Release { get; set; }

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
            = ImmutableList.Create<ISdkIntegration>(new AppDomainUnhandledExceptionIntegration());

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

        /// <summary>
        /// Creates a new instance of <see cref="SentryOptions"/>
        /// </summary>
        public SentryOptions()
        {
            EventProcessorsProviders
                = ImmutableList.Create<Func<IEnumerable<ISentryEventProcessor>>>(
                    () => EventProcessors);

            ExceptionProcessorsProviders
                = ImmutableList.Create<Func<IEnumerable<ISentryEventExceptionProcessor>>>(
                    () => ExceptionProcessors);

            EventProcessors
                = ImmutableList.Create<ISentryEventProcessor>(
                     new DuplicateEventDetectionEventProcessor(),
                     new MainSentryEventProcessor(this));

            ExceptionProcessors
                = ImmutableList.Create<ISentryEventExceptionProcessor>(
                    new MainExceptionProcessor());
        }
    }
}
