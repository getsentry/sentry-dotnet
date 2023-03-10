using FastSerialization;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry.Extensions.Profiling;

internal class SamplingTransactionProfilerFactory : ITransactionProfilerFactory
{
    // We only allow a single profile so let's keep track of the current status.
    internal int _inProgress = FALSE;

    const int TRUE = 1;
    const int FALSE = 0;

    // Stop profiling after the given number of milliseconds.
    const int TIME_LIMIT_MS = 30_000;

    /// <inheritdoc />
    public ITransactionProfiler? OnTransactionStart(ITransaction _, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Start a profiler if one wasn't running yet.
        if (Interlocked.Exchange(ref _inProgress, TRUE) == FALSE)
        {
            var profiler = new SamplingTransactionProfiler(now, cancellationToken, TIME_LIMIT_MS);
            profiler.OnFinish = () => _inProgress = FALSE;
            return profiler;
        }
        return null;
    }
}

internal class SamplingTransactionProfiler : ITransactionProfiler
{
    private SampleProfilerSession _session;
    private readonly CancellationToken _cancellationToken;
    private readonly DateTimeOffset _startTime;
    private DateTimeOffset? _endTime;
    private Task<MemoryStream>? _data;
    public Action? OnFinish;

    public SamplingTransactionProfiler(DateTimeOffset now, CancellationToken cancellationToken, int timeoutMs)
    {
        _startTime = now;
        _session = new(cancellationToken);
        _cancellationToken = cancellationToken;
        Task.Delay(timeoutMs, cancellationToken).ContinueWith(_ => Stop(now + TimeSpan.FromMilliseconds(timeoutMs)));
    }

    private void Stop(DateTimeOffset now)
    {
        if (_endTime is null)
        {
            lock (_session)
            {
                if (_endTime is null)
                {
                    _endTime = now;
                    _data = _session.Finish();
                }
            }
        }
    }

    /// <inheritdoc />
    public void OnTransactionFinish(DateTimeOffset now)
    {
        Stop(now);
        OnFinish?.Invoke();
    }

    /// <inheritdoc />
    public async Task<ProfileInfo?> Collect(Transaction transaction)
    {
        Debug.Assert(_data is not null, "OnTransactionFinish() wasn't called before Collect()");
        Debug.Assert(_endTime is not null);

        using var traceLog = await CreateTraceLog();

        if (_cancellationToken.IsCancellationRequested || traceLog is null)
        {
            return null;
        }

        var processor = new TraceLogProcessor(traceLog);
        processor.MaxTimestampMs = (ulong)(_endTime.Value - _startTime).TotalMilliseconds;

        var profile = processor.Process(_cancellationToken);
        if (_cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        return new()
        {
            Contexts = transaction.Contexts,
            Environment = transaction.Environment,
            Transaction = transaction,
            // TODO FIXME - see https://github.com/getsentry/relay/pull/1902
            // Platform = transaction.Platform,
            Platform = "dotnet",
            Release = transaction.Release,
            StartTimestamp = _startTime,
            Profile = profile
        };
    }

    // We need the TraceLog for all the stack processing it does.
    private async Task<TraceLog?> CreateTraceLog()
    {
        if (_data is null || _cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        using var nettraceStream = await _data.ConfigureAwait(false);
        if (_cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        using var eventSource = CreateEventPipeEventSource(nettraceStream);
        if (_cancellationToken.IsCancellationRequested || eventSource is null)
        {
            return null;
        }

        var etlxStream = ConvertToETLX(eventSource);
        if (_cancellationToken.IsCancellationRequested || etlxStream is null)
        {
            return null;
        }

        // We can free the original nettraceStream now.
        eventSource.Dispose();
        nettraceStream.Dispose();

        // Create TraceLog from the newly created ETLX stream.
        // new TraceLog();
        var traceLog = typeof(TraceLog)
            .GetConstructor(_commonBindingFlags, Array.Empty<Type>())?
            .Invoke(Array.Empty<object>()) as TraceLog;

        if (traceLog is null)
        {
            return null;
        }

        if (!TraceLogInitializeFromStream(traceLog, etlxStream))
        {
            traceLog.Dispose();
            etlxStream.Dispose();
            return null;
        }

        return traceLog;
    }

    // TODO make TraceLog.InitializeFromFile() alternative to work with an existing stream.
    // NOTE: this isn't finished - had some issues with initializing nullable type args in those factories...
    private bool TraceLogInitializeFromStream(TraceLog traceLog, MemoryStream etlxStream)
    {
        // As of TraceLog version 74, all StreamLabels are 64-bit.  See IFastSerializableVersion for details.
        Deserializer deserializer = new Deserializer(new PinnedStreamReader(etlxStream, 0x10000), "stream");
        deserializer.TypeResolver = typeName => System.Type.GetType(typeName);  // resolve types in this assembly (and mscorlib)

        var newTraceProcess = () =>
        {
            var ctor = typeof(TraceProcess).GetConstructor(_commonBindingFlags, new Type[] { typeof(int), typeof(TraceLog), typeof(ProcessIndex) });
            return ctor!.Invoke(new object[] { 0, traceLog, 0 })! as TraceProcess;
        };

        var newTraceModuleFile = () =>
        {
            var ctor = typeof(TraceModuleFile).GetConstructor(_commonBindingFlags, new Type[] { typeof(string), typeof(ulong), typeof(ModuleFileIndex) });
            return ctor!.Invoke(new object[] { "", (ulong)0, ModuleFileIndex.Invalid })! as TraceModuleFile;
        };

        var newTraceModuleFiles = () =>
        {
            var ctor = typeof(TraceModuleFiles).GetConstructor(_commonBindingFlags, new Type[] { typeof(TraceLog) });
            return ctor!.Invoke(new object[] { traceLog })! as TraceModuleFiles;
        };

        var newTraceCodeAddresses = () =>
        {
            var ctor = typeof(TraceCodeAddresses).GetConstructor(_commonBindingFlags, new Type[] { typeof(TraceLog), typeof(TraceModuleFiles) });
            return ctor!.Invoke(new object[] { traceLog, newTraceModuleFiles() })! as TraceCodeAddresses;
        };

        var newTraceEventStats = () =>
        {
            var ctor = typeof(TraceEventStats).GetConstructor(_commonBindingFlags, new Type[] { typeof(TraceLog) });
            return ctor!.Invoke(new object[] { traceLog })! as TraceEventStats;
        };

        // when the deserializer needs a TraceLog we return the current instance.  We also assert that
        // we only do this once.
        deserializer.RegisterFactory(typeof(TraceLog), delegate
        {
            return traceLog;
        });
        deserializer.RegisterFactory(typeof(TraceProcess), newTraceProcess);
        deserializer.RegisterFactory(typeof(TraceProcesses), delegate
        {
            var ctor = typeof(TraceProcesses).GetConstructor(_commonBindingFlags, new Type[] { typeof(TraceLog) });
            return ctor!.Invoke(new object[] { traceLog })! as TraceProcesses;
        });
        deserializer.RegisterFactory(typeof(TraceThreads), delegate
        {
            var ctor = typeof(TraceThreads).GetConstructor(_commonBindingFlags, new Type[] { typeof(TraceLog) });
            return ctor!.Invoke(new object[] { traceLog })! as TraceThreads;
        });
        deserializer.RegisterFactory(typeof(TraceThread), delegate
        {
            var ctor = typeof(TraceThread).GetConstructor(_commonBindingFlags, new Type[] { typeof(int), typeof(TraceProcess), typeof(ThreadIndex) });
            return ctor!.Invoke(new object[] { 0, newTraceProcess(), ThreadIndex.Invalid })! as TraceThread;
        });
        // deserializer.RegisterFactory(typeof(TraceActivity), delegate
        // { return new TraceActivity(ActivityIndex.Invalid, null, EventIndex.Invalid, CallStackIndex.Invalid, 0, 0, false, false, TraceActivity.ActivityKind.Invalid); });

        deserializer.RegisterFactory(typeof(TraceModuleFiles), newTraceModuleFiles);
        deserializer.RegisterFactory(typeof(TraceModuleFile), newTraceModuleFile);
        deserializer.RegisterFactory(typeof(TraceMethods), delegate
        {
            var ctor = typeof(TraceMethods).GetConstructor(_commonBindingFlags, new Type[] { typeof(TraceCodeAddresses) });
            return ctor!.Invoke(new object[] { newTraceCodeAddresses() })! as TraceMethods;
        });
        deserializer.RegisterFactory(typeof(TraceCodeAddresses), newTraceCodeAddresses);
        var TraceCodeAddresses_ILToNativeMap = typeof(TraceCodeAddresses).GetNestedType("ILToNativeMap", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        deserializer.RegisterFactory(TraceCodeAddresses_ILToNativeMap, delegate
        {
            var ctor = TraceCodeAddresses_ILToNativeMap.GetConstructor(_commonBindingFlags, new Type[] { });
            return ctor!.Invoke(new object[] { })! as IFastSerializable;
        });
        deserializer.RegisterFactory(typeof(TraceCallStacks), delegate
        {
            var ctor = typeof(TraceCallStacks).GetConstructor(_commonBindingFlags, new Type[] { typeof(TraceLog), typeof(TraceCodeAddresses) });
            return ctor!.Invoke(new object[] { traceLog, newTraceCodeAddresses() })! as TraceCallStacks;
        });
        deserializer.RegisterFactory(typeof(TraceEventStats), newTraceEventStats);
        deserializer.RegisterFactory(typeof(TraceEventCounts), delegate
        {
            var ctor = typeof(TraceEventCounts).GetConstructor(_commonBindingFlags, new Type[] { typeof(TraceEventStats), TraceEvent });
            return ctor!.Invoke(new object[] { newTraceEventStats(), null })! as TraceEventCounts;
        });

        deserializer.RegisterFactory(typeof(TraceLoadedModules), delegate
        {
            var ctor = typeof(TraceLoadedModules).GetConstructor(_commonBindingFlags, new Type[] { typeof(TraceProcess) });
            return ctor!.Invoke(new object[] { newTraceProcess() })! as TraceLoadedModules;
        });
        deserializer.RegisterFactory(typeof(TraceLoadedModule), delegate
        {
            var ctor = typeof(TraceLoadedModules).GetConstructor(_commonBindingFlags, new Type[] { typeof(TraceProcess), typeof(TraceModuleFile), typeof(ulong) });
            return ctor!.Invoke(new object[] { newTraceProcess(), newTraceModuleFile(), (ulong)0 })! as TraceLoadedModules;
        });
        deserializer.RegisterFactory(typeof(TraceLoadedModule), delegate
        {
            var ctor = typeof(TraceLoadedModule).GetConstructor(_commonBindingFlags, new Type[] { typeof(TraceProcess), typeof(TraceModuleFile), typeof(ulong) });
            return ctor!.Invoke(new object[] { newTraceProcess(), newTraceModuleFile(), (ulong)0 })! as TraceLoadedModule;
        });
        deserializer.RegisterFactory(typeof(TraceManagedModule), delegate
        {
            var ctor = typeof(TraceManagedModule).GetConstructor(_commonBindingFlags, new Type[] { typeof(TraceProcess), typeof(TraceModuleFile), typeof(long) });
            return ctor!.Invoke(new object[] { newTraceProcess(), newTraceModuleFile(), (long)0 })! as TraceManagedModule;
        });

        // deserializer.RegisterFactory(typeof(ProviderManifest), delegate
        // {
        //     return new ProviderManifest(null, ManifestEnvelope.ManifestFormats.SimpleXmlFormat, 0, 0, "");
        // });
        // deserializer.RegisterFactory(typeof(DynamicTraceEventData), delegate
        // {
        //     return new DynamicTraceEventData(null, 0, 0, null, Guid.Empty, 0, null, Guid.Empty, null);
        // });

        // when the serializer needs any TraceEventParser class, we assume that its constructor
        // takes an argument of type TraceEventSource and that you can pass null to make an
        // 'empty' parser to fill in with FromStream.
        deserializer.RegisterDefaultFactory(delegate (Type typeToMake)
        {
            //     if (typeToMake.GetTypeInfo().IsSubclassOf(typeof(TraceEventParser)))
            //     {
            //         return (IFastSerializable)Activator.CreateInstance(typeToMake, new object[] { null })!;
            //     }

            return null;
        });

        IFastSerializable entry = deserializer.GetEntryObject(); // side-effect?

        // traceLog.RegisterStandardParsers();
        var method = typeof(TraceLog).GetMethod("RegisterStandardParsers", _commonBindingFlags);
        if (method is null)
        {
            return false;
        }
        method.Invoke(traceLog, Array.Empty<object>());

        return true;
    }

    // EventPipeEventSource(Stream stream) sets isStreaming = true even though the stream is pre-collected. This
    // causes read issues when converting to ETLX. It works fine if we use the private constructor, setting false.
    // TODO make a PR to change this
    private EventPipeEventSource? CreateEventPipeEventSource(MemoryStream nettraceStream)
    {
        var privateNewEventPipeEventSource = typeof(EventPipeEventSource).GetConstructor(
            _commonBindingFlags,
            new Type[] { typeof(PinnedStreamReader), typeof(string), typeof(bool) });

        var eventSource = privateNewEventPipeEventSource?.Invoke(new object[] {
                new PinnedStreamReader(nettraceStream, 16384, new SerializationConfiguration{ StreamLabelWidth = StreamLabelWidth.FourBytes }, StreamReaderAlignment.OneByte),
                "stream",
                false
            });

        return eventSource as EventPipeEventSource;
    }

    // Currently there doesn't seem to be a way to initialize a TraceLog without writing the ETLX to file first.
    // This circumvents that by using some private and internal methods to create a MemoryStream instead.
    // Ideally, we would be able to just initialize the TraceLog, see https://github.com/microsoft/perfview/issues/1829
    private MemoryStream? ConvertToETLX(EventPipeEventSource source)
    {
        using var traceLog = typeof(TraceLog)
            .GetConstructor(_commonBindingFlags, Array.Empty<Type>())?
            .Invoke(Array.Empty<object>()) as TraceLog;

        if (traceLog is null)
        {
            return null;
        }

        var prop = typeof(TraceLog).GetField("rawEventSourceToConvert", _commonBindingFlags);
        if (prop is null)
        {
            return null;
        }
        prop.SetValue(traceLog, source);

        // ContinueOnError - best-effort if there's a broken trace. The resulting file may contain broken stacks as a result.
        var options = new TraceLogOptions() { ContinueOnError = true };
        prop = typeof(TraceLog).GetField("options", _commonBindingFlags);
        if (prop is null)
        {
            return null;
        }
        prop.SetValue(traceLog, options);

        var dynamicParser = source.Dynamic; // side-effect

        // Get all the users data from the original source.   Note that this happens by reference, which means
        // that even though we have not built up the state yet (since we have not scanned the data yet), it will
        // still work properly (by the time we look at this user data, it will be updated).
        foreach (string key in source.UserData.Keys)
        {
            traceLog.UserData[key] = source.UserData[key];
        }

        var etlxMemoryStream = new MemoryStream();
        using var serializer = new Serializer(etlxMemoryStream, traceLog, leaveOpen: true);
        serializer.Dispose();
        etlxMemoryStream.Position = 0;
        return etlxMemoryStream;
    }

    private BindingFlags _commonBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
}
