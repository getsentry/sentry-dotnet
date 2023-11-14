namespace Sentry;

/// <summary>
/// Contains representations of the subset of properties in SentryOptions that can be set from ConfigurationBindings.
/// Note that all of these properties are nullable, so that if they are not present in configuration, the values from
/// the type being bound to will be preserved.
/// </summary>
internal class BindableSentryOptions
{
    public bool? IsGlobalModeEnabled { get; set; }
    public bool? EnableScopeSync { get; set; }
    // public List<SubstringOrRegexPattern>? TagFilters { get; set; }
    public bool? SendDefaultPii { get; set; }
    public bool? IsEnvironmentUser { get; set; }
    public string? ServerName { get; set; }
    public bool? AttachStacktrace { get; set; }
    public int? MaxBreadcrumbs { get; set; }
    public float? SampleRate { get; set; }
    public string? Release { get; set; }
    public string? Distribution { get; set; }
    public string? Environment { get; set; }
    public string? Dsn { get; set; }
    public int? MaxQueueItems { get; set; }
    public int? MaxCacheItems { get; set; }
    //public TimeSpan? ShutdownTimeout { get; set; }
    //public TimeSpan? FlushTimeout { get; set; }
    public DecompressionMethods? DecompressionMethods { get; set; }
    public CompressionLevel? RequestBodyCompressionLevel { get; set; }
    public bool? RequestBodyCompressionBuffered { get; set; }
    public bool? SendClientReports { get; set; }
    public bool? Debug { get; set; }
    public SentryLevel? DiagnosticLevel { get; set; }
    // public bool? ReportAssemblies { get; set; }
    public ReportAssembliesMode? ReportAssembliesMode { get; set; }
    public DeduplicateMode? DeduplicateMode { get; set; }
    public string? CacheDirectoryPath { get; set; }
    public bool? CaptureFailedRequests { get; set; }
    //public List<HttpStatusCodeRange>? FailedRequestStatusCodes { get; set; }
    //public List<SubstringOrRegexPattern>? FailedRequestTargets { get; set; }
    //public TimeSpan? InitCacheFlushTimeout { get; set; }
    // public Dictionary<string,string> DefaultTags { get; set; }
    public bool? EnableTracing { get; set; }
    public double? TracesSampleRate { get; set; }
    // public List<SubstringOrRegexPattern>? TracePropagationTargets { get; set; }
    public StackTraceMode? StackTraceMode { get; set; }
    public long? MaxAttachmentSize { get; set; }
    public StartupTimeDetectionMode? DetectStartupTime { get; set; }
    // public TimeSpan? AutoSessionTrackingInterval { get; set; }
    public bool? AutoSessionTracking { get; set; }
    public bool? UseAsyncFileIO { get; set; }
    // public bool? KeepAggregateException { get; set; }
    public bool? JsonPreserveReferences { get; set; }

    public void ApplyTo(SentryOptions options)
    {
        options.Dsn = Dsn ?? options.Dsn;
        options.Environment = Environment ?? options.Environment;
        options.EnableScopeSync = EnableScopeSync ?? options.EnableScopeSync;
        // TODO: all the other options...
        options.IsGlobalModeEnabled = IsGlobalModeEnabled ?? options.IsGlobalModeEnabled;
        options.EnableScopeSync = EnableScopeSync ?? options.EnableScopeSync;
        // Add assertion for TagFilters here if needed
        options.SendDefaultPii = SendDefaultPii ?? options.SendDefaultPii;
        options.IsEnvironmentUser = IsEnvironmentUser ?? options.IsEnvironmentUser;
        options.ServerName = ServerName ?? options.ServerName;
        options.AttachStacktrace = AttachStacktrace ?? options.AttachStacktrace;
        options.MaxBreadcrumbs = MaxBreadcrumbs ?? options.MaxBreadcrumbs;
        options.SampleRate = SampleRate ?? options.SampleRate;
        options.Release = Release ?? options.Release;
        options.Distribution = Distribution ?? options.Distribution;
        options.Environment = Environment ?? options.Environment;
        options.Dsn = Dsn ?? options.Dsn;
        options.MaxQueueItems = MaxQueueItems ?? options.MaxQueueItems;
        options.MaxCacheItems = MaxCacheItems ?? options.MaxCacheItems;
        // Add assertion for ShutdownTimeout here if needed
        // Add assertion for FlushTimeout here if needed
        options.DecompressionMethods = DecompressionMethods ?? options.DecompressionMethods;
        options.RequestBodyCompressionLevel = RequestBodyCompressionLevel ?? options.RequestBodyCompressionLevel;
        options.RequestBodyCompressionBuffered = RequestBodyCompressionBuffered ?? options.RequestBodyCompressionBuffered;
        options.SendClientReports = SendClientReports ?? options.SendClientReports;
        options.Debug = Debug ?? options.Debug;
        options.DiagnosticLevel = DiagnosticLevel ?? options.DiagnosticLevel;
        options.ReportAssembliesMode = ReportAssembliesMode ?? options.ReportAssembliesMode;
        options.DeduplicateMode = DeduplicateMode ?? options.DeduplicateMode;
        options.CacheDirectoryPath = CacheDirectoryPath ?? options.CacheDirectoryPath;
        options.CaptureFailedRequests = CaptureFailedRequests ?? options.CaptureFailedRequests;
        // Add assertion for FailedRequestStatusCodes here if needed
        // Add assertion for FailedRequestTargets here if needed
        // Add assertion for InitCacheFlushTimeout here if needed
        // Add assertion for DefaultTags here if needed
        options.EnableTracing = EnableTracing ?? options.EnableTracing;
        options.TracesSampleRate = TracesSampleRate ?? options.TracesSampleRate;
        // Add assertion for TracePropagationTargets here if needed
        options.StackTraceMode = StackTraceMode ?? options.StackTraceMode;
        options.MaxAttachmentSize = MaxAttachmentSize ?? options.MaxAttachmentSize;
        options.DetectStartupTime = DetectStartupTime ?? options.DetectStartupTime;
        // Add assertion for AutoSessionTrackingInterval here if needed
        options.AutoSessionTracking = AutoSessionTracking ?? options.AutoSessionTracking;
        options.UseAsyncFileIO = UseAsyncFileIO ?? options.UseAsyncFileIO;
        options.JsonPreserveReferences = JsonPreserveReferences ?? options.JsonPreserveReferences;
    }
}
