using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Sentry.Extensibility;
using Sentry.Reflection;

namespace Sentry.Internal
{
    internal class MainSentryEventProcessor : ISentryEventProcessor
    {
        internal const string CultureInfoKey = "Current Culture";
        internal const string CurrentUiCultureKey = "Current UI Culture";
        internal const string MemoryInfoKey = "Memory Info";

        private readonly Enricher _enricher;

        private readonly SentryOptions _options;
        internal Func<ISentryStackTraceFactory> SentryStackTraceFactoryAccessor { get; }

        internal string? Release => ReleaseLocator.Resolve(_options);

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

            AddMemoryInfo(@event.Contexts);
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

            if (@event.Level == null)
            {
                @event.Level = SentryLevel.Error;
            }

            if (@event.Release == null)
            {
                @event.Release = Release;
            }

            if (@event.Exception == null)
            {
                var stackTrace = SentryStackTraceFactoryAccessor().Create(@event.Exception);
                if (stackTrace != null)
                {
                    var thread = new SentryThread
                    {
                        Crashed = false,
                        Current = true,
                        Name = Thread.CurrentThread.Name,
                        Id = Environment.CurrentManagedThreadId,
                        Stacktrace = stackTrace
                    };

                    @event.SentryThreads = @event.SentryThreads?.Any() == true
                        ? new List<SentryThread>(@event.SentryThreads) { thread }
                        : new[] { thread }.AsEnumerable();
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
                        ReportAssembliesMode.InformationalVersion => assembly.GetNameAndVersion().Version ?? string.Empty,
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

        private void AddMemoryInfo(Contexts contexts)
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
                memory.Generation,
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

        private static IDictionary<string, string>? CultureInfoToDictionary(CultureInfo cultureInfo)
        {
            var dic = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(cultureInfo.Name))
            {
                dic.Add("Name", cultureInfo.Name);
            }
            if (!string.IsNullOrWhiteSpace(cultureInfo.DisplayName))
            {
                dic.Add("DisplayName", cultureInfo.DisplayName);
            }
            if (cultureInfo.Calendar is { } cal)
            {
                dic.Add("Calendar", cal.GetType().Name);
            }

            return dic.Count > 0 ? dic : null;
        }
    }

}
