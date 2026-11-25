extern void sentry__backend_preload(void);

// installs sentry-native's signal handlers before the .NET runtime
__attribute__((constructor)) static void
sentry_dotnet_preload(void)
{
    sentry__backend_preload();
}
