namespace Sentry.Internal;

internal static class AotHelper
{
    internal const string SuppressionJustification = "Non-trimmable code is avoided at runtime";

    private class AotTester
    {
        public void Test() { }
    }

    internal static bool IsAot { get; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = AotHelper.SuppressionJustification)]
    static AotHelper()
    {
#if NET6_0_OR_GREATER   // TODO NET7 once we target it
        var stackTrace = new StackTrace(false);
        IsAot = stackTrace.GetFrame(0)?.GetMethod() is null;
#else
        IsAot = false;
#endif
    }
}
