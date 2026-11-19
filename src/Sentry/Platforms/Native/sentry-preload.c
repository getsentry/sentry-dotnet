extern void sentry__backend_preload(void);

// Runs before the .NET runtime installs its signal handlers. The runtime then handles managed
// faults, such as NullReferenceException, and passes native crashes on to sentry-native.
__attribute__((constructor)) static void
sentry_dotnet_preload(void)
{
    sentry__backend_preload();
}
