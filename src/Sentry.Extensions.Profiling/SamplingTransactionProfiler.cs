using FastSerialization;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry.Extensions.Profiling;

internal class SamplingTransactionProfilerFactory : ITransactionProfilerFactory
{
    // We only allow a single profile so let's keep track of the current status.
    internal int _inProgress = FALSE;

    private const int TRUE = 1;
    private const int FALSE = 0;

    // Stop profiling after the given number of milliseconds.
    private const int TIME_LIMIT_MS = 30_000;

    private readonly string _tempDirectoryPath;

    private readonly IDiagnosticLogger? _logger;

    public SamplingTransactionProfilerFactory(string tempDirectoryPath, IDiagnosticLogger? logger)
    {
        _tempDirectoryPath = tempDirectoryPath;
        _logger = logger;
    }

    /// <inheritdoc />
    public ITransactionProfiler? OnTransactionStart(ITransaction _, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Start a profiler if one wasn't running yet.
        if (Interlocked.Exchange(ref _inProgress, TRUE) == FALSE)
        {
            _logger?.LogDebug("Starting a sampling profiler session.");
            try
            {
                return new SamplingTransactionProfiler(_tempDirectoryPath, now, TIME_LIMIT_MS, _logger, cancellationToken)
                {
                    OnFinish = () => _inProgress = FALSE
                };
            }
            catch (Exception e)
            {
                _logger?.LogWarning("Failed to start a profiler session.", e);
                _inProgress = FALSE;
            }
        }
        return null;
    }
}

internal class SamplingTransactionProfiler : ITransactionProfiler
{
    public Action? OnFinish;
    private readonly SampleProfilerSession _session;
    private readonly CancellationToken _cancellationToken;
    private readonly DateTimeOffset _startTime;
    private DateTimeOffset? _endTime;
    private Task<MemoryStream>? _data;
    private readonly string _tempDirectoryPath;
    private Transaction? _transaction;
    private readonly IDiagnosticLogger? _logger;

    public SamplingTransactionProfiler(string tempDirectoryPath, DateTimeOffset now, int timeoutMs, IDiagnosticLogger? logger, CancellationToken cancellationToken)
    {
        _logger = logger;
        _tempDirectoryPath = tempDirectoryPath;
        _startTime = now;
        _cancellationToken = cancellationToken;
        _session = new(cancellationToken);
        var clock = Stopwatch.StartNew();
        _cancellationToken.Register(() =>
        {
            _logger?.LogDebug("Profiling cancelled.");
            Stop(now + clock.Elapsed);
        });
        Task.Delay(timeoutMs, _cancellationToken).ContinueWith(_ =>
        {
            _logger?.LogDebug("Profiling is being cut-of after {0} ms because the transaction takes longer than that.", timeoutMs);
            Stop(now + TimeSpan.FromMilliseconds(timeoutMs));
        }, CancellationToken.None);
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
                    try
                    {
                        _data = _session.Finish();
                    }
                    catch (Exception e)
                    {
                        _logger?.LogWarning("Exception while stopping a profiler session.", e);
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public void OnTransactionFinish(DateTimeOffset now)
    {
        _logger?.LogDebug("Stopping profiling collection on transaction finish.");
        Stop(now);
        OnFinish?.Invoke();
    }

    /// <inheritdoc />
    public async Task<ProfileInfo?> Collect(Transaction transaction)
    {
        if (_data is null || _endTime is null)
        {
            _logger?.LogDebug("Profiling collection cannot proceed because it doesn't seem to have finished properly.");
            return null;
        }
        _logger?.LogDebug("Starting profile processing.");

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

                _logger?.LogDebug("Converting profile to Sentry format.");

                var processor = new TraceLogProcessor(traceLog)
                {
                    MaxTimestampMs = (ulong)(_endTime.Value - _startTime).TotalMilliseconds
                };

                var profile = processor.Process(_cancellationToken);
                if (_cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                _logger?.LogDebug("Profiling successfully finished.");
                return CreateProfileInfo(transaction, _startTime, profile);
            }
            finally
            {
                traceLog.Dispose();
                if (File.Exists(traceLog.FilePath))
                {
                    _logger?.LogDebug("Removing temporarily file '{0}'.", traceLog.FilePath);
                    File.Delete(traceLog.FilePath);
                }
            }
        }
        catch (Exception e)
        {
            _logger?.LogWarning("Exception while collecting/processing a profiler session.", e);
            return null;
        }
    }

    internal static ProfileInfo CreateProfileInfo(Transaction transaction, DateTimeOffset startTime, SampleProfile profile)
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
            StartTimestamp = startTime,
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
            _logger?.LogWarning("Couldn't initialize EventPipeEventSource.");
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
            // _logger?.LogDebug("Profile will be downsampled before processing.");
            new Downsampler().AttachTo(eventSource);
        }

        return eventSource;
    }

    private TraceLog? ConvertToETLX(EventPipeEventSource source)
    {
        Debug.Assert(_transaction is not null);
        var etlxPath = Path.Combine(_tempDirectoryPath, $"{_transaction.EventId}.etlx");
        _logger?.LogDebug("Writing profile temporarily to '{0}'.", etlxPath);
        if (File.Exists(etlxPath))
        {
            _logger?.LogDebug("Temporary file '{0}' already exists, deleting first.", etlxPath);
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
            _logger?.LogWarning("Profiler failed at CreateFromEventPipeEventSources() - temproary file '{0}' doesn't exist.", etlxPath);
            return null;
        }

        return new TraceLog(etlxPath);
    }

    private readonly BindingFlags _commonBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
}
