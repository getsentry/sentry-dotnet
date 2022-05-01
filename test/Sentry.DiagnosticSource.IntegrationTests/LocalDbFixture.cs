using System.Runtime.InteropServices;
using LocalDb;

public sealed class LocalDbFixture : IDisposable
{
    public SqlInstance SqlInstance { get; }

    public LocalDbFixture()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        SqlInstance = new SqlInstance(
            name: "SqlListenerTests" + Namer.RuntimeAndVersion,
            buildTemplate: TestDbBuilder.CreateTable);
    }

    public void Dispose()
    {
        SqlInstance?.Cleanup();
    }
}
