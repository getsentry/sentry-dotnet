#if NET8_0_OR_GREATER
using System.Diagnostics.Metrics;

namespace Sentry;

/// <summary>
/// Sentry metrics that can be added to
/// <see cref="ExperimentalMetricsOptions.CaptureSystemDiagnosticsMeters"/>
/// </summary>
public static partial class SentryMeters
{
    private const string BlockingDetectorPattern = @"^Sentry\.Ben\.BlockingDetector$";

    private static readonly Lazy<Meter> LazyBlockingDetectorMeter = new(() =>
        new Meter("Sentry.Ben.BlockingDetector"));
    internal static Meter BlockingDetectorMeter => LazyBlockingDetectorMeter.Value;

    /// <summary>
    /// Matches internal metrics emitted by the Sentry Blocking Detector
    /// </summary>
    public static readonly SubstringOrRegexPattern BlockingDetector = BlockingDetectorRegex();

    [GeneratedRegex(BlockingDetectorPattern, RegexOptions.Compiled)]
    private static partial Regex BlockingDetectorRegex();

    private static readonly Lazy<IList<SubstringOrRegexPattern>> LazyAll = new(() => new List<SubstringOrRegexPattern>
    {
        BlockingDetector,
    });

    /// <summary>
    /// Matches all built in metrics
    /// </summary>
    /// <returns></returns>
    public static IList<SubstringOrRegexPattern> All => LazyAll.Value;
}
#endif
