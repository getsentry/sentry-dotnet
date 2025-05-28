namespace Sentry.Infrastructure;

internal static class DiagnosticId
{
#if NET5_0_OR_GREATER
    /// <summary>
    /// Indicates that the feature is experimental and may be subject to change or removal in future versions.
    /// </summary>
    internal const string ExperimentalFeature = "SENTRY0001";
#endif
}
