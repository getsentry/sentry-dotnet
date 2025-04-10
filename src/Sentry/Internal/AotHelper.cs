namespace Sentry.Internal;

internal static class AotHelper
{
    internal const string AvoidAtRuntime = "Non-trimmable code is avoided at runtime";

    internal static bool IsTrimmed { get; }

    static AotHelper()
    {
        IsTrimmed = CheckIsTrimmed();
    }


    [UnconditionalSuppressMessage("Trimming", "IL2026: RequiresUnreferencedCode", Justification = AvoidAtRuntime)]
    private static bool CheckIsTrimmed()
    {
        if (Check("publishtrimmed"))
            return true;

        if (Check("publishaot"))
            return true;

        // fallback check
        var stackTrace = new StackTrace(false);
        return stackTrace.GetFrame(0)?.GetMethod() is null;
    }

    private static bool Check(string key)
    {
        if (SentrySdk.BuildVariables?.TryGetValue(key, out var aotValue) ?? false)
        {
            if (bool.TryParse(aotValue, out var result))
            {
                return result;
            }
        }

        return false;
    }
}
