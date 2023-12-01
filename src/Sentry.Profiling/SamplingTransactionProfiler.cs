using Microsoft.Diagnostics.Tracing;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry.Profiling;

internal class SamplingTransactionProfiler : ITransactionProfiler
{
    public Action? OnFinish;
    private readonly CancellationToken _cancellationToken;
    private bool _stopped = false;
    private readonly SentryOptions _options;
    private SampleProfileBuilder _processor;
    private SampleProfilerSession _session;
    private readonly double _startTimeMs;
    private double _endTimeMs;
    private TaskCompletionSource _completionSource = new();

    public SamplingTransactionProfiler(SentryOptions options, SampleProfilerSession session, int timeoutMs, CancellationToken cancellationToken)
    {
        _options = options;
        _session = session;
        _cancellationToken = cancellationToken;
        _startTimeMs = session.Elapsed.TotalMilliseconds;
        _endTimeMs = double.MaxValue;
        _processor = new SampleProfileBuilder(options, session.TraceLog);
        session.SampleEventParser.ThreadSample += OnThreadSample;
        cancellationToken.Register(() =>
        {
            if (Stop())
            {
                options.LogDebug("Profiling cancelled.");
            }
        });
        Task.Delay(timeoutMs, cancellationToken).ContinueWith(_ =>
        {
            if (Stop(_startTimeMs + timeoutMs))
            {
                options.LogDebug("Profiling is being cut-of after {0} ms because the transaction takes longer than that.", timeoutMs);
            }
        }, CancellationToken.None);
    }

    private bool Stop(double? endTimeMs = null)
    {
        endTimeMs ??= _session.Elapsed.TotalMilliseconds;
        if (!_stopped)
        {
            lock (_session)
            {
                if (!_stopped)
                {
                    _stopped = true;
                    _endTimeMs = endTimeMs.Value;
                    OnFinish?.Invoke();
                    return true;
                }
            }
        }
        return false;
    }

    // We need custom sampling because the TraceLog dispatches events from a queue with a delay of about 2 seconds.
    private void OnThreadSample(TraceEvent data)
    {
        var timestampMs = data.TimeStampRelativeMSec;
        if (timestampMs >= _startTimeMs)
        {
            if (timestampMs <= _endTimeMs)
            {
                try
                {
                    _processor.AddSample(data, timestampMs - _startTimeMs);
                }
                catch (Exception e)
                {
                    _options.LogWarning("Failed to process a profile sample.", e);
                }
            }
            else
            {
                _session.SampleEventParser.ThreadSample -= OnThreadSample;
                _completionSource.TrySetResult();
            }
        }
    }

    /// <inheritdoc />
    public void Finish()
    {
        if (Stop())
        {
            _options.LogDebug("Profiling stopped on transaction finish.");
        }
    }

    /// <inheritdoc />
    public Protocol.Envelopes.ISerializable Collect(Transaction transaction)
        => Protocol.Envelopes.AsyncJsonSerializable.CreateFrom(CollectAsync(transaction));

    private async Task<ProfileInfo> CollectAsync(Transaction transaction)
    {
        if (!_stopped)
        {
            throw new InvalidOperationException("Profiler.Collect() called before Finish()");
        }

        // Wait for the last sample (<= _endTimeMs), or at most 1 second. The timeout shouldn't happen because
        // TraceLog should dispatch events immediately. But if it does, send at least what we have already got.
        var completedTask = await Task.WhenAny(_completionSource.Task, Task.Delay(1_000, _cancellationToken)).ConfigureAwait(false);
        if (!completedTask.Equals(_completionSource.Task))
        {
            _options.LogWarning("CollectAsync timed out after 1 second. Were there any samples collected?");
        }

        return CreateProfileInfo(transaction, _processor.Profile);
    }

    internal static ProfileInfo CreateProfileInfo(Transaction transaction, SampleProfile profile)
    {
        return new()
        {
            Contexts = transaction.Contexts,
            Environment = transaction.Environment,
            Transaction = transaction,
            Platform = transaction.Platform,
            Release = transaction.Release,
            StartTimestamp = transaction.StartTimestamp,
            Profile = profile
        };
    }
}
