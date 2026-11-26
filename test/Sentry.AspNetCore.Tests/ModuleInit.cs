namespace Sentry.AspNetCore.Tests;

public static class ModuleInit
{
    [ModuleInitializer]
    public static void Init() => VerifyHttp.Initialize();
}
