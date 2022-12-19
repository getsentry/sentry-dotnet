#if NET6_0_OR_GREATER
namespace Sentry.Google.Cloud.Functions.Tests;

public static class ModuleInit
{
    [ModuleInitializer]
    public static void Init() =>
        VerifyHttp.Enable();
}
#endif
