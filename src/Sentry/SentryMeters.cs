namespace Sentry;

/// <summary>
/// Sentry metrics that can be added to
/// <see cref="ExperimentalMetricsOptions.CaptureSystemDiagnosticsMeters"/>
/// </summary>
public static partial class SentryMeters
{
    private const string BackgroundWorkerPattern = @"^Sentry\.Internal\.Backgroundworker$";

    /// <summary>
    /// Matches the built in <see cref="System.Net.Http"/> metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly SubstringOrRegexPattern BackgroundWorker = SystemNetHttpRegex();

    [GeneratedRegex(BackgroundWorkerPattern, RegexOptions.Compiled)]
    private static partial Regex SystemNetHttpRegex();
#else
    public static readonly SubstringOrRegexPattern BackgroundWorker = new Regex(BackgroundWorkerPattern, RegexOptions.Compiled);
#endif

    private static readonly Lazy<IList<SubstringOrRegexPattern>> LazyAll = new(() => new List<SubstringOrRegexPattern>
    {
        BackgroundWorker,
    });

    /// <summary>
    /// Matches all built in metrics
    /// </summary>
    /// <returns></returns>
    public static IList<SubstringOrRegexPattern> All => LazyAll.Value;
}
