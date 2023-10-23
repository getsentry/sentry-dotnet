public static class CommonModuleInit
{
    [ModuleInitializer]
    public static void Init() =>
        VerifyDiffPlex.Initialize();
}
