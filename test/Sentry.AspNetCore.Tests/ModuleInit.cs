#if NET6_0

using System.Runtime.CompilerServices;

public static class ModuleInit
{
    [ModuleInitializer]
    public static void Init() =>
        VerifyHttp.Enable();
}

#endif
