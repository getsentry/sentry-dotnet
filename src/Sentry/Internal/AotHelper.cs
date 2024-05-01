namespace Sentry.Internal;

/// <summary>
/// Helper class to detect if the application has been compiled Ahead of Time (AOT)
/// either for UWP .NET Native or on .NET 8.0+
/// </summary>
internal static class AotHelper
{
    internal const string SuppressionJustification = "Non-trimmable code is avoided at runtime";

#if NETSTANDARD
    internal const bool IsNativeAot = false;
    internal static bool IsDotNetNative { get; }

    static AotHelper()
    {
        var stackTrace = new StackTrace(false);
        IsDotNetNative = stackTrace.GetFrame(0)?.GetMethod() is null;
    }
#elif NET8_0_OR_GREATER
    internal static bool IsNativeAot { get; }
    internal const bool IsDotNetNative = false;

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = AotHelper.SuppressionJustification)]
    static AotHelper()
    {
        var stackTrace = new StackTrace(false);
        IsNativeAot = stackTrace.GetFrame(0)?.GetMethod() is null;
    }
#else
    internal const bool IsNativeAot = false;
    internal const bool IsDotNetNative = false;
#endif
}
