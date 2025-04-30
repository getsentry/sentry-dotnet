namespace Sentry.Infrastructure;

internal static class DiagnosticId
{
#if NET5_0_OR_GREATER
    /// <summary>
    /// Indicates that the feature is experimental and may be subject to change or removal in future versions.
    /// </summary>
    internal const string ExperimentalFeature = "SENTRY0001";
#endif

    //TODO: QUESTION: Should we re-use the above for all [Experimental] features or have one ID per experimental feature?
    internal const string ExperimentalSentryLogs = "SENTRY0002";
}

//TODO: not sure about this type name
internal static class UrlFormats
{
    internal const string ExperimentalSentryLogs = "https://github.com/getsentry/sentry-dotnet/issues/4132";
}
