#if NET6_0_OR_GREATER
namespace Sentry.Serilog.Tests;

public static class ModuleInit
{
    [ModuleInitializer]
    public static void Init()
    {
        // We just need to call something on the referenced project to force its init to run.
        _ = typeof(Sentry.AspNetCore.Tests.ModuleInit).Assembly;
    }
}
#endif
