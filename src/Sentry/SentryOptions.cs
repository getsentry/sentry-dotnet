using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Sentry.Extensibility;
using Sentry.Http;
using Sentry.Integrations;
using Sentry.Internal;
using Sentry.Protocol;
using static Sentry.Internal.Constants;
using static Sentry.Protocol.Constants;

namespace Sentry
{
    /// <summary>
    /// Sentry SDK options
    /// </summary>
    /// <inheritdoc />
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
        /// A list of integrations to be added when the SDK is initialized
        /// </summary>
        internal ImmutableList<ISdkIntegration> Integrations { get; set; }

        /// <summary>
        /// A list of namespaces (or prefixes) considered not part of application code
        /// </summary>
        /// <remarks>
        /// Sentry by default filters the stacktrace to display only application code.
        /// A user can optionally click to see all which will include framework and libraries.
        /// A <see cref="string.StartsWith(string)"/> is executed
        /// </remarks>
        /// <example>
        /// 'System.', 'Microsoft.'
        /// </example>
        internal ImmutableList<string> InAppExclude { get; set; }

        /// <summary>
        /// Whether to include default Personal Identifiable information
        /// </summary>
        /// <remarks>
        /// By default PII data like Username and Client IP address are not sent to Sentry.
        /// When this flag is turned on, default PII data like Cookies, Claims in Web applications
        /// and user data read from the request are sent.
        /// </remarks>
        public bool SendDefaultPii { get; set; }

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
        /// <inheritdoc />
        public int MaxBreadcrumbs { get; set; } = DefaultMaxBreadcrumbs;

        /// <summary>
        /// The rate to sample events
        /// </summary>
        /// <remarks>
        /// Can be anything between 0.01 (1%) and 1.0 (99.9%) or null (default), to disable it.
        /// </remarks>
        /// <example>
        /// 0.1 = 10% of events are sent
        /// </example>
        /// <see href="https://docs.sentry.io/clientdev/features/#event-sampling"/>
        private float? _sampleRate;
        public float? SampleRate
        {
            get => _sampleRate;
            set
            {
                if (value > 1 || value <= 0)
                {
                    throw new InvalidOperationException($"The value {value} is not valid. Use null to disable or values between 0.01 (inclusive) and 1.0 (exclusive) ");
                }
                _sampleRate = value;
            }
        }

        /// <summary>
        /// The release version of the application.
        /// </summary>
        /// <example>
        /// 721e41770371db95eee98ca2707686226b993eda
        /// 14.1.16.32451
        /// </example>
        /// <remarks>
        /// This value will generally be something along the lines of the git SHA for the given project.
        /// If not explicitly defined via configuration or environment variable (SENTRY_RELEASE).
        /// It will attempt o read it from:
        /// <see cref="System.Reflection.AssemblyInformationalVersionAttribute"/>
        ///
        /// Don't rely on discovery if your release is: '1.0.0' or '0.0.0'. Since those are
        /// default values for new projects, they are not considered valid by the discovery process.
        /// </remarks>
        /// <seealso href="https://docs.sentry.io/learn/releases/"/>
        public string Release { get; set; }

        /// <summary>
        /// The environment the application is running
        /// </summary>
        /// <remarks>
        /// This value can also be set via environment variable: SENTRY_ENVIRONMENT
        /// In some cases you don't need to set this manually since integrations, when possible, automatically fill this value.
        /// For ASP.NET Core which can read from IHostingEnvironment
        /// </remarks>
        /// <example>
        /// Production, Staging
        /// </example>
        /// <seealso href="https://docs.sentry.io/learn/environments/"/>
        public string Environment { get; set; }

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

        // TODO: this shouldn't be a prop exposed. Needs an API to replacing the strategy and the level. Mind lifetime
        public IDiagnosticLogger DiagnosticLogger { get; set; } = new ConsoleDiagnosticLogger(SentryLevel.Error);

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
                    new MainExceptionProcessor(this));

            Integrations
                = ImmutableList.Create<ISdkIntegration>(
                    new AppDomainUnhandledExceptionIntegration());

            InAppExclude
                = ImmutableList.Create(
                    "System.",
                    "Microsoft.",
                    "MS", // MS.Win32, MS.Internal, etc: Desktop apps
                    "FSharp.",
                    "Giraffe.");
        }
    }
}
