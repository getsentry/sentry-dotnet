#if !__MOBILE__
using System.Runtime.CompilerServices;

public static class ModuleInit
{
    [ModuleInitializer]
    public static void Init() =>
        VerifyDiffPlex.Initialize();
}
#endif
