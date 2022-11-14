#if NET6_0_OR_GREATER

using System.Runtime.CompilerServices;

public static class ModuleInit
{
    [ModuleInitializer]
    public static void Init() =>
        VerifyHttp.Enable();
}

#endif
