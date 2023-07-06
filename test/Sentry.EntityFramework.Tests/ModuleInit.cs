public static class ModuleInit
{
    [ModuleInitializer]
    public static void Init() =>
        EffortProviderConfiguration.RegisterProvider();
}
