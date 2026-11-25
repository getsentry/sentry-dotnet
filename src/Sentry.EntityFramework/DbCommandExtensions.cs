namespace Sentry.EntityFramework;

internal static class DbCommandExtensions
{
    internal static string? GetDbSystem(this DbCommand command) => DbSystemForNamespace(command.GetType().Namespace);

    internal static string? DbSystemForNamespace(string? commandNamespace) => commandNamespace switch
    {
        "System.Data.SqlClient" or "Microsoft.Data.SqlClient" => "mssql",
        "System.Data.SqlServerCe" => "mssqlcompact",
        "MySql.Data.MySqlClient" or "MySqlConnector" or "Devart.Data.MySql" => "mysql",
        "System.Data.SQLite" or "Microsoft.Data.Sqlite" or "Devart.Data.SQLite" => "sqlite",
        _ => null
    };
}
