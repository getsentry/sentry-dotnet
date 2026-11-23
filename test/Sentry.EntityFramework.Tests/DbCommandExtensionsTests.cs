namespace Sentry.EntityFramework.Tests;

public class DbCommandExtensionsTests
{
    [Theory]
    [InlineData("System.Data.SqlClient", "mssql")]
    [InlineData("Microsoft.Data.SqlClient", "mssql")]
    [InlineData("System.Data.SqlServerCe", "mssqlcompact")]
    [InlineData("MySql.Data.MySqlClient", "mysql")]
    [InlineData("MySqlConnector", "mysql")]
    [InlineData("Devart.Data.MySql", "mysql")]
    [InlineData("System.Data.SQLite", "sqlite")]
    [InlineData("Microsoft.Data.Sqlite", "sqlite")]
    [InlineData("Devart.Data.SQLite", "sqlite")]
    [InlineData("Npgsql", null)]
    [InlineData("Effort.Provider", null)]
    [InlineData(null, null)]
    public void DbSystemForNamespace_ProviderNamespace_MapsToDbSystem(string commandNamespace, string expectedDbSystem) =>
        DbCommandExtensions.DbSystemForNamespace(commandNamespace).Should().Be(expectedDbSystem);

    [Fact]
    public void GetDbSystem_Command_UsesItsTypeNamespace()
    {
        using (new AssertionScope())
        {
            new MySql.Data.MySqlClient.FakeMySqlCommand().GetDbSystem().Should().Be("mysql");
            new EffortCommand().GetDbSystem().Should().BeNull();
        }
    }
}
