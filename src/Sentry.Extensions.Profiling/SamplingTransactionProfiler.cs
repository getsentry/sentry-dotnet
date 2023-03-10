using System.Threading;
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

    private const int TRUE = 1;
    private const int FALSE = 0;

    // Stop profiling after the given number of milliseconds.
    private const int TIME_LIMIT_MS = 30_000;

    private readonly string _cacheDirectoryPath;

    public SamplingTransactionProfilerFactory(string cacheDirectoryPath)
    {
        _cacheDirectoryPath = cacheDirectoryPath;
    }

    /// <inheritdoc />
    public ITransactionProfiler? OnTransactionStart(ITransaction _, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Start a profiler if one wasn't running yet.
        if (Interlocked.Exchange(ref _inProgress, TRUE) == FALSE)
        {
            return new SamplingTransactionProfiler(_cacheDirectoryPath, now, TIME_LIMIT_MS, cancellationToken)
            {
                OnFinish = () => _inProgress = FALSE
            };
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
    private readonly string _cacheDirectoryPath;
    private Transaction? _transaction;

    public SamplingTransactionProfiler(string cacheDirectoryPath, DateTimeOffset now, int timeoutMs, CancellationToken cancellationToken)
    {
        _cacheDirectoryPath = cacheDirectoryPath;
        _startTime = now;
        _cancellationToken = cancellationToken;
        _session = new(cancellationToken);
        _cancellationToken.Register(() => Stop(now + TimeSpan.FromMilliseconds(timeoutMs)));
        Task.Delay(timeoutMs, _cancellationToken).ContinueWith(_ => Stop(now + TimeSpan.FromMilliseconds(timeoutMs)), CancellationToken.None);
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

            var processor = new TraceLogProcessor(traceLog)
            {
                MaxTimestampMs = (ulong)(_endTime.Value - _startTime).TotalMilliseconds
            };

            var profile = processor.Process(_cancellationToken);
            if (_cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            return CreateProfileInfo(transaction, _startTime, profile);
        }
        finally
        {
            traceLog.Dispose();
            if (File.Exists(traceLog.FilePath))
            {
                File.Delete(traceLog.FilePath);
            }
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
            new Downsampler().AttachTo(eventSource);
        }

        return eventSource;
    }

    private TraceLog? ConvertToETLX(EventPipeEventSource source)
    {
        Debug.Assert(_transaction is not null);
        var etlxPath = Path.Combine(_cacheDirectoryPath, $"{_transaction.EventId}.etlx");
        if (File.Exists(etlxPath))
        {
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
            return null;
        }

        return new TraceLog(etlxPath);
    }

    private readonly BindingFlags _commonBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
}
