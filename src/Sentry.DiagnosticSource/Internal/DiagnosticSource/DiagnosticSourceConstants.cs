namespace Sentry.Internal.DiagnosticSource;

/// <summary>
/// Open Telemetry Keys
/// https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/database.md
/// </summary>
internal static class OTelKeys
{
    internal const string DbName = "db.name";
    internal const string DbSystem = "db.system";
    internal const string DbServer = "db.server";
}

/// <summary>
/// Keys specific to the SqlClient listener
/// </summary>
internal static class SqlKeys
{
    internal const string DbConnectionId = "db.connection_id";
    internal const string DbOperationId = "db.operation_id";
}

/// <summary>
/// Keys specific to the Entity Framework listener
/// </summary>
internal static class EFKeys
{
    // Entity Framework specific
    internal const string DbConnectionId = "db.connection_id";
    internal const string DbCommandId = "db.command_id";
}

/// <summary>
/// Mapping of Database Providers to known Open Telemetry db.system
/// https://learn.microsoft.com/en-us/ef/core/providers/?tabs=dotnet-core-cli
/// https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/database.md#notes-and-well-known-identifiers-for-dbsystem
/// </summary>
internal static class DatabaseProviderSystems
{
    public static readonly Dictionary<string, string> ProviderSystems = new()
    {
        { "Microsoft.EntityFrameworkCore.SqlServer", "mssql" },
        { "Microsoft.EntityFrameworkCore.Sqlite", "sqlite" },
        { "Microsoft.EntityFrameworkCore.InMemory", "inmemory" },
        { "Microsoft.EntityFrameworkCore.Cosmos", "cosmosdb" },
        { "Npgsql.EntityFrameworkCore.PostgreSQL", "postgresql" },
        { "Pomelo.EntityFrameworkCore.MySql", "mysql" },
        { "MySql.EntityFrameworkCore", "mysql" },
        { "Oracle.EntityFrameworkCore", "oracle" },
        { "Devart.Data.MySql.EFCore", "mysql" },
        { "Devart.Data.Oracle.EFCore", "oracle" },
        { "Devart.Data.PostgreSql.EFCore", "postgres" },
        { "Devart.Data.SQLite.EFCore", "sqlite" },
        { "InterBase", "interbase" },
        { "FirebirdSql.EntityFrameworkCore.Firebird", "firebird" },
        { "IBM.EntityFrameworkCore", "db2" },
        { "IBM.EntityFrameworkCore-lnx", "db2" },
        { "IBM.EntityFrameworkCore-osx", "db2" },
        { "EntityFrameworkCore.Jet", "accessfiles" },
        { "Google.Cloud.EntityFrameworkCore.Spanner", "spanner" },
        { "Teradata.EntityFrameworkCore", "teradata" },
        { "FileContextCore", "datainfiles" },
        { "FileBaseContext", "tablesinfiles" },
        { "EntityFrameworkCore.SqlServerCompact35", "mssqlcompact" },
        { "EntityFrameworkCore.SqlServerCompact40", "mssqlcompact" },
        { "EntityFrameworkCore.OpenEdge", "progress" },
    };
}
