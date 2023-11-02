#if !__MOBILE__
public static class CommonModuleInit
{
    [ModuleInitializer]
    [SuppressMessage("Usage", "CA2255:The \'ModuleInitializer\' attribute should not be used in libraries")]
    public static void Init()
    {
        VerifyDiffPlex.Initialize();
    }
}
#endif
