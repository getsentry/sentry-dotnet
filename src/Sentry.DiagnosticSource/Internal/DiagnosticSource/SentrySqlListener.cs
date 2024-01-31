using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Internal.DiagnosticSource;

internal class SentrySqlListener : IObserver<KeyValuePair<string, object?>>
{
    private enum SentrySqlSpanType
    {
        Connection,
        Execution
    };

    internal const string SqlDataWriteConnectionOpenBeforeCommand = "System.Data.SqlClient.WriteConnectionOpenBefore";
    internal const string SqlMicrosoftWriteConnectionOpenBeforeCommand = "Microsoft.Data.SqlClient.WriteConnectionOpenBefore";

    internal const string SqlMicrosoftWriteConnectionOpenAfterCommand = "Microsoft.Data.SqlClient.WriteConnectionOpenAfter";
    internal const string SqlDataWriteConnectionOpenAfterCommand = "System.Data.SqlClient.WriteConnectionOpenAfter";

    internal const string SqlMicrosoftWriteConnectionCloseAfterCommand = "Microsoft.Data.SqlClient.WriteConnectionCloseAfter";
    internal const string SqlDataWriteConnectionCloseAfterCommand = "System.Data.SqlClient.WriteConnectionCloseAfter";

    internal const string SqlDataBeforeExecuteCommand = "System.Data.SqlClient.WriteCommandBefore";
    internal const string SqlMicrosoftBeforeExecuteCommand = "Microsoft.Data.SqlClient.WriteCommandBefore";

    internal const string SqlDataAfterExecuteCommand = "System.Data.SqlClient.WriteCommandAfter";
    internal const string SqlMicrosoftAfterExecuteCommand = "Microsoft.Data.SqlClient.WriteCommandAfter";

    internal const string SqlDataWriteCommandError = "System.Data.SqlClient.WriteCommandError";
    internal const string SqlMicrosoftWriteCommandError = "Microsoft.Data.SqlClient.WriteCommandError";

    private readonly IHub _hub;
    private readonly SentryOptions _options;

    public SentrySqlListener(IHub hub, SentryOptions options)
    {
        _hub = hub;
        _options = options;
    }

    private static void SetDatabaseName(ISpan span, string databaseName)
    {
        Debug.Assert(databaseName != string.Empty);

        span.SetExtra(OTelKeys.DbName, databaseName);
    }

    private static void SetDatabaseAddress(ISpan span, string databaseAddress)
    {
        Debug.Assert(databaseAddress != string.Empty);

        span.SetExtra(OTelKeys.DbServer, databaseAddress);
    }

    private static void SetConnectionId(ISpan span, Guid? connectionId)
    {
        Debug.Assert(connectionId != Guid.Empty);

        span.SetExtra(SqlKeys.DbConnectionId, connectionId);
    }

    private static void SetOperationId(ISpan span, Guid? operationId)
    {
        Debug.Assert(operationId != Guid.Empty);

        span.SetExtra(SqlKeys.DbOperationId, operationId);
    }

    private static Guid? TryGetOperationId(ISpan span) => span.Extra.TryGetValue<string, Guid?>(SqlKeys.DbOperationId);

    private static Guid? TryGetConnectionId(ISpan span) => span.Extra.TryGetValue<string, Guid?>(SqlKeys.DbConnectionId);

    private void AddSpan(string operation, object? value)
    {
        var transaction = _hub.GetTransactionIfSampled();
        if (transaction == null)
        {
            return;
        }

        var parent = transaction.GetDbParentSpan();
        var span = parent.StartChild(operation);
        span.SetExtra(OTelKeys.DbSystem, "sql");
        SetOperationId(span, value?.GetGuidProperty("OperationId"));
        SetConnectionId(span, value?.GetGuidProperty("ConnectionId"));
    }

    private ISpan? GetSpan(SentrySqlSpanType type, object? value)
    {
        var transaction = _hub.GetTransactionIfSampled();
        if (transaction == null)
        {
            return null;
        }

        switch (type)
        {
            case SentrySqlSpanType.Execution:
                var operationId = value?.GetGuidProperty("OperationId");
                if (TryGetQuerySpan(transaction, operationId) is { } querySpan)
                {
                    return querySpan;
                }

                _options.LogWarning(
                    "Trying to get an execution span with operation id {0}, but it was not found.",
                    operationId);
                return null;

            case SentrySqlSpanType.Connection:
                var connectionId = value?.GetGuidProperty("ConnectionId");
                if (TryGetConnectionSpan(transaction, connectionId) is { } connectionSpan)
                {
                    return connectionSpan;
                }

                _options.LogWarning(
                    "Trying to get a connection span with connection id {0}, but it was not found.",
                    connectionId);
                return null;

            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private static ISpan? TryGetConnectionSpan(ITransactionTracer transaction, Guid? connectionId) =>
        connectionId == null
            ? null
            : transaction.Spans
                .FirstOrDefault(span =>
                    span is { IsFinished: false, Operation: "db.connection" } &&
                    TryGetConnectionId(span) == connectionId);

    private static ISpan? TryGetQuerySpan(ITransactionTracer transaction, Guid? operationId) =>
        operationId == null
            ? null
            : transaction.Spans.FirstOrDefault(span => TryGetOperationId(span) == operationId);

    private void UpdateConnectionSpan(object? value)
    {
        if (value == null)
        {
            return;
        }

        var transaction = _hub.GetTransactionIfSampled();
        if (transaction == null)
        {
            return;
        }

        var operationId = value.GetGuidProperty("OperationId");
        if (operationId == null)
        {
            return;
        }

        var spans = transaction.Spans.Where(span => span.Operation is "db.connection").ToList();
        if (spans.Find(span => !span.IsFinished && TryGetOperationId(span) == operationId) is { } connectionSpan)
        {
            if (value.GetGuidProperty("ConnectionId") is { } connectionId)
            {
                SetConnectionId(connectionSpan, connectionId);
            }

            if (value.GetStringProperty("Connection.Database") is { } dbName)
            {
                connectionSpan.Description = dbName;
                SetDatabaseName(connectionSpan, dbName);
            }

            if (value.GetStringProperty("Connection.DataSource") is { } dbSource)
            {
                SetDatabaseAddress(connectionSpan, dbSource);
            }
        }
    }

    private void FinishCommandSpan(object? value, SpanStatus spanStatus)
    {
        if (value == null)
        {
            return;
        }


        if (GetSpan(SentrySqlSpanType.Execution, value) is not { } commandSpan)
        {
            return;
        }

        // Try to lookup the associated connection span so that we can store the db.name in
        // the command span as well. This will be easier for users to read/identify than the
        // ConnectionId (which is a Guid)
        var connectionId = value.GetGuidProperty("ConnectionId");
        var transaction = _hub.GetTransactionIfSampled();
        if (TryGetConnectionSpan(transaction!, connectionId) is { } connectionSpan)
        {
            if (connectionSpan.Extra.TryGetValue<string, string>(OTelKeys.DbName) is { } dbName)
            {
                SetDatabaseName(commandSpan, dbName);
            }
        }

        commandSpan.Description = value.GetStringProperty("Command.CommandText");
        commandSpan.Finish(spanStatus);
    }

    public void OnCompleted() { }

    public void OnError(Exception error) { }

    public void OnNext(KeyValuePair<string, object?> kvp)
    {
        try
        {
            switch (kvp.Key)
            {
                // Query
                case SqlMicrosoftBeforeExecuteCommand or SqlDataBeforeExecuteCommand:
                    AddSpan("db.query", kvp.Value);
                    return;
                case SqlMicrosoftAfterExecuteCommand or SqlDataAfterExecuteCommand:
                    FinishCommandSpan(kvp.Value, SpanStatus.Ok);
                    return;
                case SqlMicrosoftWriteCommandError or SqlDataWriteCommandError:
                    FinishCommandSpan(kvp.Value, SpanStatus.InternalError);
                    return;

                // Connection
                case SqlMicrosoftWriteConnectionOpenBeforeCommand or SqlDataWriteConnectionOpenBeforeCommand:
                    AddSpan("db.connection", kvp.Value);
                    return;
                case SqlMicrosoftWriteConnectionOpenAfterCommand or SqlDataWriteConnectionOpenAfterCommand:
                    UpdateConnectionSpan(kvp.Value);
                    return;
                case SqlMicrosoftWriteConnectionCloseAfterCommand or SqlDataWriteConnectionCloseAfterCommand
                    when GetSpan(SentrySqlSpanType.Connection, kvp.Value) is { } closeSpan:
                    TrySetConnectionStatistics(closeSpan, kvp.Value);
                    closeSpan.Finish(SpanStatus.Ok);
                    return;
            }
        }
        catch (Exception ex)
        {
            _options.LogError(ex, "Failed to intercept SQL event.");
        }
    }

    private static void TrySetConnectionStatistics(ISpan span, object? value)
    {
        if (value?.GetProperty("Statistics") is not Dictionary<object, object> statistics)
        {
            return;
        }

        if (statistics["SelectRows"] is long selectRows)
        {
            span.SetExtra("rows_sent", selectRows);
        }

        if (statistics["BytesReceived"] is long bytesReceived)
        {
            span.SetExtra("bytes_received", bytesReceived);
        }

        if (statistics["BytesSent"] is long bytesSent)
        {
            span.SetExtra("bytes_sent ", bytesSent);
        }
    }
}
