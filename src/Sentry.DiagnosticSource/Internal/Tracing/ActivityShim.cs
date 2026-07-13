// Alias required because Android TFMs of the core Sentry package otherwise see an ambiguous reference
// between System.Diagnostics.Activity (global using) and Android.App.Activity (Android implicit usings).
using Activity = System.Diagnostics.Activity;

namespace Sentry.Internal.Tracing;

/// <summary>
/// The ActivitySource used for Activities created by the Sentry tracing shim (i.e. when users call
/// SentrySdk.StartTransaction / ISpan.StartChild and Activity-based tracing is enabled).
/// </summary>
internal static class SentryActivitySources
{
    public const string ShimSourceName = "Sentry";

    public static readonly ActivitySource Shim = new(ShimSourceName);
}

/// <summary>
/// Keys for side-channel values fused onto an <see cref="Activity"/> by the shim, read back by
/// <see cref="SentryActivityProcessor"/>. These carry Sentry concepts that have no native representation
/// on an Activity (rich span status, custom sampling context, an inbound dynamic sampling context).
/// </summary>
internal static class ShimKeys
{
    public const string SpanStatus = "sentry.shim.status";
    public const string CustomSamplingContext = "sentry.shim.custom_sampling_context";
}
