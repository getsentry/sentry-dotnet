#if NET6_0_OR_GREATER
public static class ModuleInit
{
    [ModuleInitializer]
    public static void Init() => VerifyEntityFramework.Enable();
}
#endif
