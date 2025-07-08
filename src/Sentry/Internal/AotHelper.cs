using Sentry.CompilerServices;

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
        if (BuildProperties.TryGetBoolean("PublishTrimmed", out var trimmed))
        {
            return trimmed;
        }

        if (BuildProperties.TryGetBoolean("PublishAot", out var aot))
        {
            return aot;
        }

        // fallback check
        var stackTrace = new StackTrace(false);
        return stackTrace.GetFrame(0)?.GetMethod() is null;
    }
}
