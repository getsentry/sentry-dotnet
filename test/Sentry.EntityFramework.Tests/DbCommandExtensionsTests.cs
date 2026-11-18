namespace Sentry.EntityFramework.Tests;

public class DbCommandExtensionsTests
{
    [Theory]
    [InlineData("System.Data.SqlClient", "SqlServer")]
    [InlineData("Microsoft.Data.SqlClient", "SqlServer")]
    [InlineData("System.Data.SqlServerCe", "SqlServer")]
    [InlineData("MySql.Data.MySqlClient", "MySql")]
    [InlineData("MySqlConnector", "MySql")]
    [InlineData("Devart.Data.MySql", "MySql")]
    [InlineData("System.Data.SQLite", "Sqlite")]
    [InlineData("Microsoft.Data.Sqlite", "Sqlite")]
    [InlineData("Devart.Data.SQLite", "Sqlite")]
    [InlineData("Npgsql", "Generic")]
    [InlineData("Effort.Provider", "Generic")]
    [InlineData(null, "Generic")]
    public void SqlDialectForNamespace_ProviderNamespace_MapsToDialect(string commandNamespace, string expectedDialect) =>
        DbCommandExtensions.SqlDialectForNamespace(commandNamespace).ToString().Should().Be(expectedDialect);

    [Fact]
    public void GetSqlDialect_Command_UsesItsTypeNamespace()
    {
        using (new AssertionScope())
        {
            new MySql.Data.MySqlClient.FakeMySqlCommand().GetSqlDialect().ToString().Should().Be("MySql");
            new EffortCommand().GetSqlDialect().ToString().Should().Be("Generic");
        }
    }
}
