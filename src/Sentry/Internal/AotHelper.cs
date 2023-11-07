using Sentry.Internal.Extensions;

namespace Sentry.Internal;

internal static class AotHelper
{
    internal const string SuppressionJustification = "Non-trimmable code is avoided at runtime";

    private class AotTester
    {
        public void Test() { }
    }

    internal static bool IsAot { get; }

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = SuppressionJustification)]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = SuppressionJustification)]
    static AotHelper()
    {
#if NET6_0_OR_GREATER   // TODO NET7 once we target it
        try
        {
            var _ = JsonSerializer.Serialize(
                new { a = "1" },
                JsonExtensions.SerializerOptions
            );
            IsAot = false;
        }
        catch
        {
            IsAot = true;
        }
#else
        IsAot = false;
#endif
    }
}
