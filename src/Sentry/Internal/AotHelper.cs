namespace Sentry.Internal;

internal static class AotHelper
{
    internal const string SuppressionJustification = "Non-trimmable code is avoided at runtime";

    private class AotTester
    {
        public void Test() { }
    }

#if NET8_0_OR_GREATER
    // TODO this probably more closely represents trimming rather than NativeAOT?
    internal static bool IsNativeAot { get; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = AotHelper.SuppressionJustification)]
    static AotHelper()
    {
        var stackTrace = new StackTrace(false);
        IsNativeAot = stackTrace.GetFrame(0)?.GetMethod() is null;
    }
#else
    // This is a compile-time const so that the irrelevant code is removed during compilation.
    internal const bool IsNativeAot = false;
#endif
}
