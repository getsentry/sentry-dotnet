using Quartz;
using Sentry.Extensibility;

namespace Sentry.Quartz;

/// <summary>
/// A Quartz.NET job listener that sends a Sentry check-in each time a job with
/// <see cref="SentryCronMonitorSlugAttribute"/> runs.
/// </summary>
public class SentryJobListener : IJobListener
{
    // Keyed on the context, which Quartz passes to both callbacks of a run
    private readonly ConditionalWeakTable<IJobExecutionContext, JobCheckIn> _checkIns = new();
    private readonly JobMonitor _monitor;

    /// <summary>
    /// Creates a listener with the default options.
    /// </summary>
    public SentryJobListener() : this(new SentryQuartzOptions())
    {
    }

    /// <summary>
    /// Creates a listener with the given options.
    /// </summary>
    /// <param name="options">The options.</param>
    public SentryJobListener(SentryQuartzOptions options) : this(options, HubAdapter.Instance)
    {
    }

    internal SentryJobListener(SentryQuartzOptions options, IHub hub, IDiagnosticLogger? logger = null)
    {
        _monitor = new JobMonitor(options, hub, logger);

        // Quartz 4 default-implements IJobListener with ValueTask methods, so ours would never be called
        var quartzVersion = typeof(IJobListener).Assembly.GetName().Version;
        if (IsUnsupportedQuartzVersion(quartzVersion))
        {
            _monitor.Logger?.LogError("Sentry.Quartz3 supports Quartz.NET 3.x, but Quartz.NET {0} is loaded, so no " +
                                      "check-ins will be sent. Use the Sentry.Quartz package for Quartz.NET 4.x.", quartzVersion);
        }
    }

    internal static bool IsUnsupportedQuartzVersion(Version? version) => version is { Major: >= 4 };

    /// <inheritdoc />
    public string Name => "Sentry";

    /// <inheritdoc />
    public async Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (await _monitor.StartAsync(context, cancellationToken).ConfigureAwait(false) is { } checkIn)
        {
            _checkIns.Remove(context);
            _checkIns.Add(context, checkIn);
        }
    }

    /// <inheritdoc />
    public Task JobWasExecuted(
        IJobExecutionContext context,
        JobExecutionException? jobException,
        CancellationToken cancellationToken = default)
    {
        if (_checkIns.TryGetValue(context, out var checkIn))
        {
            _checkIns.Remove(context);
            _monitor.Finish(checkIn, failed: jobException is not null, context.JobRunTime);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
