#if NET6_0_OR_GREATER
namespace Sentry.DiagnosticSource.IntegrationTests;

public static class ModuleInit
{
    [ModuleInitializer]
    public static void Init() => VerifyEntityFramework.Enable();
}
#endif
