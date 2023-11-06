namespace Sentry.Internal;

internal static class AotHelper
{
    internal const string SuppressionJustification = "Non-trimmable code is avoided at runtime";

    private class AotTester
    {
        public void Test() { }
    }

    internal static bool IsAot { get; }

    static AotHelper()
    {
        IsAot = false;
#if NET6_0_OR_GREATER   // TODO NET7 once we target it
        var stackTrace = new StackTrace(false);
        IsAot = stackTrace.GetFrame(0)?.GetMethod() is null;
#endif
    }
}
