#if NET6_0_OR_GREATER
namespace Sentry.AspNetCore.Tests;

public static class ModuleInit
{
    private static volatile bool Initialized;

    [ModuleInitializer]
    public static void Init()
    {
        if (!Initialized)
        {
            Initialized = true;
            VerifyHttp.Enable();
        }
    }
}
#endif
