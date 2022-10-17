using System.Runtime.CompilerServices;

public static class ModuleInit
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifyNSubstitute.Enable();
        VerifyDiffPlex.Initialize();
    }
}
