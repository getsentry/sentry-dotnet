namespace Sentry.DiagnosticSource.IntegrationTests;

public sealed class LocalDbFixture : IDisposable
{
    public SqlInstance SqlInstance { get; }

    public static string InstanceName =>
#if NETFRAMEWORK
        "SqlListenerTests4";
#elif NET10_0
        "SqlListenerTests10";
#elif NET11_0
        "SqlListenerTests11";
#elif NET9_0
        "SqlListenerTests9";
#elif NET8_0
        "SqlListenerTests8";
#else
#error Needs a version specific name to prevent the tests from tripping over one another when running in parallel
#endif

    public LocalDbFixture()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        SqlInstance = new(
            name: InstanceName,
            buildTemplate: TestDbBuilder.CreateTableAsync);
    }

    public void Dispose()
    {
        if (BuildServerDetector.Detected)
        {
            SqlInstance?.Cleanup();
        }
    }
}
