namespace Sentry.NLog;

/// <summary>
/// Sentry NLog Target.
/// </summary>
[Target("Sentry")]
public sealed class SentryTarget : TargetWithContext
{
    // For testing:
    internal Func<IHub> HubAccessor { get; }

    private readonly ISystemClock _clock;
    private IDisposable? _sdkDisposable;

    internal static readonly SdkVersion NameAndVersion = typeof(SentryTarget).Assembly.GetNameAndVersion();

    internal static readonly string AdditionalGroupingKeyProperty = "AdditionalGroupingKey";

    private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;

    /// <summary>
    /// Creates a new instance of <see cref="SentryTarget"/>.
    /// </summary>
    public SentryTarget() : this(new SentryNLogOptions())
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="SentryTarget"/>.
    /// </summary>
    public SentryTarget(SentryNLogOptions options)
        : this(
            options,
            () => HubAdapter.Instance,
            null,
            SystemClock.Clock)
    {
    }

    internal SentryTarget(SentryNLogOptions options, Func<IHub> hubAccessor, IDisposable? sdkInstance, ISystemClock clock)
    {
        Options = options;
        HubAccessor = hubAccessor;
        _clock = clock;

        // Overrides default layout. Still will be explicitly overwritten if manually configured in the
        // NLog.config file.
        Layout = "${message}";
        BreadcrumbCategory = Options.BreadcrumbCategoryLayout ?? "${logger}";
        IncludeEventProperties = true;

        if (sdkInstance != null)
        {
            _sdkDisposable = sdkInstance;
        }
    }

    /// <summary>
    /// Options for both the <see cref="SentryTarget"/> and the sentry sdk itself.
    /// </summary>
    public SentryNLogOptions Options { get; }

    /// <summary>
    /// Add any desired additional tags that will be sent with every message.
    /// </summary>
    [ArrayParameter(typeof(TargetPropertyWithContext), "tag")]
    public IList<TargetPropertyWithContext> Tags => Options.Tags;

    /// <summary>
    /// Configured layout for Data Source Name of a given project in Sentry
    /// </summary>
    public Layout? Dsn
    {
        get => Options.DsnLayout;
        set => Options.DsnLayout = value;
    }

    /// <summary>
    /// Configured layout for application Release version to Sentry
    /// </summary>
    public Layout? Release
    {
        get => Options.ReleaseLayout;
        set => Options.ReleaseLayout = value;
    }

    /// <summary>
    /// Configured layout for application Environment to Sentry
    /// </summary>
    public Layout? Environment
    {
        get => Options.EnvironmentLayout;
        set => Options.EnvironmentLayout = value;
    }

    /// <summary>
    /// An optional layout specific to breadcrumbs. If not set, uses the same layout as the standard <see cref="TargetWithContext.Layout"/>.
    /// </summary>
    public Layout? BreadcrumbLayout
    {
        get => Options.BreadcrumbLayout ?? Layout;
        set => Options.BreadcrumbLayout = value;
    }

    /// <summary>
    /// Configured layout for application Environment to Sentry
    /// </summary>
    public Layout? BreadcrumbCategory
    {
        get => Options.BreadcrumbCategoryLayout;
        set => Options.BreadcrumbCategoryLayout = value;
    }

    /// <summary>
    /// Minimum log level for events to trigger a send to Sentry. Defaults to <see cref="M:LogLevel.Error" />.
    /// </summary>
    public string MinimumEventLevel
    {
        get => Options.MinimumEventLevel?.ToString() ?? LogLevel.Off.ToString();
        set => Options.MinimumEventLevel = LogLevel.FromString(value);
    }

    /// <summary>
    /// Minimum log level to be included in the breadcrumb. Defaults to <see cref="M:LogLevel.Info" />.
    /// </summary>
    public string MinimumBreadcrumbLevel
    {
        get => Options.MinimumBreadcrumbLevel?.ToString() ?? LogLevel.Off.ToString();
        set => Options.MinimumBreadcrumbLevel = LogLevel.FromString(value);
    }

    /// <summary>
    /// Whether the NLog integration should initialize the SDK.
    /// </summary>
    /// <remarks>
    /// By default, if a DSN is provided to the NLog integration it will initialize the SDK.
    /// This might be not ideal when using multiple integrations in case you want another one doing the Init.
    /// </remarks>
    public bool InitializeSdk
    {
        get => Options.InitializeSdk;
        set => Options.InitializeSdk = value;
    }

    /// <summary>
    /// Set this to <see langword="true" /> to ignore log messages that don't contain an exception.
    /// </summary>
    public bool IgnoreEventsWithNoException
    {
        get => Options.IgnoreEventsWithNoException;
        set => Options.IgnoreEventsWithNoException = value;
    }

    /// <summary>
    /// Determines whether event properties will be sent to sentry as Tags or not.
    /// Defaults to <see langword="false" />.
    /// </summary>
    public bool IncludeEventPropertiesAsTags
    {
        get => Options.IncludeEventPropertiesAsTags;
        set => Options.IncludeEventPropertiesAsTags = value;
    }

    /// <summary>
    /// Determines whether or not to include event-level data as data in breadcrumbs for future errors.
    /// Defaults to <see langword="false" />.
    /// </summary>
    public bool IncludeEventDataOnBreadcrumbs
    {
        get => Options.IncludeEventDataOnBreadcrumbs;
        set => Options.IncludeEventDataOnBreadcrumbs = value;
    }

    /// <summary>
    /// How many seconds to wait after triggering <see cref="LogManager.Shutdown()"/> before just shutting down the
    /// Sentry sdk.
    /// </summary>
    public int ShutdownTimeoutSeconds
    {
        get => Options.ShutdownTimeoutSeconds;
        set => Options.ShutdownTimeoutSeconds = value;
    }

    /// <summary>
    /// How long to wait for the flush to finish, in seconds. Defaults to 2 seconds.
    /// </summary>
    public int FlushTimeoutSeconds
    {
        get => (int)Options.FlushTimeout.TotalSeconds;
        set => Options.FlushTimeout = TimeSpan.FromSeconds(value);
    }

    /// <summary>
    /// Optionally configure one or more parts of the user information to be rendered dynamically from an NLog layout
    /// </summary>
    public SentryNLogUser? User
    {
        get => Options.User;
        set => Options.User = value;
    }

    /// <inheritdoc />
    protected override void CloseTarget()
    {
        _sdkDisposable?.Dispose();
        base.CloseTarget();
    }

    /// <inheritdoc />
    protected override void InitializeTarget()
    {
        // If a layout has been configured on the options, replace the default logger.
        if (Options.Layout != null)
        {
            Layout = Options.Layout;
        }

        base.InitializeTarget();

        if (InternalLogger.IsDebugEnabled || InternalLogger.IsInfoEnabled || InternalLogger.IsWarnEnabled || InternalLogger.IsErrorEnabled || InternalLogger.IsFatalEnabled)
        {
            var existingLogger = Options.DiagnosticLogger;
            if (existingLogger is not NLogDiagnosticLogger)
            {
                Options.DiagnosticLogger = new NLogDiagnosticLogger(existingLogger);
            }
            Options.Debug = true;
        }

        var customDsn = Dsn?.Render(LogEventInfo.CreateNullEvent());
        if (!string.IsNullOrEmpty(customDsn))
        {
            Options.Dsn = customDsn;
        }

        var customRelease = Release?.Render(LogEventInfo.CreateNullEvent());
        if (!string.IsNullOrEmpty(customRelease))
        {
            Options.Release = customRelease;
        }

        var customEnvironment = Environment?.Render(LogEventInfo.CreateNullEvent());
        if (!string.IsNullOrEmpty(customEnvironment))
        {
            Options.Environment = customEnvironment;
        }

        // If the sdk is not there, set it on up.
        if (InitializeSdk && _sdkDisposable == null)
        {
            _sdkDisposable = SentrySdk.Init(Options);
        }

        if (!HubAccessor().IsEnabled)
        {
            InternalLogger.Info("Sentry(Name={0}): Hub not enabled", Name);
        }
    }

    /// <inheritdoc />
    protected override void FlushAsync(AsyncContinuation asyncContinuation)
    {
        _ = HubAccessor()
            .FlushAsync(Options.FlushTimeout)
            .ContinueWith(t => asyncContinuation(t.Exception));
    }
    static AsyncLocal<bool> isReentrant = new();
    /// <summary>
    /// <para>
    /// If the event level &gt;= the <see cref="MinimumEventLevel"/>, the
    /// <paramref name="logEvent"/> is captured as an event by sentry.
    /// </para>
    /// <para>
    /// If the event level is &gt;= the <see cref="MinimumBreadcrumbLevel"/>, the event is added
    /// as a breadcrumb to the Sentry Sdk.
    /// </para>
    /// <para>
    /// If sentry is not enabled, this is a No-op.
    /// </para>
    /// </summary>
    /// <param name="logEvent">The event that is being logged.</param>
    /// <inheritdoc />
    protected override void Write(LogEventInfo logEvent)
    {
        //TODO: check if can really be null
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (logEvent == null)
        {
            return;
        }

        if (isReentrant.Value)
        {
            Options.DiagnosticLogger?.LogError($"Reentrant log event detected. Logging when inside the scope of another log event can cause a StackOverflowException. LogEventInfo.Message:{logEvent.Message}");
            return;
        }

        isReentrant.Value = true;

        try
        {
            InnerWrite(logEvent);
        }
        catch (Exception exception)
        {
            Options.DiagnosticLogger?.LogError(exception, "Failed to write log event");
            throw;
        }
        finally
        {
            isReentrant.Value = false;
        }
    }

    private void InnerWrite(LogEventInfo logEvent)
    {
        if (logEvent.Message == null)
        {
            return;
        }

        var hub = HubAccessor();

        if (!hub.IsEnabled)
        {
            return;
        }

        var exception = logEvent.Exception;
        var shouldOnlyLogExceptions = exception == null && IgnoreEventsWithNoException;
        var shouldIncludeProperties = ContextProperties?.Count > 0 || ShouldIncludeProperties(logEvent);

        if (logEvent.Level >= Options.MinimumEventLevel && !shouldOnlyLogExceptions)
        {
            var formatted = RenderLogEvent(Layout, logEvent);
            var template = logEvent.Message;

            var evt = new SentryEvent(exception)
            {
                Message = new SentryMessage
                {
                    Formatted = formatted,
                    Message = template
                },
                Logger = logEvent.LoggerName,
                Level = logEvent.Level.ToSentryLevel(),
                Release = Options.Release,
                Environment = Options.Environment,
                User = GetUser(logEvent) ?? new User(),
            };

            if (evt.Sdk is { } sdk)
            {
                sdk.Name = Constants.SdkName;
                sdk.Version = NameAndVersion.Version;

                if (NameAndVersion.Version is { } version)
                {
                    sdk.AddPackage(ProtocolPackageName, version);
                }
            }

            if (Tags.Count > 0 || IncludeEventPropertiesAsTags && logEvent.HasProperties)
            {
                evt.SetTags(GetTagsFromLogEvent(logEvent));
            }

            if (shouldIncludeProperties)
            {
                var contextProps = GetAllProperties(logEvent);
                evt.SetExtras(contextProps);

                if (contextProps.TryGetValue(AdditionalGroupingKeyProperty, out var additionalGroupingKey)
                    && additionalGroupingKey is string groupingKey)
                {
                    var overridenFingerprint = evt.Fingerprint.ToList();
                    if (!evt.Fingerprint.Any())
                    {
                        overridenFingerprint.Add("{{ default }}");
                    }

                    overridenFingerprint.Add(groupingKey);

                    evt.SetFingerprint(overridenFingerprint);
                }
            }

            _ = hub.CaptureEvent(evt);
        }

        // Whether or not it was sent as event, add breadcrumb so the next event includes it
        if (logEvent.Level >= Options.MinimumBreadcrumbLevel)
        {
            var breadcrumbFormatted = RenderLogEvent(BreadcrumbLayout, logEvent);
            var breadcrumbCategory = RenderLogEvent(BreadcrumbCategory, logEvent);
            if (string.IsNullOrEmpty(breadcrumbCategory))
            {
                breadcrumbCategory = null;
            }

            var message = string.IsNullOrWhiteSpace(breadcrumbFormatted)
                ? exception?.Message ?? logEvent.FormattedMessage
                : breadcrumbFormatted;

            IDictionary<string, string>? data = null;
            // If this is true, an exception is being logged with no custom message
            if (exception?.Message != null && !message.StartsWith(exception.Message))
            {
                // Exception won't be used as Breadcrumb message. Avoid losing it by adding as data:
                data = new Dictionary<string, string>
                {
                    {"exception_type", exception.GetType().ToString()},
                    {"exception_message", exception.Message},
                };
            }

            if (IncludeEventDataOnBreadcrumbs)
            {
                if (shouldIncludeProperties)
                {
                    var contextProps = GetAllProperties(logEvent);
                    data ??= new Dictionary<string, string>(contextProps.Count);
                    foreach (var contextProp in contextProps)
                    {
                        if (contextProp.Value?.ToString() is { } value)
                        {
                            data.Add(contextProp.Key, value);
                        }
                    }
                }
            }

            hub.AddBreadcrumb(
                _clock,
                message,
                breadcrumbCategory,
                data: data,
                level: logEvent.Level.ToBreadcrumbLevel());
        }
    }

    private User? GetUser(LogEventInfo logEvent)
    {
        if (User is null)
        {
            return null;
        }

        var user = new User
        {
            Id = User.Id?.Render(logEvent),
            Username = User.Username?.Render(logEvent),
            Email = User.Email?.Render(logEvent),
            IpAddress = User.IpAddress?.Render(logEvent),
            Segment = User.Segment?.Render(logEvent)
        };

        if (User.Other?.Count > 0)
        {
            user.Other = new Dictionary<string, string>(User.Other.Count);
            for (var i = 0; i < User.Other.Count; ++i)
            {
                if (User.Other[i].Layout?.Render(logEvent) is { } value)
                {
                    if (!string.IsNullOrEmpty(value) || User.Other[i].IncludeEmptyValue)
                    {
                        user.Other[User.Other[i].Name] = value;
                    }
                }
            }
        }

        return user;
    }

    private IEnumerable<KeyValuePair<string, string>> GetTagsFromLogEvent(LogEventInfo logEvent)
    {
        if (IncludeEventPropertiesAsTags)
        {
            if (logEvent.HasProperties)
            {
                foreach (var kv in logEvent.Properties)
                {
                    if (kv.Value?.ToString() is { } value)
                    {
                        yield return new KeyValuePair<string, string>(kv.Key?.ToString() ?? "", value);
                    }
                }
            }
        }

        if (Tags.Count > 0)
        {
            foreach (var tag in Tags)
            {
                var tagValue = RenderLogEvent(tag.Layout, logEvent);
                if (!tag.IncludeEmptyValue && string.IsNullOrEmpty(tagValue))
                {
                    continue;
                }

                yield return new KeyValuePair<string, string>(tag.Name, tagValue);
            }
        }
    }
}
