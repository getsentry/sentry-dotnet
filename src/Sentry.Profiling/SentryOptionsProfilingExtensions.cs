using Sentry.Extensibility;
using Sentry.Profiling;

namespace Sentry;

/// <summary>
/// The additional Sentry Options extensions from Sentry Profiling.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryOptionsProfilingExtensions
{
    /// <summary>
    /// Adds ProfilingIntegration to Sentry.
    /// </summary>
    /// <param name="options">The Sentry options.</param>
    /// <param name="startupTimeout">
    /// If not given or TimeSpan.Zero, then the profiler initialization is asynchronous.
    /// This is useful for applications that need to start quickly. The profiler will start in the background
    /// and will be ready to capture transactions that have started after the profiler has started.
    ///
    /// If given a non-zero timeout, profiling startup blocks up to the given amount of time. If the timeout is reached
    /// and the profiler session hasn't started yet, the execution is unblocked and behaves as the async startup,
    /// i.e. transactions will be profiled only after the session is eventually started.
    /// </param>
    public static void AddProfilingIntegration(this SentryOptions options, TimeSpan startupTimeout = default)
    {
        if (options.HasIntegration<ProfilingIntegration>())
        {
            options.LogWarning($"{nameof(ProfilingIntegration)} has already been added. The second call to {nameof(AddProfilingIntegration)} will be ignored.");
            return;
        }

        options.AddIntegration(new ProfilingIntegration(startupTimeout));
    }

    /// <summary>
    /// Disables the Profiling integration.
    /// </summary>
    /// <param name="options">The SentryOptions to remove the integration from.</param>
    public static void DisableProfilingIntegration(this SentryOptions options)
    {
        options.RemoveIntegration<ProfilingIntegration>();
    }
}
