using Sentry.Extensibility;
using Sentry.Reflection;

namespace Sentry.Internal;

internal class MainSentryEventProcessor : ISentryEventProcessor
{
    internal const string CultureInfoKey = "Current Culture";
    internal const string CurrentUiCultureKey = "Current UI Culture";
    internal const string MemoryInfoKey = "Memory Info";
    internal const string ThreadPoolInfoKey = "ThreadPool Info";
    internal const string IsDynamicCodeKey = "Dynamic Code";
    internal const string IsDynamicCodeCompiledKey = "Compiled";
    internal const string IsDynamicCodeSupportedKey = "Supported";

    private readonly Enricher _enricher;

    private readonly SentryOptions _options;
    internal Func<ISentryStackTraceFactory> SentryStackTraceFactoryAccessor { get; }

    internal string? Release => _options.SettingLocator.GetRelease();

    internal string? Distribution => _options.Distribution;

    public MainSentryEventProcessor(
        SentryOptions options,
        Func<ISentryStackTraceFactory> sentryStackTraceFactoryAccessor)
    {
        _options = options;
        SentryStackTraceFactoryAccessor = sentryStackTraceFactoryAccessor;

        _enricher = new Enricher(options);
    }

    public SentryEvent Process(SentryEvent @event)
    {
        _options.LogDebug("Running main event processor on: Event {0}", @event.EventId);

        if (TimeZoneInfo.Local is { } timeZoneInfo)
        {
            @event.Contexts.Device.Timezone = timeZoneInfo;
        }

        IDictionary<string, string>? cultureInfoMapped = null;
        if (!@event.Contexts.ContainsKey(CultureInfoKey)
            && CultureInfoToDictionary(CultureInfo.CurrentCulture) is { } currentCultureMap)
        {
            cultureInfoMapped = currentCultureMap;
            @event.Contexts[CultureInfoKey] = currentCultureMap;
        }

        if (!@event.Contexts.ContainsKey(CurrentUiCultureKey)
            && CultureInfoToDictionary(CultureInfo.CurrentUICulture) is { } currentUiCultureMap
            && (cultureInfoMapped is null || currentUiCultureMap.Any(p => !cultureInfoMapped.Contains(p))))
        {
            @event.Contexts[CurrentUiCultureKey] = currentUiCultureMap;
        }

#if NETCOREAPP3_0_OR_GREATER
        @event.Contexts[IsDynamicCodeKey] = new Dictionary<string, bool>
        {
            { IsDynamicCodeCompiledKey, RuntimeFeature.IsDynamicCodeCompiled },
            { IsDynamicCodeSupportedKey, RuntimeFeature.IsDynamicCodeSupported }
        };
#endif

        AddMemoryInfo(@event.Contexts);
        AddThreadPoolInfo(@event.Contexts);
        if (@event.ServerName == null)
        {
            // Value set on the options take precedence over device name.
            if (!string.IsNullOrEmpty(_options.ServerName))
            {
                @event.ServerName = _options.ServerName;
            }
            else if (_options.SendDefaultPii)
            {
                @event.ServerName = Environment.MachineName;
            }
        }

        @event.Level ??= SentryLevel.Error;
        @event.Release ??= Release;
        @event.Distribution ??= Distribution;

        // if there's no exception with a stack trace, then get the current stack trace
        if (@event.Exception?.StackTrace is null)
        {
            var stackTrace = @event.SentryExceptions?.FirstOrDefault()?.Stacktrace
                            ?? SentryStackTraceFactoryAccessor().Create();
            if (stackTrace != null)
            {
                var currentThread = Thread.CurrentThread;
                var thread = new SentryThread
                {
                    Crashed = false,
                    Current = true,
                    Name = currentThread.Name,
                    Id = currentThread.ManagedThreadId,
                    Stacktrace = stackTrace
                };

                @event.SentryThreads = @event.SentryThreads?.Any() == true
                    ? new List<SentryThread>(@event.SentryThreads) { thread }
                    : new[] { thread }.AsEnumerable();

                if (stackTrace is DebugStackTrace debugStackTrace)
                {
                    debugStackTrace.MergeDebugImagesInto(@event);
                }
            }
        }

        // Add all the Debug Images that were referenced from stack traces to the Event.
        if (@event.SentryExceptions is { } sentryExceptions)
        {
            foreach (var sentryException in sentryExceptions)
            {
                if (sentryException.Stacktrace is DebugStackTrace debugStackTrace)
                {
                    debugStackTrace.MergeDebugImagesInto(@event);
                }
            }
        }

        if (_options.ReportAssembliesMode != ReportAssembliesMode.None)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }

                var asmName = assembly.GetName();
                if (asmName.Name is null)
                {
                    continue;
                }

                var asmVersion = _options.ReportAssembliesMode switch
                {
                    ReportAssembliesMode.Version => asmName.Version?.ToString() ?? string.Empty,
                    ReportAssembliesMode.InformationalVersion => assembly.GetVersion() ?? string.Empty,
                    _ => throw new ArgumentOutOfRangeException(
                        $"Report assemblies mode '{_options.ReportAssembliesMode}' is not yet supported")
                };

                if (!string.IsNullOrWhiteSpace(asmVersion))
                {
                    @event.Modules[asmName.Name] = asmVersion;
                }
            }
        }

        // Run enricher to fill in the gaps
        _enricher.Apply(@event);

        return @event;
    }

    private static void AddMemoryInfo(SentryContexts contexts)
    {
#if NETCOREAPP3_0_OR_GREATER
        var memory = GC.GetGCMemoryInfo();
        var allocatedBytes = GC.GetTotalAllocatedBytes();
#if NET5_0_OR_GREATER
        contexts[MemoryInfoKey] = new MemoryInfo(
            allocatedBytes,
            memory.FragmentedBytes,
            memory.HeapSizeBytes,
            memory.HighMemoryLoadThresholdBytes,
            memory.TotalAvailableMemoryBytes,
            memory.MemoryLoadBytes,
            memory.TotalCommittedBytes,
            memory.PromotedBytes,
            memory.PinnedObjectsCount,
            memory.PauseTimePercentage,
            memory.Index,
            memory.FinalizationPendingCount,
            memory.Compacted,
            memory.Concurrent,
            memory.PauseDurations.ToArray());
#else
            contexts[MemoryInfoKey] = new MemoryInfo(
            allocatedBytes,
            memory.FragmentedBytes,
            memory.HeapSizeBytes,
            memory.HighMemoryLoadThresholdBytes,
            memory.TotalAvailableMemoryBytes,
            memory.MemoryLoadBytes);
#endif
#endif
    }

    private static void AddThreadPoolInfo(SentryContexts contexts)
    {
        ThreadPool.GetMinThreads(out var minWorkerThreads, out var minCompletionPortThreads);
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
        ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableCompletionPortThreads);
        contexts[ThreadPoolInfoKey] = new ThreadPoolInfo(
            minWorkerThreads,
            minCompletionPortThreads,
            maxWorkerThreads,
            maxCompletionPortThreads,
            availableWorkerThreads,
            availableCompletionPortThreads);
    }

    private static IDictionary<string, string>? CultureInfoToDictionary(CultureInfo cultureInfo)
    {
        var dic = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(cultureInfo.Name))
        {
            dic.Add("name", cultureInfo.Name);
        }
        if (!string.IsNullOrWhiteSpace(cultureInfo.DisplayName))
        {
            dic.Add("display_name", cultureInfo.DisplayName);
        }
        if (cultureInfo.Calendar is { } cal)
        {
            dic.Add("calendar", cal.GetType().Name);
        }

        return dic.Count > 0 ? dic : null;
    }
}
