using FastSerialization;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry.Profiling;

internal class SamplingTransactionProfiler : ITransactionProfiler
{
    public Action? OnFinish;
    private SampleProfilerSession? _session;
    private readonly CancellationToken _cancellationToken;
    private readonly SentryStopwatch _stopwatch = SentryStopwatch.StartNew();
    private TimeSpan? _duration;
    private Task<MemoryStream>? _data;
    private readonly string _tempDirectoryPath;
    private Transaction? _transaction;
    private readonly SentryOptions _options;

    public SamplingTransactionProfiler(string tempDirectoryPath, SentryOptions options, CancellationToken cancellationToken)
    {
        _options = options;
        _tempDirectoryPath = tempDirectoryPath;
        _cancellationToken = cancellationToken;
    }

    public void Start(int timeoutMs)
    {
        _session = SampleProfilerSession.StartNew(_cancellationToken);
        _cancellationToken.Register(() =>
        {
            if (Stop())
            {
                _options.LogDebug("Profiling cancelled.");
            }
        });
        Task.Delay(timeoutMs, _cancellationToken).ContinueWith(_ =>
        {
            if (Stop(TimeSpan.FromMilliseconds(timeoutMs)))
            {
                _options.LogDebug("Profiling is being cut-of after {0} ms because the transaction takes longer than that.", timeoutMs);
            }
        }, CancellationToken.None);
    }

    private bool Stop(TimeSpan? duration = null)
    {
        if (_duration is null && _session is not null)
        {
            lock (_session)
            {
                if (_duration is null)
                {
                    _duration = duration ?? _stopwatch.Elapsed;
                    try
                    {
                        _data = _session.FinishAsync();
                    }
                    catch (Exception e)
                    {
                        _options.LogWarning("Exception while stopping a profiler session.", e);
                    }
                    return true;
                }
            }
        }
        return false;
    }

    /// <inheritdoc />
    public void Finish()
    {
        if (Stop())
        {
            _options.LogDebug("Profiling stopped on transaction finish.");
        }
        OnFinish?.Invoke();
    }

    /// <inheritdoc />
    public async Task<ProfileInfo?> CollectAsync(Transaction transaction)
    {
        if (_data is null || _duration is null)
        {
            _options.LogDebug("Profiling collection cannot proceed because it doesn't seem to have finished properly.");
            return null;
        }
        _options.LogDebug("Starting profile processing.");

        try
        {
            _transaction = transaction;
            using var traceLog = await CreateTraceLogAsync().ConfigureAwait(false);

            if (traceLog is null)
            {
                return null;
            }

            try
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                _options.LogDebug("Converting profile to Sentry format.");

                var processor = new TraceLogProcessor(_options, traceLog)
                {
                    MaxTimestampMs = _duration.Value.TotalMilliseconds
                };

                var profile = processor.Process(_cancellationToken);
                if (_cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                if (profile.Samples.Empty)
                {
                    _options.LogDebug("Profiling finished successfully but there are no samples, skipping.");
                    return null;
                }
                else
                {
                    _options.LogDebug("Profiling finished successfully.");
                    return CreateProfileInfo(transaction, profile);
                }
            }
            finally
            {
                traceLog.Dispose();
                if (File.Exists(traceLog.FilePath))
                {
                    _options.LogDebug("Removing temporarily file '{0}'.", traceLog.FilePath);
                    File.Delete(traceLog.FilePath);
                }
            }
        }
        catch (Exception e)
        {
            _options.LogWarning("Exception while collecting/processing a profiler session.", e);
            return null;
        }
    }

    internal static ProfileInfo CreateProfileInfo(Transaction transaction, SampleProfile profile)
    {
        return new()
        {
            Contexts = transaction.Contexts,
            Environment = transaction.Environment,
            Transaction = transaction,
            // TODO FIXME - see https://github.com/getsentry/relay/pull/1902
            // Platform = transaction.Platform,
            Platform = "dotnet",
            Release = transaction.Release,
            StartTimestamp = transaction.StartTimestamp,
            Profile = profile
        };
    }

    // We need the TraceLog for all the stack processing it does.
    private async Task<TraceLog?> CreateTraceLogAsync()
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
            _options.LogWarning("Couldn't initialize EventPipeEventSource.");
            return null;
        }

        return ConvertToETLX(eventSource);
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
            }) as EventPipeEventSource;

        if (eventSource is not null)
        {
            // TODO make downsampling conditional once this is available: https://github.com/dotnet/runtime/issues/82939
            // _options.LogDebug("Profile will be downsampled before processing.");
            new Downsampler().AttachTo(eventSource);
        }

        return eventSource;
    }

    private TraceLog? ConvertToETLX(EventPipeEventSource source)
    {
        Debug.Assert(_transaction is not null);
        var etlxPath = Path.Combine(_tempDirectoryPath, $"{_transaction.EventId}.etlx");
        _options.LogDebug("Writing profile temporarily to '{0}'.", etlxPath);
        if (File.Exists(etlxPath))
        {
            _options.LogDebug("Temporary file '{0}' already exists, deleting first.", etlxPath);
            File.Delete(etlxPath);
        }
        typeof(TraceLog)
            .GetMethod(
                "CreateFromEventPipeEventSources",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                new Type[] { typeof(TraceEventDispatcher), typeof(string), typeof(TraceLogOptions) })?
            .Invoke(null, new object[] { source, etlxPath, new TraceLogOptions() { ContinueOnError = true } });

        if (!File.Exists(etlxPath))
        {
            _options.LogWarning("Profiler failed at CreateFromEventPipeEventSources() - temproary file '{0}' doesn't exist.", etlxPath);
            return null;
        }

        return new TraceLog(etlxPath);
    }

    private readonly BindingFlags _commonBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
}
