namespace Sentry.DiagnosticSource.IntegrationTests;

public sealed class LocalDbFixture : IDisposable
{
    public SqlInstance SqlInstance { get; }

    public static string InstanceName =>
#if NETFRAMEWORK
        "SqlListenerTests4";
#elif NET6_0
        "SqlListenerTests6";
#elif NET7_0
        "SqlListenerTests7";
#elif NET8_0
        "SqlListenerTests8";
#elif NET9_0
        "SqlListenerTests9";
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
