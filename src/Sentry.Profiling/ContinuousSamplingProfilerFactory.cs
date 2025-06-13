using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Profiling;

internal class ContinuousSamplingProfilerFactory : IDisposable
{
    private const int TRUE = 1;
    private const int FALSE = 0;

    private readonly SentryOptions _options;
    private readonly Task<SampleProfilerSession> _sessionTask;
    private ContinuousSamplingProfiler? _profiler;
    private bool _errorLogged = false;
    private int _inProgress = FALSE;

    public ContinuousSamplingProfilerFactory(SentryOptions options, TimeSpan startupTimeout)
    {
        _options = options;

        _sessionTask = Task.Run(async () =>
        {
            var session = SampleProfilerSession.StartNew(options.DiagnosticLogger);
            await session.WaitForFirstEventAsync().ConfigureAwait(false);
            return session;
        });

        if (startupTimeout != TimeSpan.Zero && !_sessionTask.Wait(startupTimeout))
        {
            options.LogWarning("Profiler session startup took longer than the given timeout {0:c}. Profiling will start once the first event is received.", startupTimeout);
        }
    }

    public ContinuousSamplingProfiler? Start()
    {
        if (Interlocked.CompareExchange(ref _inProgress, TRUE, FALSE) == FALSE)
        {
            try
            {
                if (_sessionTask.IsCompleted)
                {
                    if (_sessionTask.IsCompletedSuccessfully)
                    {
                        _profiler = new ContinuousSamplingProfiler(_options, _sessionTask.Result);
                        return _profiler;
                    }

                    if (!_errorLogged)
                    {
                        _errorLogged = true;
                        if (_sessionTask.Exception is not null)
                        {
                            _options.LogError(_sessionTask.Exception, "Failed to start profiler session.");
                        }
                        else
                        {
                            _options.LogError("Failed to start profiler session with unknown error.");
                        }
                    }
                }
                else
                {
                    _options.LogDebug("Profiler session not ready yet.");
                }
            }
            finally
            {
                Interlocked.Exchange(ref _inProgress, FALSE);
            }
        }

        return null;
    }

    public void Dispose()
    {
        _profiler?.Dispose();
        if (_sessionTask.IsCompletedSuccessfully)
        {
            _sessionTask.Result.Dispose();
        }
    }
}
