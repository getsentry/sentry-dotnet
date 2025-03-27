namespace Sentry.Android.AssemblyReader;

internal abstract class AndroidAssemblyReader : IDisposable
{
    protected DebugLogger? Logger { get; }
    protected ZipArchive ZipArchive { get; }
    protected IList<string> SupportedAbis { get; }

    protected AndroidAssemblyReader(ZipArchive zip, IList<string> supportedAbis, DebugLogger? logger)
    {
        ZipArchive = zip;
        Logger = logger;
        SupportedAbis = supportedAbis;
    }

    public void Dispose()
    {
        ZipArchive.Dispose();
    }
}
