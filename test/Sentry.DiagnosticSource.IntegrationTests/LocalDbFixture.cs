using LocalDb;

public sealed class LocalDbFixture : IDisposable
{
    public SqlInstance SqlInstance { get; }

    public LocalDbFixture()
    {
        SqlInstance = new SqlInstance(
            name: "SqlListenerTests" + Namer.RuntimeAndVersion,
            buildTemplate: TestDbBuilder.CreateTable);
    }

    public void Dispose()
    {
        SqlInstance.Cleanup();
    }
}
