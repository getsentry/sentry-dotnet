namespace Sentry.EntityFramework.Tests;

public class DbCommandExtensionsTests
{
    [Theory]
    [InlineData(typeof(Microsoft.Data.SqlClient.FakeSqlCommand), "SqlServer")]
    [InlineData(typeof(MySql.Data.MySqlClient.FakeMySqlCommand), "MySql")]
    [InlineData(typeof(System.Data.SQLite.FakeSQLiteCommand), "Sqlite")]
    [InlineData(typeof(EffortCommand), "Generic")]
    public void GetSqlDialect_ProviderCommand_MapsNamespaceToDialect(Type commandType, string expectedDialect)
    {
        var command = (DbCommand)Activator.CreateInstance(commandType)!;

        command.GetSqlDialect().ToString().Should().Be(expectedDialect);
    }
}
