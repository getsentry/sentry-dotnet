using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
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
    public class SentryOptions : IScopeOptions
    {
        internal string ClientVersion { get; } = SdkName;

        internal int SentryVersion { get; } = ProtocolVersion;

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

        internal IBackgroundWorker BackgroundWorker { get; set; }

        internal ISentryHttpClientFactory SentryHttpClientFactory { get; set; }

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
        /// Whether to send the stack trace of a event captured without an exception
        /// </summary>
        /// <remarks>
        /// Append stack trace of the call to the SDK to capture a message or event without Exception
        /// </remarks>
        public bool AttachStacktrace { get; set; }

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
        /// A callback invoked when a breadcrumb is about to be stored.
        /// </summary>
        /// <remarks>
        /// Gives a chance to inspect and modify/reject a breadcrumb.
        /// </remarks>
        public Func<Breadcrumb, Breadcrumb> BeforeBreadcrumb { get; set; }

        private int _maxQueueItems = 30;
        /// <summary>
        /// The maximum number of events to keep while the worker attempts to send them
        /// </summary>
        public int MaxQueueItems
        {
            get => _maxQueueItems;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "At least 1 item must be allowed in the queue.");
                }
                _maxQueueItems = value;
            }
        }

        /// <summary>
        /// How long to wait for events to be sent before shutdown
        /// </summary>
        /// <remarks>
        /// In case there are events queued when the SDK is closed, upper bound limit to wait
        /// for the worker to send the events to Sentry.
        /// </remarks>
        /// <example>
        /// The SDK is closed while the queue has 1 event queued.
        /// The worker takes 50 milliseconds to send an event to Sentry.
        /// Even though default settings say 2 seconds, closing the SDK would block for 50ms.
        /// </example>
        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Decompression methods accepted
        /// </summary>
        /// <remarks>
        /// By default accepts all available compression methods supported by the platform
        /// </remarks>
        public DecompressionMethods DecompressionMethods { get; set; }
            // Note the ~ enabling all bits
            = ~DecompressionMethods.None;

        /// <summary>
        /// The level of which to compress the <see cref="SentryEvent"/> before sending to Sentry
        /// </summary>
        /// <remarks>
        /// To disable request body compression, use <see cref="CompressionLevel.NoCompression"/>
        /// </remarks>
        public CompressionLevel RequestBodyCompressionLevel { get; set; } = CompressionLevel.Optimal;

        /// <summary>
        /// Whether the body compression is buffered and the request 'Content-Length' known in advance.
        /// </summary>
        /// <remarks>
        /// Without reading through the Gzip stream to have its final size, it's no possible to use 'Content-Length'
        /// header value. That means 'Content-Encoding: chunked' has to be used which is sometimes not supported.
        /// Sentry on-premise without a reverse-proxy, for example, does not support 'chunked' requests.
        /// </remarks>
        /// <see href="https://github.com/getsentry/sentry-dotnet/issues/71"/>
        public bool RequestBodyCompressionBuffered { get; set; } = true;

        /// <summary>
        /// An optional web proxy
        /// </summary>
        public IWebProxy HttpProxy { get; set; }

        /// <summary>
        /// A callback invoked when a <see cref="SentryClient"/> is created.
        /// </summary>
        public Action<HttpClientHandler, Dsn> ConfigureHandler { get; set; }

        /// <summary>
        /// A callback invoked when a <see cref="SentryClient"/> is created.
        /// </summary>
        public Action<HttpClient, Dsn> ConfigureClient { get; set; }

        private volatile bool _debug;

        /// <summary>
        /// Whether to log diagnostics messages
        /// </summary>
        /// <remarks>
        /// The verbosity can be controlled through <see cref="DiagnosticsLevel"/>
        /// and the implementation via <see cref="DiagnosticLogger"/>.
        /// </remarks>
        public bool Debug
        {
            get => _debug;
            set => _debug = value;
        }

        /// <summary>
        /// The diagnostics level to be used
        /// </summary>
        /// <remarks>
        /// The <see cref="Debug"/> flag has to be switched on for this setting to take effect.
        /// </remarks>
        public SentryLevel DiagnosticsLevel { get; set; } = SentryLevel.Debug;

        private volatile IDiagnosticLogger _diagnosticLogger;
        /// <summary>
        /// The implementation of the logger.
        /// </summary>
        /// <remarks>
        /// The <see cref="Debug"/> flag has to be switched on for this logger to be used at all.
        /// When debugging is turned off, this property is made null and any internal logging results in a no-op.
        /// </remarks>
        public IDiagnosticLogger DiagnosticLogger
        {
            get => Debug ? _diagnosticLogger : null;
            set
            {
                _diagnosticLogger?.LogInfo("Replacing current logger with: '{0}'.", value?.GetType().Name);
                _diagnosticLogger = value;
            }
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

            var sentryStackTraceFactory = new SentryStackTraceFactory(this);
            EventProcessors
                = ImmutableList.Create<ISentryEventProcessor>(
                    // de-dupe to be the first to run
                    new DuplicateEventDetectionEventProcessor(this),
                    new MainSentryEventProcessor(this, sentryStackTraceFactory));

            ExceptionProcessors
                = ImmutableList.Create<ISentryEventExceptionProcessor>(
                    new MainExceptionProcessor(this, sentryStackTraceFactory));

            Integrations
                = ImmutableList.Create<ISdkIntegration>(
                    new AppDomainUnhandledExceptionIntegration());

            InAppExclude
                = ImmutableList.Create(
                    "System.",
                    "Microsoft.",
                    "MS", // MS.Win32, MS.Internal, etc: Desktop apps
                    "Newtonsoft.Json",
                    "FSharp.",
                    "Serilog",
                    "Giraffe.");
        }
    }
}
