using Sentry.Extensibility;
using Sentry.Http;
using Sentry.Infrastructure;
using Sentry.Integrations;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Internal.Http;
using Sentry.Internal.ScopeStack;
using Sentry.PlatformAbstractions;
using static Sentry.SentryConstants;

#if HAS_DIAGNOSTIC_INTEGRATION
using Sentry.Internal.DiagnosticSource;
#endif

#if ANDROID
using Sentry.Android;
using Sentry.Android.AssemblyReader;
#endif

namespace Sentry;

/// <summary>
/// Sentry SDK options
/// </summary>
#if __MOBILE__
public partial class SentryOptions
#else
public class SentryOptions
#endif
{
    private Dictionary<string, string>? _defaultTags;
    private const RegexOptions DefaultRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant;

    /// <summary>
    /// If set, the <see cref="SentryScopeManager"/> will ignore <see cref="IsGlobalModeEnabled"/>
    /// and use the provided container instead.
    /// </summary>
    /// <remarks>
    /// Used by the ASP.NET (classic) integration.
    /// </remarks>
    internal IScopeStackContainer? ScopeStackContainer { get; set; }

    private Lazy<string?> LazyInstallationId => new(()
        => new InstallationIdHelper(this).TryGetInstallationId());
    internal string? InstallationId => LazyInstallationId.Value;

#if __MOBILE__
    private bool _isGlobalModeEnabled = true;
    /// <summary>
    /// Specifies whether to use global scope management mode.
    /// Should be <c>true</c> for client applications and <c>false</c> for server applications.
    /// The default is <c>true</c> for mobile targets.
    /// </summary>
    public bool IsGlobalModeEnabled
    {
        get => _isGlobalModeEnabled;
        set
        {
            _isGlobalModeEnabled = value;
            if (!value)
            {
                _diagnosticLogger?.LogWarning("Global Mode should usually be enabled on {0}", DeviceInfo.PlatformName);
            }
        }
    }
#else
    private bool? _isGlobalModeEnabled;

    /// <summary>
    /// Specifies whether to use global scope management mode.
    /// Should be <c>true</c> for client applications and <c>false</c> for server applications.
    /// The default is <c>false</c>. The default for Blazor WASM, MAUI, and Mobile apps is <c>true</c>.
    /// </summary>
    public bool IsGlobalModeEnabled
    {
        get => _isGlobalModeEnabled ??= SentryRuntime.Current.IsBrowserWasm();
        set => _isGlobalModeEnabled = value;
    }
#endif

    /// <summary>
    /// A scope set outside of Sentry SDK. If set, the global parameters from the SDK's scope will be sent to the observed scope.<br/>
    /// NOTE: EnableScopeSync must be set true for the scope to be synced.
    /// </summary>
    public IScopeObserver? ScopeObserver { get; set; }

    /// <summary>
    /// If true, the SDK's scope will be synced with the observed scope.
    /// </summary>
    public bool EnableScopeSync { get; set; }

    /// <summary>
    /// This holds a reference to the current transport, when one is active.
    /// If set manually before initialization, the provided transport will be used instead of the default transport.
    /// </summary>
    /// <remarks>
    /// If <seealso cref="CacheDirectoryPath"/> is set, any transport set here will be wrapped in a
    /// <seealso cref="CachingTransport"/> and used as its inner transport.
    /// </remarks>
    public ITransport? Transport { get; set; }

    private Lazy<IClientReportRecorder> _clientReportRecorder;

    internal IClientReportRecorder ClientReportRecorder
    {
        get => _clientReportRecorder.Value;
        set => _clientReportRecorder = new Lazy<IClientReportRecorder>(() => value);
    }

    private Lazy<ISentryStackTraceFactory> _sentryStackTraceFactory;

    internal ISentryStackTraceFactory SentryStackTraceFactory
    {
        get => _sentryStackTraceFactory.Value;
        set => _sentryStackTraceFactory = new Lazy<ISentryStackTraceFactory>(() => value);
    }

    internal int SentryVersion { get; } = ProtocolVersion;

    /// <summary>
    /// A list of exception processors
    /// </summary>
    internal List<(Type Type, Lazy<ISentryEventExceptionProcessor> Lazy)> ExceptionProcessors { get; set; }

    /// <summary>
    /// A list of transaction processors
    /// </summary>
    internal List<ISentryTransactionProcessor>? TransactionProcessors { get; set; }

    /// <summary>
    /// A list of event processors
    /// </summary>
    internal List<(Type Type, Lazy<ISentryEventProcessor> Lazy)> EventProcessors { get; set; }

    /// <summary>
    /// A list of providers of <see cref="ISentryEventProcessor"/>
    /// </summary>
    internal List<Func<IEnumerable<ISentryEventProcessor>>> EventProcessorsProviders { get; set; }

    /// <summary>
    /// A list of providers of <see cref="ISentryTransactionProcessor"/>
    /// </summary>
    internal List<Func<IEnumerable<ISentryTransactionProcessor>>> TransactionProcessorsProviders { get; set; }

    /// <summary>
    /// A list of providers of <see cref="ISentryEventExceptionProcessor"/>
    /// </summary>
    internal List<Func<IEnumerable<ISentryEventExceptionProcessor>>> ExceptionProcessorsProviders { get; set; }

    private DefaultIntegrations _defaultIntegrations;

    /// <summary>
    /// A list of integrations to be added when the SDK is initialized.
    /// </summary>
    internal IEnumerable<ISdkIntegration> Integrations
    {
        get
        {
            // Auto-session tracking to be the first to run
            if ((_defaultIntegrations & DefaultIntegrations.AutoSessionTrackingIntegration) != 0)
            {
                yield return new AutoSessionTrackingIntegration();
            }

            if ((_defaultIntegrations & DefaultIntegrations.AppDomainUnhandledExceptionIntegration) != 0)
            {
                yield return new AppDomainUnhandledExceptionIntegration();
            }

            if ((_defaultIntegrations & DefaultIntegrations.AppDomainProcessExitIntegration) != 0)
            {
                yield return new AppDomainProcessExitIntegration();
            }

            if ((_defaultIntegrations & DefaultIntegrations.UnobservedTaskExceptionIntegration) != 0)
            {
                yield return new UnobservedTaskExceptionIntegration();
            }

#if NETFRAMEWORK
            if ((_defaultIntegrations & DefaultIntegrations.NetFxInstallationsIntegration) != 0)
            {
                yield return new NetFxInstallationsIntegration();
            }
#endif

#if HAS_DIAGNOSTIC_INTEGRATION
            if ((_defaultIntegrations & DefaultIntegrations.SentryDiagnosticListenerIntegration) != 0)
            {
                yield return new SentryDiagnosticListenerIntegration();
            }
#endif

#if NET5_0_OR_GREATER && !__MOBILE__
            if ((_defaultIntegrations & DefaultIntegrations.WinUiUnhandledExceptionIntegration) != 0
                && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return new WinUIUnhandledExceptionIntegration();
            }
#endif

#if NET8_0_OR_GREATER
            if ((_defaultIntegrations & DefaultIntegrations.SystemDiagnosticsMetricsIntegration) != 0)
            {
                yield return new SystemDiagnosticsMetricsIntegration();
            }
#endif

            foreach (var integration in _integrations)
            {
                yield return integration;
            }
        }
    }

    internal List<IExceptionFilter>? ExceptionFilters { get; set; } = new();

    /// <summary>
    /// List of substrings or regular expression patterns to filter out tags
    /// </summary>
    public ICollection<SubstringOrRegexPattern> TagFilters { get; set; } = new List<SubstringOrRegexPattern>();

    /// <summary>
    /// The worker used by the client to pass envelopes.
    /// </summary>
    public IBackgroundWorker? BackgroundWorker { get; set; }

    internal ISentryHttpClientFactory? SentryHttpClientFactory { get; set; }

    internal HttpClient GetHttpClient()
    {
        var factory = SentryHttpClientFactory ?? new DefaultSentryHttpClientFactory();
        return factory.Create(this);
    }

    /// <summary>
    /// Scope state processor.
    /// </summary>
    public ISentryScopeStateProcessor SentryScopeStateProcessor { get; set; } = new DefaultSentryScopeStateProcessor();

    /// <summary>
    /// A list of prefixes or patterns used to filter namespaces that are not considered part of application code
    /// </summary>
    /// <remarks>
    /// Sentry by default filters the stacktrace to display only application code.
    /// A user can optionally click to see all which will include framework and libraries.
    /// </remarks>
    /// <example>
    /// 'System.', 'Microsoft.'
    /// </example>
    internal List<StringOrRegex>? InAppExclude { get; set; }

    /// <summary>
    /// A list of prefixes or patterns used to filter namespaces that are considered part of application code
    /// </summary>
    /// <remarks>
    /// Sentry by default filters the stacktrace to display only application code.
    /// A user can optionally click to see all which will include framework and libraries.
    /// </remarks>
    /// <example>
    /// 'System.CustomNamespace', 'Microsoft.Azure.App'
    /// </example>
    /// <seealso href="https://docs.sentry.io/platforms/dotnet/guides/aspnet/configuration/options/#in-app-include"/>
    internal List<StringOrRegex>? InAppInclude { get; set; }

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
    /// Whether to report the <see cref="System.Environment.UserName"/> as the User affected in the event.
    /// </summary>
    /// <remarks>
    /// This configuration is only relevant if <see cref="SendDefaultPii"/> is set to true.
    /// In environments like server applications this is set to false in order to not report server account names as user names.
    /// </remarks>
    public bool IsEnvironmentUser { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the server running the application.
    /// </summary>
    /// <remarks>
    /// When <see cref="SendDefaultPii"/> is set to <c>true</c>, <see cref="System.Environment.MachineName"/> is
    /// automatically set as ServerName. This property can serve as an override.
    /// This is relevant only to server applications.
    /// </remarks>
    public string? ServerName { get; set; }

    /// <summary>
    /// Whether to send the stack trace of a event captured without an exception.
    /// As of version 3.22.0, the default is <c>true</c>.
    /// </summary>
    /// <remarks>
    /// Append stack trace of the call to the SDK to capture a message or event without Exception
    /// </remarks>
    public bool AttachStacktrace { get; set; } = true;

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

    private float? _sampleRate;

    /// <summary>
    /// The rate to sample error and crash events.
    /// </summary>
    /// <remarks>
    /// Can be anything between 0.01 (1%) and 1.0 (99.9%) or null (default), to disable it.
    /// </remarks>
    /// <example>
    /// 0.1 = 10% of events are sent
    /// </example>
    /// <see href="https://develop.sentry.dev/sdk/features/#event-sampling"/>
    /// <exception cref="InvalidOperationException"></exception>
    public float? SampleRate
    {
        get => _sampleRate;
        set
        {
            if (value is > 1 or <= 0)
            {
                throw new InvalidOperationException(
                    $"The value {value} is not valid. Use null to disable or values between 0.01 (inclusive) and 1.0 (exclusive) ");
            }

            _sampleRate = value;
        }
    }

    /// <summary>
    /// The release information for the application.
    /// Can be anything, but generally should be either a semantic version string in the format
    /// <c>package@version</c> or <c>package@version+build</c>, or a commit SHA from a version control system.
    /// </summary>
    /// <example>
    /// MyApp@1.2.3
    /// MyApp@1.2.3+foo
    /// 721e41770371db95eee98ca2707686226b993eda
    /// 14.1.16.32451
    /// </example>
    /// <remarks>
    /// This value will generally be something along the lines of the git SHA for the given project.
    /// If not explicitly defined via configuration or environment variable (SENTRY_RELEASE).
    /// It will attempt to read it from:
    /// <see cref="System.Reflection.AssemblyInformationalVersionAttribute"/>
    /// </remarks>
    /// <seealso href="https://docs.sentry.io/platforms/dotnet/configuration/releases/"/>
    public string? Release { get; set; }

    /// <summary>
    /// The distribution of the application, associated with the release set in <see cref="Release"/>.
    /// </summary>
    /// <example>
    /// 22
    /// 14G60
    /// </example>
    /// <remarks>
    /// Distributions are used to disambiguate build or deployment variants of the same release of
    /// an application. For example, it can be the build number of an XCode (iOS) build, or the version
    /// code of an Android build.
    /// A distribution can be set under any circumstances, and is passed along to Sentry if provided.
    /// However, they are generally relevant only for mobile application scenarios.
    /// </remarks>
    /// <seealso href="https://develop.sentry.dev/sdk/event-payloads/#optional-attributes"/>
    public string? Distribution { get; set; }

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
    /// <seealso href="https://docs.sentry.io/platforms/dotnet/configuration/environments/"/>
    public string? Environment { get; set; }

    private string? _dsn;

    /// <summary>
    /// The Data Source Name of a given project in Sentry.
    /// </summary>
    public string? Dsn
    {
        get => _dsn;
        set
        {
            _dsn = value;
            _parsedDsn = null;
        }
    }

    internal Dsn? _parsedDsn;
    internal Dsn ParsedDsn => _parsedDsn ??= Sentry.Dsn.Parse(Dsn!);

    private readonly Lazy<string> _sentryBaseUrl;

    internal bool IsSentryRequest(string? requestUri) =>
        !string.IsNullOrEmpty(requestUri) && IsSentryRequest(new Uri(requestUri));

    internal bool IsSentryRequest(Uri? requestUri)
    {
        if (string.IsNullOrEmpty(Dsn) || requestUri is null)
        {
            return false;
        }

        var requestBaseUrl = requestUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
        return string.Equals(requestBaseUrl, _sentryBaseUrl.Value, StringComparison.OrdinalIgnoreCase);
    }

    private Func<SentryEvent, SentryHint, SentryEvent?>? _beforeSend;

    internal Func<SentryEvent, SentryHint, SentryEvent?>? BeforeSendInternal => _beforeSend;

    /// <summary>
    /// Configures a callback function to be invoked before sending an event to Sentry
    /// </summary>
    /// <remarks>
    /// The event returned by this callback will be sent to Sentry. This allows the
    /// application a chance to inspect and/or modify the event before it's sent. If the
    /// event should not be sent at all, return null from the callback.
    /// </remarks>
    public void SetBeforeSend(Func<SentryEvent, SentryHint, SentryEvent?> beforeSend)
    {
        _beforeSend = beforeSend;
    }

    /// <summary>
    /// Configures a callback function to be invoked before sending an event to Sentry
    /// </summary>
    /// <remarks>
    /// The event returned by this callback will be sent to Sentry. This allows the
    /// application a chance to inspect and/or modify the event before it's sent. If the
    /// event should not be sent at all, return null from the callback.
    /// </remarks>
    public void SetBeforeSend(Func<SentryEvent, SentryEvent?> beforeSend)
    {
        _beforeSend = (@event, _) => beforeSend(@event);
    }

    private Func<SentryTransaction, SentryHint, SentryTransaction?>? _beforeSendTransaction;

    internal Func<SentryTransaction, SentryHint, SentryTransaction?>? BeforeSendTransactionInternal => _beforeSendTransaction;

    /// <summary>
    /// Configures a callback to invoke before sending a transaction to Sentry
    /// </summary>
    /// <param name="beforeSendTransaction">The callback</param>
    public void SetBeforeSendTransaction(Func<SentryTransaction, SentryHint, SentryTransaction?> beforeSendTransaction)
    {
        _beforeSendTransaction = beforeSendTransaction;
    }

    /// <summary>
    /// Configures a callback to invoke before sending a transaction to Sentry
    /// </summary>
    /// <param name="beforeSendTransaction">The callback</param>
    public void SetBeforeSendTransaction(Func<SentryTransaction, SentryTransaction?> beforeSendTransaction)
    {
        _beforeSendTransaction = (transaction, _) => beforeSendTransaction(transaction);
    }

    private Func<Breadcrumb, SentryHint, Breadcrumb?>? _beforeBreadcrumb;

    internal Func<Breadcrumb, SentryHint, Breadcrumb?>? BeforeBreadcrumbInternal => _beforeBreadcrumb;

    /// <summary>
    /// Sets a callback function to be invoked when a breadcrumb is about to be stored.
    /// </summary>
    /// <remarks>
    /// Gives a chance to inspect and modify the breadcrumb. If null is returned, the
    /// breadcrumb will be discarded. Otherwise the result of the callback will be stored.
    /// </remarks>
    public void SetBeforeBreadcrumb(Func<Breadcrumb, SentryHint, Breadcrumb?> beforeBreadcrumb)
    {
        _beforeBreadcrumb = beforeBreadcrumb;
    }

    /// <summary>
    /// Sets a callback function to be invoked when a breadcrumb is about to be stored.
    /// </summary>
    /// <remarks>
    /// Gives a chance to inspect and modify the breadcrumb. If null is returned, the
    /// breadcrumb will be discarded. Otherwise the result of the callback will be stored.
    /// </remarks>
    public void SetBeforeBreadcrumb(Func<Breadcrumb, Breadcrumb?> beforeBreadcrumb)
    {
        _beforeBreadcrumb = (breadcrumb, _) => beforeBreadcrumb(breadcrumb);
    }

    private int _maxQueueItems = 30;

    /// <summary>
    /// The maximum number of events to keep while the worker attempts to send them.
    /// </summary>
    public int MaxQueueItems
    {
        get => _maxQueueItems;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "At least 1 item must be allowed in the queue.");
            }

            _maxQueueItems = value;
        }
    }

    private int _maxCacheItems = 30;

    /// <summary>
    /// The maximum number of events to keep in cache.
    /// This option only works if <see cref="CacheDirectoryPath"/> is configured as well.
    /// </summary>
    public int MaxCacheItems
    {
        get => _maxCacheItems;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "At least 1 item must be allowed in the cache.");
            }

            _maxCacheItems = value;
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
    /// How long to wait for flush operations to finish. Defaults to 2 seconds.
    /// </summary>
    /// <remarks>
    /// When using the <c>Sentry.NLog</c> integration, the default is increased to 15 seconds.
    /// </remarks>
    public TimeSpan FlushTimeout { get; set; } = TimeSpan.FromSeconds(2);

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
    /// Whether to send client reports, which contain statistics about discarded events.
    /// </summary>
    /// <see href="https://develop.sentry.dev/sdk/client-reports/"/>
    public bool SendClientReports { get; set; } = true;

    /// <summary>
    /// An optional web proxy
    /// </summary>
    public IWebProxy? HttpProxy { get; set; }

    /// <summary>
    /// Creates the inner most <see cref="HttpMessageHandler"/>.
    /// </summary>
    public Func<HttpMessageHandler>? CreateHttpMessageHandler { get; set; }

    /// <summary>
    /// A callback invoked when a <see cref="SentryClient"/> is created.
    /// </summary>
    public Action<HttpClient>? ConfigureClient { get; set; }

    private volatile bool _debug;

    /// <summary>
    /// Whether to log diagnostics messages
    /// </summary>
    /// <remarks>
    /// The verbosity can be controlled through <see cref="DiagnosticLevel"/>
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
    public SentryLevel DiagnosticLevel { get; set; } = SentryLevel.Debug;

    private volatile IDiagnosticLogger? _diagnosticLogger;

    /// <summary>
    /// The implementation of the logger.
    /// </summary>
    /// <remarks>
    /// The <see cref="Debug"/> flag has to be switched on for this logger to be used at all.
    /// When debugging is turned off, this property is made null and any internal logging results in a no-op.
    /// </remarks>
    public IDiagnosticLogger? DiagnosticLogger
    {
        get => Debug ? _diagnosticLogger : null;
        set
        {
            if (value is null)
            {
                _diagnosticLogger?.LogDebug(
                    "Sentry will not emit SDK debug messages because debug mode has been turned off.");
            }
            else
            {
                _diagnosticLogger?.LogInfo("Replacing current logger with: '{0}'.", value.GetType().Name);
            }

            _diagnosticLogger = value;
        }
    }

    /// <summary>
    /// What mode to use for reporting referenced assemblies in each event sent to sentry. Defaults to <see cref="Sentry.ReportAssembliesMode.Version"/>.
    /// </summary>
    public ReportAssembliesMode ReportAssembliesMode { get; set; } = ReportAssembliesMode.Version;

    /// <summary>
    /// What modes to use for event automatic deduplication
    /// </summary>
    /// <remarks>
    /// By default will not drop an event solely for including an inner exception that was already captured.
    /// </remarks>
    public DeduplicateMode DeduplicateMode { get; set; } = DeduplicateMode.All ^ DeduplicateMode.InnerException;

    /// <summary>
    /// Path to the root directory used for storing events locally for resilience.
    /// If set to <i>null</i>, caching will not be used.
    /// </summary>
    public string? CacheDirectoryPath { get; set; }

    /// <summary>
    /// <para>The SDK will only capture HTTP Client errors if it is enabled.</para>
    /// <para><see cref="FailedRequestStatusCodes"/> can be used to configure which requests will be treated as failed.</para>
    /// <para>Also <see cref="FailedRequestTargets"/> can be used to filter to match only certain request URLs.</para>
    /// <para>Defaults to true.</para>
    /// </summary>
    public bool CaptureFailedRequests { get; set; } = true;

    /// <summary>
    /// <para>The SDK will only capture HTTP Client errors if the HTTP Response status code is within these defined ranges.</para>
    /// <para>Defaults to 500-599 (Server error responses only).</para>
    /// </summary>
    public IList<HttpStatusCodeRange> FailedRequestStatusCodes { get; set; } = new List<HttpStatusCodeRange>
    {
        (500, 599)
    };

    // The default failed request target list will match anything, but adding to the list should clear that.
    private Lazy<IList<SubstringOrRegexPattern>> _failedRequestTargets = new(() =>
        new AutoClearingList<SubstringOrRegexPattern>(
            new[] { new SubstringOrRegexPattern(".*") }, clearOnNextAdd: true));

    /// <summary>
    /// <para>The SDK will only capture HTTP Client errors if the HTTP Request URL is a match for any of the failedRequestsTargets.</para>
    /// <para>Targets may be URLs or Regular expressions.</para>
    /// <para>Matches "*." by default.</para>
    /// </summary>
    public IList<SubstringOrRegexPattern> FailedRequestTargets
    {
        get => _failedRequestTargets.Value;
        set => _failedRequestTargets = new(value.WithConfigBinding);
    }

    /// <summary>
    /// Sets the filesystem instance to use. Defaults to the actual <see cref="Sentry.Internal.FileSystem"/>.
    /// Used for testing.
    /// </summary>
    internal IFileSystem FileSystem { get; set; } = Internal.FileSystem.Instance;

    /// <summary>
    /// If set to a positive value, Sentry will attempt to flush existing local event cache when initializing.
    /// Set to <see cref="TimeSpan.Zero"/> to disable this feature.
    /// This option only works if <see cref="CacheDirectoryPath"/> is configured as well.
    /// </summary>
    /// <remarks>
    /// The trade off here is: Ensure a crash that happens during app start is sent to Sentry
    /// even though that might slow down the app start. If set to false, the app might crash
    /// too quickly, before Sentry can capture the cached error in the background.
    /// </remarks>
    public TimeSpan InitCacheFlushTimeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Defaults tags to add to all events. (These are indexed by Sentry).
    /// </summary>
    /// <remarks>
    /// If the key already exists in the event, it will not be overwritten by a default tag.
    /// </remarks>
    public Dictionary<string, string> DefaultTags
    {
        get => _defaultTags ??= new Dictionary<string, string>();
        internal set => _defaultTags = value;
    }

    /// <summary>
    /// Indicates whether the performance feature is enabled, via any combination of
    /// <see cref="EnableTracing"/>, <see cref="TracesSampleRate"/>, or <see cref="TracesSampler"/>.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    internal bool IsPerformanceMonitoringEnabled => EnableTracing switch
    {
        false => false,
        null => TracesSampler is not null || TracesSampleRate is > 0.0,
        true => TracesSampler is not null || TracesSampleRate is > 0.0 or null
    };
#pragma warning restore CS0618 // Type or member is obsolete

    /// <summary>
    /// Indicates whether profiling is enabled, via any combination of
    /// <see cref="EnableTracing"/>, <see cref="TracesSampleRate"/>, or <see cref="TracesSampler"/>.
    /// </summary>
    internal bool IsProfilingEnabled => IsPerformanceMonitoringEnabled && ProfilesSampleRate > 0.0;

    /// <summary>
    /// Simplified option for enabling or disabling tracing.
    /// <list type="table">
    ///   <listheader>
    ///     <term>Value</term>
    ///     <description>Effect</description>
    ///   </listheader>
    ///   <item>
    ///     <term><c>true</c></term>
    ///     <description>
    ///       Tracing is enabled. <see cref="TracesSampleRate"/> or <see cref="TracesSampler"/> will be used if set,
    ///       or 100% sample rate will be used otherwise.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term><c>false</c></term>
    ///     <description>
    ///       Tracing is disabled, regardless of <see cref="TracesSampleRate"/> or <see cref="TracesSampler"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term><c>null</c></term>
    ///     <description>
    ///       <b>The default setting.</b>
    ///       Tracing is enabled only if <see cref="TracesSampleRate"/> or <see cref="TracesSampler"/> are set.
    ///     </description>
    ///   </item>
    /// </list>
    /// </summary>
    [Obsolete("Use TracesSampleRate or TracesSampler instead")]
    public bool? EnableTracing { get; set; }

    private double? _tracesSampleRate;

    /// <summary>
    /// Indicates the percentage of the tracing data that is collected.
    /// <list type="table">
    ///   <listheader>
    ///     <term>Value</term>
    ///     <description>Effect</description>
    ///   </listheader>
    ///   <item>
    ///     <term><c>&gt;= 0.0 and &lt;=1.0</c></term>
    ///     <description>
    ///       A custom sample rate is used unless <see cref="EnableTracing"/> is <c>false</c>,
    ///       or unless overriden by a <see cref="TracesSampler"/> function.
    ///       Values outside of this range are invalid.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term><c>null</c></term>
    ///     <description>
    ///       <b>The default setting.</b>
    ///       The tracing sample rate is determined by the <see cref="EnableTracing"/> property,
    ///       unless overriden by a <see cref="TracesSampler"/> function.
    ///     </description>
    ///   </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Random sampling rate is only applied to transactions that don't already
    /// have a sampling decision set by other means, such as through <see cref="TracesSampler"/>,
    /// by inheriting it from an incoming trace header, or by copying it from <see cref="TransactionContext"/>.
    /// </remarks>
    public double? TracesSampleRate
    {
        get => _tracesSampleRate;
        set
        {
            if (value is < 0.0 or > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "The traces sample rate must be between 0.0 and 1.0, inclusive.");
            }

            _tracesSampleRate = value;
        }
    }

    private double? _profilesSampleRate;

    /// <summary>
    /// The sampling rate for profiling is relative to <see cref="TracesSampleRate"/>.
    /// Setting to 1.0 will profile 100% of sampled transactions.
    /// <list type="table">
    ///   <listheader>
    ///     <term>Value</term>
    ///     <description>Effect</description>
    ///   </listheader>
    ///   <item>
    ///     <term><c>&gt;= 0.0 and &lt;=1.0</c></term>
    ///     <description>
    ///       A custom sample rate is. Values outside of this range are invalid.
    ///       Setting to 0.0 will disable profiling.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term><c>null</c></term>
    ///     <description>
    ///       <b>The default setting.</b>
    ///       At this time, this is equivalent to 0.0, i.e. disabling profiling, but that may change in the future.
    ///     </description>
    ///   </item>
    /// </list>
    /// </summary>
    public double? ProfilesSampleRate
    {
        get => _profilesSampleRate;
        set
        {
            if (value is < 0.0 or > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "The profiles sample rate must be between 0.0 and 1.0, inclusive.");
            }

            _profilesSampleRate = value;
        }
    }

    /// <summary>
    /// Custom delegate that returns sample rate dynamically for a specific transaction context.
    /// </summary>
    /// <remarks>
    /// Returning <c>null</c> signals that the sampler did not reach a sampling decision.
    /// In such case, if the transaction already has a sampling decision (for example, if it's
    /// started from a trace header) that decision is retained.
    /// Otherwise sampling decision is determined by applying the static sampling rate
    /// set in <see cref="TracesSampleRate"/>.
    /// </remarks>
    public Func<TransactionSamplingContext, double?>? TracesSampler { get; set; }

    // The default propagation list will match anything, but adding to the list should clear that.
    private IList<SubstringOrRegexPattern> _tracePropagationTargets = new AutoClearingList<SubstringOrRegexPattern>
        (new[] { new SubstringOrRegexPattern(".*") }, clearOnNextAdd: true);

    /// <summary>
    /// A customizable list of <see cref="SubstringOrRegexPattern"/> objects, each containing either a
    /// substring or regular expression pattern that can be used to control which outgoing HTTP requests
    /// will have the <c>sentry-trace</c> and <c>baggage</c> headers propagated, for purposes of distributed tracing.
    /// The default value contains a single value of <c>.*</c>, which matches everything.
    /// To disable propagation completely, clear this collection or set it to an empty collection.
    /// </summary>
    /// <seealso href="https://develop.sentry.dev/sdk/performance/#tracepropagationtargets"/>
    /// <remarks>
    /// Adding an item to the default list will clear the <c>.*</c> value automatically.
    /// </remarks>
    public IList<SubstringOrRegexPattern> TracePropagationTargets
    {
        // NOTE: During configuration binding, .NET 6 and lower used to just call Add on the existing item.
        //       .NET 7 changed this to call the setter with an array that already starts with the old value.
        //       We have to handle both cases.
        get => _tracePropagationTargets;
        set => _tracePropagationTargets = value.WithConfigBinding();
    }

    internal ITransactionProfilerFactory? TransactionProfilerFactory { get; set; }

    private StackTraceMode? _stackTraceMode;
    private readonly List<ISdkIntegration> _integrations = new();

    /// <summary>
    /// ATTENTION: This option will change how issues are grouped in Sentry!
    /// </summary>
    /// <remarks>
    /// Sentry groups events by stack traces. If you change this mode and you have thousands of groups,
    /// you'll get thousands of new groups. So use this setting with care.
    /// </remarks>
    public StackTraceMode StackTraceMode
    {
        get
        {
            if (_stackTraceMode is not null)
            {
                return _stackTraceMode.Value;
            }

            try
            {
                // from 3.0.0 uses Enhanced (Ben.Demystifier) by default which is a breaking change
                // unless you are using .NET Native which isn't compatible with Ben.Demystifier.
                _stackTraceMode = SentryRuntime.Current.Name == ".NET Native"
                    ? StackTraceMode.Original
                    : StackTraceMode.Enhanced;
            }
            catch (Exception ex)
            {
                _stackTraceMode = StackTraceMode.Enhanced;
                DiagnosticLogger?.LogError(ex, "Failed to get runtime, setting {0} to {1} ", nameof(StackTraceMode), _stackTraceMode);
            }

            return _stackTraceMode.Value;
        }
        set => _stackTraceMode = value;
    }

    /// <summary>
    /// Maximum allowed file size of attachments, in bytes.
    /// Attachments above this size will be discarded.
    /// </summary>
    /// <remarks>
    /// Regardless of this setting, attachments are also limited to 20mb (compressed) on Relay.
    /// </remarks>
    public long MaxAttachmentSize { get; set; } = 20 * 1024 * 1024;

    /// <summary>
    /// The mode that the SDK should use when attempting to detect the app's and device's startup time.
    /// </summary>
    /// <remarks>
    /// Note that the highest precision value relies on <see cref="System.Diagnostics.Process.GetCurrentProcess"/>
    /// which might not be available. For example on Unity's IL2CPP or WebAssembly.
    /// Additionally, "Best" mode is not available on mobile platforms.
    /// </remarks>
    public StartupTimeDetectionMode DetectStartupTime { get; set; } =
#if __MOBILE__
        StartupTimeDetectionMode.Fast;
#else
        SentryRuntime.Current.IsBrowserWasm() ? StartupTimeDetectionMode.Fast : StartupTimeDetectionMode.Best;
#endif

    /// <summary>
    /// Determines the duration of time a session can stay paused before it's considered ended.
    /// </summary>
    /// <remarks>
    /// Note: This interval is only taken into account when integrations support Pause and Resume.
    /// </remarks>
    public TimeSpan AutoSessionTrackingInterval { get; set; } = TimeSpan.FromSeconds(30);

#if __MOBILE__
    /// <summary>
    /// Whether the SDK should start a session automatically when it's initialized and
    /// end the session when it's closed.
    /// On mobile application platforms, this is enabled by default.
    /// </summary>
    public bool AutoSessionTracking { get; set; } = true;
#else
    /// <summary>
    /// Whether the SDK should start a session automatically when it's initialized and
    /// end the session when it's closed.
    /// </summary>
    /// <remarks>
    /// Note: this is disabled by default in the current version (except for mobile targets and MAUI),
    /// but will become enabled by default in the next major version.
    /// Currently this only works for release health in client mode
    /// (desktop, mobile applications, but not web servers).
    /// </remarks>
    public bool AutoSessionTracking { get; set; } = false;
#endif

    /// <summary>
    /// Whether the SDK should attempt to use asynchronous file I/O.
    /// For example, when reading a file to use as an attachment.
    /// </summary>
    /// <remarks>
    /// This option should rarely be disabled, but is necessary in some environments such as Unity WebGL.
    /// </remarks>
    public bool UseAsyncFileIO { get; set; } = true;

    /// <summary>
    /// Delegate which is used to check whether the application crashed during last run.
    /// </summary>
    public Func<bool>? CrashedLastRun { get; set; }

    /// <summary>
    /// <para>
    ///     Gets the <see cref="Instrumenter"/> used to create spans.
    /// </para>
    /// <para>
    ///     Defaults to <see cref="Instrumenter.Sentry"/>
    /// </para>
    /// </summary>
    internal Instrumenter Instrumenter { get; set; } = Instrumenter.Sentry;

    /// <summary>
    /// Adds a <see cref="JsonConverter"/> to be used when serializing or deserializing
    /// objects to JSON with this SDK.  For example, when custom context data might use
    /// a data type that requires custom serialization logic.
    /// </summary>
    /// <param name="converter">The <see cref="JsonConverter"/> to add.</param>
    /// <remarks>
    /// This currently modifies a static list, so will affect any instance of the Sentry SDK.
    /// If that becomes problematic, we will have to refactor all serialization code to be
    /// able to accept an instance of <see cref="SentryOptions"/>.
    /// </remarks>
    public void AddJsonConverter(JsonConverter converter)
    {
        // protect against null because user may not have nullability annotations enabled
        if (converter == null!)
        {
            throw new ArgumentNullException(nameof(converter));
        }

        JsonExtensions.AddJsonConverter(converter);
    }

    /// <summary>
    /// Configures a custom <see cref="JsonSerializerContext"/> to be used when serializing or deserializing
    /// objects to JSON with this SDK.
    /// </summary>
    /// <param name="contextBuilder">
    /// A builder that takes <see cref="JsonSerializerOptions"/> and returns a <see cref="JsonSerializerContext"/>
    /// </param>
    /// <remarks>
    /// This currently modifies a static list, so will affect any instance of the Sentry SDK.
    /// If that becomes problematic, we will have to refactor all serialization code to be
    /// able to accept an instance of <see cref="SentryOptions"/>.
    /// </remarks>
    public void AddJsonSerializerContext<T>(Func<JsonSerializerOptions, T> contextBuilder)
        where T : JsonSerializerContext
    {
        // protect against null because user may not have nullability annotations enabled
        if (contextBuilder == null!)
        {
            throw new ArgumentNullException(nameof(contextBuilder));
        }

        JsonExtensions.AddJsonSerializerContext(contextBuilder);
    }

    /// <summary>
    /// When <c>true</c>, if an object being serialized to JSON contains references to other objects, and the
    /// serialized object graph exceed the maximum allowable depth, the object will instead be serialized using
    /// <see cref="ReferenceHandler.Preserve"/> (from System.Text.Json) - which adds <c>$id</c> and <c>$ref</c>
    /// metadata to the JSON.  When <c>false</c>, an object graph exceeding the maximum depth will be truncated.
    /// The default value is <c>true</c>.
    /// </summary>
    /// <remarks>
    /// This option applies only to complex objects being added to Sentry events as contexts or extras, which do not
    /// implement <see cref="ISentryJsonSerializable"/>.
    /// </remarks>
    public bool JsonPreserveReferences
    {
        get => JsonExtensions.JsonPreserveReferences;
        set => JsonExtensions.JsonPreserveReferences = value;
    }

    /// <summary>
    /// Provides a mechanism to convey network status to the caching transport, so that it does not attempt
    /// to send cached events to Sentry when the network is offline. Used internally by some integrations.
    /// Not intended for public usage.
    /// </summary>
    /// <remarks>
    /// This must be public because we use it in Sentry.Maui, which can't use InternalsVisibleTo
    /// because MAUI assemblies are not strong-named.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public INetworkStatusListener? NetworkStatusListener { get; set; }

    /// <summary>
    /// Allows integrations to provide a custom assembly reader.
    /// </summary>
    /// <remarks>
    /// This is for Sentry use only, and can change without a major version bump.
    /// </remarks>
#if !__MOBILE__
    [CLSCompliant(false)]
#endif
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Func<string, PEReader?>? AssemblyReader { get; set; }

    /// <summary>
    /// <para>
    /// Settings for the EXPERIMENTAL metrics feature. This feature is preview only and subject to change without a
    /// major version bump. Currently it's recommended for noodling only - DON'T USE IN PRODUCTION!
    /// </para>
    /// <para>
    /// By default the ExperimentalMetrics Options is null, which means the feature is disabled. If you want to enable
    /// Experimental metrics, you must set this property to a non-null value.
    /// </para>
    /// </summary>
    public ExperimentalMetricsOptions? ExperimentalMetrics { get; set; }

    /// <summary>
    /// The Spotlight URL. Defaults to http://localhost:8969/stream
    /// <see cref="EnableSpotlight"/>
    /// <see href="https://spotlightjs.com/"/>
    /// </summary>
    public string SpotlightUrl { get; set; } = "http://localhost:8969/stream";

    /// <summary>
    /// Whether to enable Spotlight for local development.
    /// </summary>
    /// <remarks>
    /// Only set this option to `true` while developing, not in production!
    /// </remarks>
    /// <see cref="SpotlightUrl"/>
    /// <see href="https://spotlightjs.com/"/>
    public bool EnableSpotlight { get; set; }

    internal SettingLocator SettingLocator { get; set; }

    /// <summary>
    /// Controls whether the native SDKs (Android, Cocoa, etc.) will be initialized (when applicable).
    /// Should be set <c>false</c> (disabled) only when testing, and then only if the test initializes the managed SDK.
    /// Defaults to <c>true</c> (enabled).
    /// </summary>
    internal bool InitNativeSdks { get; set; } = true;

    /// <summary>
    /// These callbacks are Called after Hub init and may be used, for example, to synchronize native contexts.
    /// This list is cleared after init to avoid duplicate invocations when the same Options object is used again.
    /// </summary>
    internal List<Action<IHub>> PostInitCallbacks { get; set; } = new();

    /// <summary>
    /// Creates a new instance of <see cref="SentryOptions"/>
    /// </summary>
    public SentryOptions()
    {
        SettingLocator = new SettingLocator(this);

        TransactionProcessorsProviders = new() {
            () => TransactionProcessors ?? Enumerable.Empty<ISentryTransactionProcessor>()
        };

        _clientReportRecorder = new Lazy<IClientReportRecorder>(() => new ClientReportRecorder(this));

        _sentryStackTraceFactory = new(() => new SentryStackTraceFactory(this));

        ISentryStackTraceFactory SentryStackTraceFactoryAccessor() => SentryStackTraceFactory;

        EventProcessors = new(){
            // De-dupe to be the first to run
            (typeof(DuplicateEventDetectionEventProcessor), new(() => new DuplicateEventDetectionEventProcessor(this))),
            (typeof(MainSentryEventProcessor), new(() => new MainSentryEventProcessor(this, SentryStackTraceFactoryAccessor))),
        };

        EventProcessorsProviders = new() {
            () => EventProcessors.Select(x => x.Item2.Value)
        };

        ExceptionProcessors = new(){
            ( typeof(MainExceptionProcessor), new(() => new MainExceptionProcessor(this, SentryStackTraceFactoryAccessor)) )
        };

        ExceptionProcessorsProviders = new() {
            () => ExceptionProcessors.Select(x => x.Item2.Value)
        };

        _integrations = new();

        _defaultIntegrations = DefaultIntegrations.AutoSessionTrackingIntegration |
                               DefaultIntegrations.AppDomainUnhandledExceptionIntegration |
                               DefaultIntegrations.AppDomainProcessExitIntegration |
                               DefaultIntegrations.AutoSessionTrackingIntegration |
                               DefaultIntegrations.UnobservedTaskExceptionIntegration
#if NETFRAMEWORK
                               | DefaultIntegrations.NetFxInstallationsIntegration
#endif
#if HAS_DIAGNOSTIC_INTEGRATION
                               | DefaultIntegrations.SentryDiagnosticListenerIntegration
#endif
#if NET5_0_OR_GREATER && !__MOBILE__
                               | DefaultIntegrations.WinUiUnhandledExceptionIntegration
#endif
#if NET8_0_OR_GREATER
                               | DefaultIntegrations.SystemDiagnosticsMetricsIntegration
#endif
                               ;

#if ANDROID
        Android = new AndroidOptions();
        Native = new NativeOptions(this);

        var reader = new Lazy<IAndroidAssemblyReader?>(() => AndroidHelpers.GetAndroidAssemblyReader(DiagnosticLogger));
        AssemblyReader = name => reader.Value?.TryReadAssembly(name);

#elif __IOS__
        Native = new NativeOptions(this);
#endif

        InAppExclude = new() {
                "System",
                "Mono",
                "Sentry",
                "Microsoft",
                "MS", // MS.Win32, MS.Internal, etc: Desktop apps
                "ABI.Microsoft", // MAUI
                "WinRT", // WinRT, UWP, WinUI
                "UIKit", // iOS / MacCatalyst
                "Newtonsoft.Json",
                "FSharp",
                "Serilog",
                "Giraffe",
                "NLog",
                "Npgsql",
                "RabbitMQ",
                "Hangfire",
                "IdentityServer4",
                "AWSSDK",
                "Polly",
                "Swashbuckle",
                "FluentValidation",
                "Autofac",
                "Stackexchange.Redis",
                "Dapper",
                "RestSharp",
                "SkiaSharp",
                "IdentityModel",
                "SqlitePclRaw",
                "Xamarin",
                "Android", // Ex: Android.Runtime.JNINativeWrapper...
                "Google",
                "MongoDB",
                "Remotion.Linq",
                "AutoMapper",
                "Nest",
                "Owin",
                "MediatR",
                "ICSharpCode",
                "Grpc",
                "ServiceStack"
        };

#if DEBUG
        InAppInclude = new()
        {
            "Sentry.Samples"
        };
#endif
        _sentryBaseUrl = new Lazy<string>(() =>
            new Uri(Dsn ?? string.Empty).GetComponents(
                UriComponents.SchemeAndServer,
                UriFormat.Unescaped)
        );

#if PLATFORM_NEUTRAL
        NetworkStatusListener = new PollingNetworkStatusListener(this);
#endif
    }

    /// <summary>
    /// Add an integration
    /// </summary>
    /// <param name="integration">The integration.</param>
    public void AddIntegration(ISdkIntegration integration)
    {
        _integrations.Add(integration);
    }

    /// <summary>
    /// Removes all integrations of type <typeparamref name="TIntegration"/>.
    /// </summary>
    /// <typeparam name="TIntegration">The type of the integration(s) to remove.</typeparam>
    public void RemoveIntegration<TIntegration>() where TIntegration : ISdkIntegration
    {
        // Note: Not removing default integrations
        _integrations.RemoveAll(integration => integration is TIntegration);
    }

    /// <summary>
    /// Add an exception filter.
    /// </summary>
    /// <param name="exceptionFilter">The exception filter to add.</param>
    public void AddExceptionFilter(IExceptionFilter exceptionFilter)
    {
        if (ExceptionFilters == null)
        {
            ExceptionFilters = new() { exceptionFilter };
        }
        else
        {
            ExceptionFilters.Add(exceptionFilter);
        }
    }

    /// <summary>
    /// Removes all filters of type <typeparamref name="TFilter"/>
    /// </summary>
    /// <typeparam name="TFilter">The type of filter(s) to remove.</typeparam>
    public void RemoveExceptionFilter<TFilter>() where TFilter : IExceptionFilter
        => ExceptionFilters?.RemoveAll(filter => filter is TFilter);

    /// <summary>
    /// Ignore exception of type <typeparamref name="TException"/> or derived.
    /// </summary>
    /// <typeparam name="TException">The type of the exception to ignore.</typeparam>
    public void AddExceptionFilterForType<TException>() where TException : Exception
        => AddExceptionFilter(new ExceptionTypeFilter<TException>());

    /// <summary>
    /// Add prefix to exclude from 'InApp' stacktrace list.
    /// </summary>
    /// <param name="prefix">The string used to filter the stacktrace to be excluded from InApp.</param>
    /// <remarks>
    /// Sentry by default filters the stacktrace to display only application code.
    /// A user can optionally click to see all which will include framework and libraries.
    /// A <see cref="string.StartsWith(string)"/> is executed
    /// </remarks>
    /// <example>
    /// 'System.', 'Microsoft.'
    /// </example>
    public void AddInAppExclude(string prefix)
    {
        if (InAppExclude == null)
        {
            InAppExclude = [prefix];
        }
        else
        {
            InAppExclude.Add(prefix);
        }
    }

    /// <summary>
    /// Add a regex to identify frames that are not 'InApp' in stacktraces. It will usually be more convenient to use
    /// the <see cref="AddInAppExcludeRegex"/> method, however you can use this overload if you want to control the
    /// <see cref="RegexOptions"/> used when constructing the Regular Expression. For performance reasons, it's
    /// recommend that you use <see cref="RegexOptions.Compiled"/> when creating the regex and unless you have specific
    /// reason consider using <see cref="RegexOptions.CultureInvariant"/> as well.
    /// </summary>
    /// <param name="regex">The regular expression to use to match stacktrace frames.</param>
    /// <remarks>
    /// Sentry by default filters the stacktrace to display only application code.
    /// A user can optionally click to see all which will include framework and libraries.
    /// If a frame has not already been determined to be InApp per any of the prefixes or
    /// patterns configured in <see cref="InAppInclude"/>, it will be considered InApp only
    /// if it does not match any of the prefixes or patterns in <see cref="InAppExclude"/>.
    /// </remarks>
    public void AddInAppExclude(Regex regex)
    {
        if (InAppExclude == null)
        {
            InAppExclude = [regex];
        }
        else
        {
            InAppExclude.Add(regex);
        }
    }

    /// <summary>
    /// Add a regex pattern used to identify frames that are not 'InApp' in stacktraces.
    /// </summary>
    /// <param name="pattern">The regular expression to use to match stacktrace frames</param>
    /// <remarks>
    /// Sentry by default filters the stacktrace to display only application code.
    /// A user can optionally click to see all which will include framework and libraries.
    /// If a frame has not already been determined to be InApp per any of the prefixes or
    /// patterns configured in <see cref="InAppInclude"/>, it will be considered InApp only
    /// if it does not match any of the prefixes or patterns in <see cref="InAppExclude"/>.
    /// </remarks>
    public void AddInAppExcludeRegex(string pattern) => AddInAppExclude(new Regex(pattern, DefaultRegexOptions));

    /// <summary>
    /// Add prefix to include as in 'InApp' stacktrace.
    /// </summary>
    /// <param name="prefix">The string used to filter the stacktrace to be included in InApp.</param>
    /// <remarks>
    /// Sentry by default filters the stacktrace to display only application code.
    /// A user can optionally click to see all which will include framework and libraries.
    /// A <see cref="string.StartsWith(string)"/> is executed
    /// </remarks>
    /// <example>
    /// 'System.CustomNamespace', 'Microsoft.Azure.App'
    /// </example>
    public void AddInAppInclude(string prefix)
    {
        if (InAppInclude == null)
        {
            InAppInclude = [prefix];
        }
        else
        {
            InAppInclude.Add(prefix);
        }
    }

    /// <summary>
    /// Add a regex pattern used to identify 'InApp' frames in stacktraces. It will usually be more convenient to use
    /// the <see cref="AddInAppIncludeRegex"/> method, however you can use this overload if you want to control the
    /// <see cref="RegexOptions"/> used when constructing the Regular Expression. For performance reasons, it's
    /// recommend that you use <see cref="RegexOptions.Compiled"/> when creating the regex and unless you have specific
    /// reason consider using <see cref="RegexOptions.CultureInvariant"/> as well.
    /// </summary>
    /// <param name="regex">The regular expression to use to match stacktrace frames.</param>
    /// <remarks>
    /// Sentry by default filters the stacktrace to display only application code.
    /// A user can optionally click to see all which will include framework and libraries.
    /// Frames from namespaces matching this regular expression will be considered "InApp"
    /// </remarks>
    public void AddInAppInclude(Regex regex)
    {
        if (InAppInclude == null)
        {
            InAppInclude = [regex];
        }
        else
        {
            InAppInclude.Add(regex);
        }
    }

    /// <summary>
    /// Add a regex pattern used to identify 'InApp' frames in stacktraces.
    /// </summary>
    /// <param name="pattern">A regular expression to use to match stacktrace frames.</param>
    /// <remarks>
    /// Sentry by default filters the stacktrace to display only application code.
    /// A user can optionally click to see all which will include framework and libraries.
    /// Frames from namespaces matching this regular expression will be considered "InApp"
    /// </remarks>
    public void AddInAppIncludeRegex(string pattern) => AddInAppInclude(new Regex(pattern, DefaultRegexOptions));

    /// <summary>
    /// Add an exception processor.
    /// </summary>
    /// <param name="processor">The exception processor.</param>
    public void AddExceptionProcessor(ISentryEventExceptionProcessor processor)
    {
        ExceptionProcessors.Add((processor.GetType(), new Lazy<ISentryEventExceptionProcessor>(() => processor)));
    }

    /// <summary>
    /// Add the exception processors.
    /// </summary>
    /// <param name="processors">The exception processors.</param>
    public void AddExceptionProcessors(IEnumerable<ISentryEventExceptionProcessor> processors)
    {
        foreach (var processor in processors)
        {
            AddExceptionProcessor(processor);
        }
    }

    /// <summary>
    /// Adds an event processor which is invoked when creating a <see cref="SentryEvent"/>.
    /// </summary>
    /// <param name="processor">The event processor.</param>
    public void AddEventProcessor(ISentryEventProcessor processor)
    {
        EventProcessors.Add((processor.GetType(), new Lazy<ISentryEventProcessor>(() => processor)));
    }

    /// <summary>
    /// Adds event processors which are invoked when creating a <see cref="SentryEvent"/>.
    /// </summary>
    /// <param name="processors">The event processors.</param>
    public void AddEventProcessors(IEnumerable<ISentryEventProcessor> processors)
    {
        foreach (var processor in processors)
        {
            AddEventProcessor(processor);
        }
    }

    /// <summary>
    /// Removes all event processors of type <typeparamref name="TProcessor"/>
    /// </summary>
    /// <typeparam name="TProcessor">The type of processor(s) to remove.</typeparam>
    public void RemoveEventProcessor<TProcessor>() where TProcessor : ISentryEventProcessor
        => EventProcessors.RemoveAll(processor => processor.Type == typeof(TProcessor));

    /// <summary>
    /// Adds an event processor provider which is invoked when creating a <see cref="SentryEvent"/>.
    /// </summary>
    /// <param name="processorProvider">The event processor provider.</param>
    public void AddEventProcessorProvider(Func<IEnumerable<ISentryEventProcessor>> processorProvider)
    {
        EventProcessorsProviders.Add(processorProvider);
    }

    /// <summary>
    /// Adds an transaction processor which is invoked when creating a <see cref="SentryTransaction"/>.
    /// </summary>
    /// <param name="processor">The transaction processor.</param>
    public void AddTransactionProcessor(ISentryTransactionProcessor processor)
    {
        if (TransactionProcessors == null)
        {
            TransactionProcessors = new() { processor };
        }
        else
        {
            TransactionProcessors.Add(processor);
        }
    }

    /// <summary>
    /// Adds transaction processors which are invoked when creating a <see cref="SentryTransaction"/>.
    /// </summary>
    /// <param name="processors">The transaction processors.</param>
    public void AddTransactionProcessors(IEnumerable<ISentryTransactionProcessor> processors)
    {
        if (TransactionProcessors == null)
        {
            TransactionProcessors = processors.ToList();
        }
        else
        {
            TransactionProcessors.AddRange(processors);
        }
    }

    /// <summary>
    /// Removes all transaction processors of type <typeparamref name="TProcessor"/>
    /// </summary>
    /// <typeparam name="TProcessor">The type of processor(s) to remove.</typeparam>
    public void RemoveTransactionProcessor<TProcessor>() where TProcessor : ISentryTransactionProcessor
        => TransactionProcessors?.RemoveAll(processor => processor is TProcessor);

    /// <summary>
    /// Adds an transaction processor provider which is invoked when creating a <see cref="SentryTransaction"/>.
    /// </summary>
    /// <param name="processorProvider">The transaction processor provider.</param>
    public void AddTransactionProcessorProvider(Func<IEnumerable<ISentryTransactionProcessor>> processorProvider)
        => TransactionProcessorsProviders = TransactionProcessorsProviders.Concat(new[] { processorProvider }).ToList();

    /// <summary>
    /// Add the exception processor provider.
    /// </summary>
    /// <param name="processorProvider">The exception processor provider.</param>
    public void AddExceptionProcessorProvider(Func<IEnumerable<ISentryEventExceptionProcessor>> processorProvider)
    {
        ExceptionProcessorsProviders.Add(processorProvider);
    }

    /// <summary>
    /// Invokes all event processor providers available.
    /// </summary>
    public IEnumerable<ISentryEventProcessor> GetAllEventProcessors()
        => EventProcessorsProviders.SelectMany(p => p());

    /// <summary>
    /// Invokes all transaction processor providers available.
    /// </summary>
    public IEnumerable<ISentryTransactionProcessor> GetAllTransactionProcessors()
        => TransactionProcessorsProviders.SelectMany(p => p());

    /// <summary>
    /// Invokes all exception processor providers available.
    /// </summary>
    public IEnumerable<ISentryEventExceptionProcessor> GetAllExceptionProcessors()
        => ExceptionProcessorsProviders.SelectMany(p => p());

    /// <summary>
    /// Use custom <see cref="ISentryStackTraceFactory" />.
    /// </summary>
    /// <param name="sentryStackTraceFactory">The stack trace factory.</param>
    public SentryOptions UseStackTraceFactory(ISentryStackTraceFactory sentryStackTraceFactory)
    {
        SentryStackTraceFactory = sentryStackTraceFactory ?? throw new ArgumentNullException(nameof(sentryStackTraceFactory));
        return this;
    }

    /// <summary>
    /// Applies the default tags to an event without resetting existing tags.
    /// </summary>
    /// <param name="hasTags">The event to apply the tags to.</param>
    public void ApplyDefaultTags(IHasTags hasTags)
    {
        foreach (var defaultTag in DefaultTags.Where(t => !hasTags.Tags.TryGetValue(t.Key, out _)))
        {
            hasTags.SetTag(defaultTag.Key, defaultTag.Value);
        }
    }

    /// <summary>
    /// Disables the strategy to detect duplicate events.
    /// </summary>
    /// <remarks>
    /// In case a second event is being sent out from the same exception, that event will be discarded.
    /// It is possible the second event had in fact more data. In which case it'd be ideal to avoid the first
    /// event going out in the first place.
    /// </remarks>
    public void DisableDuplicateEventDetection()
        => RemoveEventProcessor<DuplicateEventDetectionEventProcessor>();

    /// <summary>
    /// Disables the capture of errors through <see cref="AppDomain.UnhandledException"/>.
    /// </summary>
    public void DisableAppDomainUnhandledExceptionCapture() =>
        RemoveDefaultIntegration(DefaultIntegrations.AppDomainUnhandledExceptionIntegration);

#if HAS_DIAGNOSTIC_INTEGRATION
    /// <summary>
    /// Disables the integrations with Diagnostic source.
    /// </summary>
    public void DisableDiagnosticSourceIntegration()
        => RemoveDefaultIntegration(DefaultIntegrations.SentryDiagnosticListenerIntegration);
#endif

    /// <summary>
    /// Disables the capture of errors through <see cref="TaskScheduler.UnobservedTaskException"/>.
    /// </summary>
    public void DisableUnobservedTaskExceptionCapture() =>
        RemoveDefaultIntegration(DefaultIntegrations.UnobservedTaskExceptionIntegration);

#if NETFRAMEWORK
    /// <summary>
    /// Disables the list addition of .Net Frameworks into events.
    /// </summary>
    public void DisableNetFxInstallationsIntegration()
    {
        RemoveEventProcessor<NetFxInstallationsEventProcessor>();
        RemoveDefaultIntegration(DefaultIntegrations.NetFxInstallationsIntegration);
    }
#endif

    /// <summary>
    /// By default, any queued events (i.e: captures errors) are flushed on <see cref="AppDomain.ProcessExit"/>.
    /// This method disables that behaviour.
    /// </summary>
    public void DisableAppDomainProcessExitFlush() =>
        RemoveDefaultIntegration(DefaultIntegrations.AppDomainProcessExitIntegration);

#if NET5_0_OR_GREATER && !__MOBILE__
    /// <summary>
    /// Disables WinUI exception handler
    /// </summary>
    public void DisableWinUiUnhandledExceptionIntegration()
        => RemoveDefaultIntegration(DefaultIntegrations.WinUiUnhandledExceptionIntegration);
#endif

#if NET8_0_OR_GREATER
    /// <summary>
    /// Disables the System.Diagnostics.Metrics integration.
    /// </summary>
    public void DisableSystemDiagnosticsMetricsIntegration()
        => RemoveDefaultIntegration(DefaultIntegrations.SystemDiagnosticsMetricsIntegration);
#endif

    internal bool HasIntegration<TIntegration>() => _integrations.Any(integration => integration is TIntegration);

    internal void RemoveDefaultIntegration(DefaultIntegrations defaultIntegrations) => _defaultIntegrations &= ~defaultIntegrations;

    [Flags]
    internal enum DefaultIntegrations
    {
        AutoSessionTrackingIntegration = 1 << 0,
        AppDomainUnhandledExceptionIntegration = 1 << 1,
        AppDomainProcessExitIntegration = 1 << 2,
        UnobservedTaskExceptionIntegration = 1 << 3,
#if NETFRAMEWORK
        NetFxInstallationsIntegration = 1 << 4,
#endif
#if HAS_DIAGNOSTIC_INTEGRATION
        SentryDiagnosticListenerIntegration = 1 << 5,
#endif
#if NET5_0_OR_GREATER && !__MOBILE__
        WinUiUnhandledExceptionIntegration = 1 << 6,
#endif
#if NET8_0_OR_GREATER
        SystemDiagnosticsMetricsIntegration = 1 << 7,
#endif
    }

    internal void SetupLogging()
    {
        if (Debug)
        {
            if (DiagnosticLogger == null)
            {
                DiagnosticLogger = new ConsoleDiagnosticLogger(DiagnosticLevel);
                DiagnosticLogger.LogDebug("Logging enabled with ConsoleDiagnosticLogger and min level: {0}",
                    DiagnosticLevel);
            }

            if (SettingLocator.GetEnvironment().Equals("production", StringComparison.OrdinalIgnoreCase))
            {
                DiagnosticLogger.LogWarning("Sentry option 'Debug' is set to true while Environment is production. " +
                                            "Be aware this can cause performance degradation and is not advised. " +
                                            "See https://docs.sentry.io/platforms/dotnet/configuration/diagnostic-logger " +
                                            "for more information");
            }
        }
        else
        {
            DiagnosticLogger = null;
        }
    }

    internal string? TryGetDsnSpecificCacheDirectoryPath()
    {
        if (string.IsNullOrWhiteSpace(CacheDirectoryPath))
        {
            return null;
        }

        // DSN must be set to use caching
        if (string.IsNullOrWhiteSpace(Dsn))
        {
            return null;
        }

        return Path.Combine(CacheDirectoryPath, "Sentry", Dsn.GetHashString());
    }

    internal string? TryGetProcessSpecificCacheDirectoryPath()
    {
        // In the future, this will most likely contain process ID
        return TryGetDsnSpecificCacheDirectoryPath();
    }
}
