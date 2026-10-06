namespace Sentry.NLog;

/// <summary>
/// Sentry NLog Target.
/// </summary>
[Target("Sentry")]
public sealed partial class SentryTarget : TargetWithContext
{
    // For testing:
    internal Func<IHub> HubAccessor { get; }

    private readonly ISystemClock _clock;
    private readonly UninitializedSdkWarning _uninitializedSdkWarning;
    private bool _reportedUnsupportedSdkSetting;

    internal static readonly string AdditionalGroupingKeyProperty = "AdditionalGroupingKey";

    internal const string UninitializedSdkMessage =
        "Sentry: the Sentry target for NLog dropped a log event because Sentry is not initialized, but a DSN " +
        "was found in the environment or in an assembly attribute. The target no longer initializes the SDK: " +
        "call SentrySdk.Init (or UseSentry via one of the integrations) at startup. " +
        "See https://docs.sentry.io/platforms/dotnet/guides/nlog/";

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
            SystemClock.Clock)
    {
    }

    internal SentryTarget(
        SentryNLogOptions options,
        Func<IHub> hubAccessor,
        ISystemClock clock,
        UninitializedSdkWarning? uninitializedSdkWarning = null)
    {
        Options = options;
        HubAccessor = hubAccessor;
        _clock = clock;
        _uninitializedSdkWarning = uninitializedSdkWarning
                                   ?? new UninitializedSdkWarning(message => InternalLogger.Warn("{0}", message));

        // Overrides default layout. Still will be explicitly overwritten if manually configured in the
        // NLog.config file.
        Layout = "${message}";
        BreadcrumbCategory = Options.BreadcrumbCategoryLayout ?? "${logger}";
        IncludeEventProperties = true;
    }

    /// <summary>
    /// Options for the <see cref="SentryTarget"/>.
    /// </summary>
    public SentryNLogOptions Options { get; }

    /// <summary>
    /// Add any desired additional tags that will be sent with every message.
    /// </summary>
    [ArrayParameter(typeof(TargetPropertyWithContext), "tag")]
    public IList<TargetPropertyWithContext> Tags => Options.Tags;

    /// <summary>
    /// Not supported. The Sentry target no longer initializes the SDK.
    /// </summary>
    /// <exception cref="NotSupportedException">When set.</exception>
    [Obsolete(ConfigurationExtensions.ObsoleteDsnOverload, error: true)]
    public Layout? Dsn
    {
        get => null;
        set => throw ReportUnsupportedSdkSetting();
    }

    /// <summary>
    /// Not supported. The Sentry target no longer initializes the SDK.
    /// </summary>
    /// <exception cref="NotSupportedException">When set.</exception>
    [Obsolete(ConfigurationExtensions.ObsoleteDsnOverload, error: true)]
    public bool InitializeSdk
    {
        get => false;
        set
        {
            if (value)
            {
                throw ReportUnsupportedSdkSetting();
            }
        }
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

    internal const string ObsoleteEnableLogs =
        "The Sentry target always sends logs. This option is ignored and will be removed in a future major version. " +
        "To drop logs, use SetBeforeSendLog on the options passed to SentrySdk.Init and return null.";

    internal const string EnableLogsFalseIgnored =
        "The Sentry target always sends logs, so enableLogs=\"false\" no longer turns them off. " +
        "Remove enableLogs from the target configuration. " +
        "To drop logs, use SetBeforeSendLog on the options passed to SentrySdk.Init and return null.";

    /// <summary>
    /// Logs are always generated and sent.
    /// </summary>
    /// <remarks>
    /// This option no longer has any effect. The getter always returns <see langword="true"/>.
    /// Setting it to <see langword="false"/> only writes a warning to standard error.
    /// To filter or drop logs, use <see cref="SentryOptions.SetBeforeSendLog(Func{SentryLog, SentryLog})"/> and return <see langword="null"/>.
    /// </remarks>
    [Obsolete(ObsoleteEnableLogs)]
    public bool EnableLogs
    {
        get => true;
        set
        {
            if (!value)
            {
                _uninitializedSdkWarning.WriteToStandardError("Sentry: " + EnableLogsFalseIgnored);
            }
        }
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
    /// Optionally configure one or more parts of the user information to be rendered dynamically from an NLog layout
    /// </summary>
    public SentryNLogUser? User
    {
        get => Options.User;
        set => Options.User = value;
    }

    // NLog discards the exception unless throwConfigExceptions is on. Report once: a v6 config may set both.
    private NotSupportedException ReportUnsupportedSdkSetting()
    {
        if (!_reportedUnsupportedSdkSetting)
        {
            _reportedUnsupportedSdkSetting = true;
            _uninitializedSdkWarning.WriteToStandardError("Sentry: " + ConfigurationExtensions.ObsoleteDsnOverload);
        }

        return new NotSupportedException(ConfigurationExtensions.ObsoleteDsnOverload);
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

        if (!HubAccessor().IsEnabled)
        {
            InternalLogger.Info("Sentry(Name={0}): Hub not enabled", Name);
        }
    }

    /// <inheritdoc />
    protected override void FlushAsync(AsyncContinuation asyncContinuation)
    {
        _ = HubAccessor()
            .FlushAsync()
            .ContinueWith(t => asyncContinuation(t.Exception));
    }

    private static AsyncLocal<bool> isReentrant = new();
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
            HubAccessor()?.GetSentryOptions()?.DiagnosticLogger?.LogError($"Reentrant log event detected. Logging when inside the scope of another log event can cause a StackOverflowException. LogEventInfo.Message:{logEvent.Message}");
            return;
        }

        isReentrant.Value = true;

        try
        {
            InnerWrite(logEvent);
        }
        catch (Exception exception)
        {
            HubAccessor()?.GetSentryOptions()?.DiagnosticLogger?.LogError(exception, "Failed to write log event");
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
            if (logEvent.Level >= Options.MinimumEventLevel)
            {
                _uninitializedSdkWarning.WarnOnce(UninitializedSdkMessage);
            }

            return;
        }

        var exception = logEvent.Exception;
        var shouldIgnoreEvent = exception == null && IgnoreEventsWithNoException;
        var shouldIncludeProperties = ContextProperties?.Count > 0 || ShouldIncludeProperties(logEvent);
        var addedBreadcrumbForException = false;

        if (logEvent.Level >= Options.MinimumEventLevel && !shouldIgnoreEvent)
        {
            CreateSentryEvent(logEvent, exception, shouldIncludeProperties, hub);

            // Capturing exception events adds a breadcrumb automatically... we don't want to add another one
            if (exception != null)
            {
                addedBreadcrumbForException = true;
            }
        }

        // Whether or not it was sent as an event, add breadcrumb so the next event includes it
        if (!addedBreadcrumbForException && logEvent.Level >= Options.MinimumBreadcrumbLevel)
        {
            CreateBreadcrumb(logEvent, exception, shouldIncludeProperties, hub);
        }

        var sentryOptions = hub.GetSentryOptions();
        if (sentryOptions is not null)
        {
            try
            {
                CaptureStructuredLog(hub, sentryOptions, logEvent);
            }
            catch (Exception ex)
            {
                sentryOptions.DiagnosticLogger?.LogError(ex, "Failed to capture structured log. The log will be dropped.");
            }
        }
    }

    private void CreateBreadcrumb(LogEventInfo logEvent, Exception? exception, bool shouldIncludeProperties, IHub hub)
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
            type: null,
            data: data,
            level: logEvent.Level.ToBreadcrumbLevel(),
            hint: exception.ToHint());
    }

    private void CreateSentryEvent(LogEventInfo logEvent, Exception? exception, bool shouldIncludeProperties, IHub hub)
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
            User = GetUser(logEvent) ?? new SentryUser(),
        };

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

    private SentryUser? GetUser(LogEventInfo logEvent)
    {
        if (User is null)
        {
            return null;
        }

        var user = new SentryUser
        {
            Id = User.Id?.Render(logEvent),
            Username = User.Username?.Render(logEvent),
            Email = User.Email?.Render(logEvent),
            IpAddress = User.IpAddress?.Render(logEvent),
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
