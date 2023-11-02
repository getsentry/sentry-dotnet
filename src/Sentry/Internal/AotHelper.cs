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
        try
        {
            // GetMethod should throw an exception if Trimming is enabled
            var type = typeof(AotTester);
            _ = type.GetMethod(nameof(AotTester.Test));
        }
        catch
        {
            IsAot = true;
        }
        IsAot = false;
    }
}
