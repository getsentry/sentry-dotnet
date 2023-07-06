namespace Sentry.Internal.DiagnosticSource;

/// <summary>
/// Open Telemetry Keys
/// https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/database.md
/// </summary>
internal static class OTelKeys
{
    internal const string DbName = "db.name";
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
