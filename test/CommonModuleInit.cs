#if !__MOBILE__
public static class CommonModuleInit
{
    [ModuleInitializer]
    public static void Init() =>
        VerifyDiffPlex.Initialize();
}
#endif
