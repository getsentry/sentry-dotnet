#if !__MOBILE__
public static class ModuleInit
{
    [ModuleInitializer]
    public static void Init() =>
        VerifyDiffPlex.Initialize();
}
#endif
