using Microsoft.Diagnostics.Tracing;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;
using Sentry.Protocol.Envelopes;

namespace Sentry.Profiling;

internal class ContinuousSamplingProfiler : IDisposable
{
    private readonly SentryOptions _options;
    private readonly SampleProfilerSession _session;
    private readonly SampleProfileBuilder _processor;
    private readonly double _startTimeMs;
    private bool _stopped = false;
    private TaskCompletionSource _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ContinuousSamplingProfiler(SentryOptions options, SampleProfilerSession session)
    {
        _options = options;
        _session = session;
        _startTimeMs = session.Elapsed.TotalMilliseconds;
        _processor = new SampleProfileBuilder(options, session.TraceLog);
        session.SampleEventParser.ThreadSample += OnThreadSample;
    }

    private void OnThreadSample(TraceEvent data)
    {
        var timestampMs = data.TimeStampRelativeMSec;
        if (timestampMs >= _startTimeMs)
        {
            try
            {
                _processor.AddSample(data, timestampMs - _startTimeMs);
            }
            catch (Exception e)
            {
                _options.LogWarning(e, "Failed to process a continuous profile sample.");
            }
        }
    }

    public void Stop()
    {
        if (!_stopped)
        {
            lock (_session)
            {
                if (!_stopped)
                {
                    _stopped = true;
                    _session.SampleEventParser.ThreadSample -= OnThreadSample;
                    _completionSource.TrySetResult();
                    _options.LogDebug("Continuous profiling stopped.");
                }
            }
        }
    }

    public async Task<Protocol.Envelopes.ISerializable> CollectAsync()
    {
        if (!_stopped)
        {
            throw new InvalidOperationException("Profiler.Collect() called before Stop()");
        }

        // Wait for the last sample, or at most 1 second
        var completedTask = await Task.WhenAny(_completionSource.Task, Task.Delay(1_000)).ConfigureAwait(false);
        if (!completedTask.Equals(_completionSource.Task))
        {
            _options.LogWarning("CollectAsync timed out after 1 second. Were there any samples collected?");
        }

        var info = CreateProfileInfo(_processor.Profile);
        return Protocol.Envelopes.AsyncJsonSerializable.CreateFrom(Task.FromResult(info));
    }

    private ContinuousProfileInfo CreateProfileInfo(SampleProfile profile)
    {
        var runtime = SentryRuntime.Current;
        var os = SentryOperatingSystem.Current;
        var device = SentryDevice.Current;

        return new()
        {
            Version = "2",
            Platform = "dotnet",
            Profile = profile,
            Environment = _options.Environment,
            Release = _options.Release,
            OsName = os.Name,
            OsVersion = os.Version,
            DeviceArchitecture = device.Architecture,
            RuntimeName = runtime.Name,
            RuntimeVersion = runtime.Version
        };
    }

    public void Dispose()
    {
        Stop();
    }
}
