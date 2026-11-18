using Sentry.Internal.DataCollection;

namespace Sentry.EntityFramework;

internal static class DbCommandExtensions
{
    internal static SqlDialect GetSqlDialect(this DbCommand command) => SqlDialectForNamespace(command.GetType().Namespace);

    internal static SqlDialect SqlDialectForNamespace(string? commandNamespace) => commandNamespace switch
    {
        "System.Data.SqlClient" or "Microsoft.Data.SqlClient" or "System.Data.SqlServerCe" => SqlDialect.SqlServer,
        "MySql.Data.MySqlClient" or "MySqlConnector" or "Devart.Data.MySql" => SqlDialect.MySql,
        "System.Data.SQLite" or "Microsoft.Data.Sqlite" or "Devart.Data.SQLite" => SqlDialect.Sqlite,
        _ => SqlDialect.Generic
    };
}
