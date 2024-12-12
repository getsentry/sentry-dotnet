using Sentry.Protocol;

namespace Sentry.Internal;

internal static class AotHelper
{
    internal const string AvoidAtRuntime = "Non-trimmable code is avoided at runtime";

    internal static bool IsTrimmed { get; }
    internal static bool IsDynamicCodeSupported { get; }
    internal static bool IsNativeAot { get; }

    static AotHelper()
    {
        IsTrimmed = CheckIsTrimmed();
#if NETSTANDARD2_0 || NETFRAMEWORK
        IsDynamicCodeSupported = true;
#else
        IsDynamicCodeSupported = RuntimeFeature.IsDynamicCodeSupported;
#endif
        // This is our best guess at determining whether AOT is enabled
        IsNativeAot = IsTrimmed && !IsDynamicCodeSupported;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026: RequiresUnreferencedCode", Justification = AvoidAtRuntime)]
    private static bool CheckIsTrimmed()
    {
        var stackTrace = new StackTrace(false);
        return stackTrace.GetFrame(0)?.GetMethod() is null;
    }
}
