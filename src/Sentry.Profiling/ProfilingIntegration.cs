using System.Diagnostics;
using Sentry.Extensibility;
using Sentry.Integrations;
using Sentry.Protocol.Envelopes;

namespace Sentry.Profiling;

/// <summary>
/// Enables transaction performance profiling.
/// </summary>
public class ProfilingIntegration : ISdkIntegration, IDisposable
{
    private TimeSpan _startupTimeout;
    private ContinuousSamplingProfilerFactory? _continuousProfilerFactory;
    private ContinuousSamplingProfiler? _continuousProfiler;

    /// <summary>
    /// Initializes the profiling integration.
    /// </summary>
    /// <param name="startupTimeout">
    /// If not given or TimeSpan.Zero, then the profiler initialization is asynchronous.
    /// This is useful for applications that need to start quickly. The profiler will start in the background
    /// and will be ready to capture transactions that have started after the profiler has started.
    ///
    /// If given a non-zero timeout, profiling startup blocks up to the given amount of time. If the timeout is reached
    /// and the profiler session hasn't started yet, the execution is unblocked and behaves as the async startup,
    /// i.e. transactions will be profiled only after the session is eventually started.
    /// </param>
    public ProfilingIntegration(TimeSpan startupTimeout = default)
    {
        Debug.Assert(TimeSpan.Zero == default);
        _startupTimeout = startupTimeout;
    }

    /// <inheritdoc/>
    public void Register(IHub hub, SentryOptions options)
    {
        if (options.IsProfilingEnabled)
        {
            try
            {
                options.TransactionProfilerFactory ??= new SamplingTransactionProfilerFactory(options, _startupTimeout);

                if (options.IsContinuousProfilingEnabled)
                {
                    _continuousProfilerFactory = new ContinuousSamplingProfilerFactory(options, _startupTimeout);
                    _continuousProfiler = _continuousProfilerFactory.Start();

                    if (_continuousProfiler is not null)
                    {
                        // Register a background task to collect and send profiles periodically
                        Task.Run(async () =>
                        {
                            while (!options.ShutdownToken.IsCancellationRequested)
                            {
                                try
                                {
                                    await Task.Delay(options.ContinuousProfilingInterval, options.ShutdownToken).ConfigureAwait(false);

                                    if (_continuousProfiler is not null)
                                    {
                                        _continuousProfiler.Stop();
                                        var profile = await _continuousProfiler.CollectAsync().ConfigureAwait(false);
                                        var envelope = new Envelope(
                                            new Dictionary<string, object?>(),
                                            new[]
                                            {
                                                new EnvelopeItem(
                                                    new Dictionary<string, object?>
                                                    {
                                                        { "type", "profile" }
                                                    },
                                                    profile)
                                            });

                                        hub.CaptureEnvelope(envelope);

                                        // Start a new profiler for the next interval
                                        _continuousProfiler = _continuousProfilerFactory.Start();
                                    }
                                }
                                catch (Exception e)
                                {
                                    options.LogError(e, "Error during continuous profiling.");
                                }
                            }
                        }, options.ShutdownToken);
                    }
                }
            }
            catch (Exception e)
            {
                options.LogError(e, "Failed to initialize the profiler");
            }
        }
        else
        {
            options.LogInfo("Profiling Integration is disabled because profiling is disabled by configuration.");
        }
    }

    /// <summary>
    /// Disposes the profiling integration and stops any active profilers.
    /// </summary>
    public void Dispose()
    {
        _continuousProfiler?.Dispose();
        _continuousProfilerFactory?.Dispose();
    }
}
