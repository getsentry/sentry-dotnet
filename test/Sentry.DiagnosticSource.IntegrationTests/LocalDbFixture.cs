using System.Runtime.InteropServices;
using DiffEngine;
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

        SqlInstance = new(
            name: "SqlListenerTests" + Namer.RuntimeAndVersion,
            buildTemplate: TestDbBuilder.CreateTable);
    }

    public void Dispose()
    {
        if (BuildServerDetector.Detected)
        {
            SqlInstance?.Cleanup();
        }
    }
}
