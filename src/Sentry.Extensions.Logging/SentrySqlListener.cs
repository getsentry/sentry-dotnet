using System;
using System.Collections.Generic;
using System.Linq;
using Sentry.Extensibility;
using Sentry.Extensions.Logging.Extensions;

namespace Sentry.Extensions.Logging
{
    internal class SentrySqlListener : IObserver<KeyValuePair<string, object?>>
    {
        private enum SentrySqlSpanType
        {
            Connection,
            Execution
        };

        internal const string OperationKey = "OperationId";
        internal const string ConnectionKey = "ConnectionId";
        internal const string ConnectionExtraKey = "db.connection_id";
        internal const string OperationExtraKey = "db.operation_id";

        internal const string SqlDataWriteConnectionOpenBeforeCommand = "System.Data.SqlClient.WriteConnectionOpenBefore";
        internal const string SqlMicrosoftWriteConnectionOpenBeforeCommand = "Microsoft.Data.SqlClient.WriteConnectionOpenBefore";

        internal const string SqlDataWriteConnectionCloseBeforeCommand = "System.Data.SqlClient.WriteConnectionCloseBefore";
        internal const string SqlMicrosoftWriteConnectionCloseBeforeCommand = "Microsoft.Data.SqlClient.WriteConnectionCloseBefore";

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

        private IHub _hub { get; }
        private SentryOptions _options { get; }
        public SentrySqlListener(IHub hub, SentryOptions options)
        {
            _hub = hub;
            _options = options;
        }

        private void AddSpan(SentrySqlSpanType type, string operation, string? description, Guid operationId, Guid? connectionId = null)
        {
            _hub.WithScope(scope =>
            {
                if (type == SentrySqlSpanType.Connection &&
                    scope.Transaction?.StartChild(operation, description) is { } connectionSpan)
                {
                    connectionSpan.SetExtra(OperationExtraKey, operationId);
                    // ConnectionId is set afterwards.
                }
                else if (type == SentrySqlSpanType.Execution &&
                    connectionId != null &&
                    TryGetConnectionSpan(scope, connectionId.Value) is { } parentSpan)
                {
                    var span = parentSpan.StartChild(operation, description);
                    span.SetExtra(OperationExtraKey, operationId);
                }
            });
        }

        private ISpan? GetSpan(SentrySqlSpanType type, Guid? operationId = null, Guid? connectionId = null)
        {
            ISpan? span = null;
            _hub.WithScope(scope =>
            {
                if (type == SentrySqlSpanType.Execution &&
                    operationId is { } queryId &&
                    TryGetQuerySpan(scope, queryId) is { } querySpan)
                {
                    span = querySpan;
                }
                else if (type == SentrySqlSpanType.Connection &&
                    connectionId is { } id &&
                    TryGetConnectionSpan(scope, id) is { } connectionSpan)
                {
                    span = connectionSpan;
                }
                else
                {
                    _options.DiagnosticLogger?.LogWarning("Trying to close a span of type {0} with operation id {1}, but it was not found.", type, operationId);
                }
            });
            return span;
        }

        private ISpan? TryGetConnectionSpan(Scope scope, Guid connectionId)
            => scope.Transaction?.Spans.FirstOrDefault(span => TryGetKey(span.Extra, ConnectionExtraKey) is Guid id && id == connectionId);

        private ISpan? TryGetQuerySpan(Scope scope, Guid operationId)
            => scope.Transaction?.Spans.FirstOrDefault(span => TryGetKey(span.Extra, OperationExtraKey) is Guid id && id == operationId);

        private void UpdateConnectionSpan(Guid operationId, Guid connectionId)
        {
            _hub.WithScope(scope =>
            {
                var span = scope.Transaction?.Spans.FirstOrDefault(span => TryGetKey(span.Extra, OperationExtraKey) is Guid id && id == operationId);
                span?.SetExtra(ConnectionExtraKey, connectionId);
            });
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            try
            {
                // Query.
                if (value.Key == SqlMicrosoftBeforeExecuteCommand || value.Key == SqlDataBeforeExecuteCommand)
                {
                    AddSpan(SentrySqlSpanType.Execution, "db.query", null, value.GetProperty<Guid>(OperationKey), value.GetProperty<Guid>(ConnectionKey));
                }
                else if ((value.Key == SqlMicrosoftAfterExecuteCommand || value.Key == SqlDataAfterExecuteCommand) &&
                    GetSpan(SentrySqlSpanType.Execution, value.GetProperty<Guid>(OperationKey)) is { } commandSpan)
                {
                    commandSpan.Description = value.GetSubProperty<string>("Command", "CommandText");
                    commandSpan.Finish(SpanStatus.Ok);
                }
                else if ((value.Key == SqlMicrosoftWriteCommandError || value.Key == SqlDataWriteCommandError) &&
                    GetSpan(SentrySqlSpanType.Execution, value.GetProperty<Guid>(OperationKey)) is { } errorSpan)
                {
                    errorSpan.Description = value.GetSubProperty<string>("Command", "CommandText");
                    errorSpan.Finish(SpanStatus.InternalError);
                }

                // Connection.
                else if (value.Key == SqlMicrosoftWriteConnectionOpenBeforeCommand || value.Key == SqlDataWriteConnectionOpenBeforeCommand)
                {
                    AddSpan(SentrySqlSpanType.Connection, "db.connection", null, value.GetProperty<Guid>(OperationKey));
                }
                else if (value.Key == SqlMicrosoftWriteConnectionOpenAfterCommand || value.Key == SqlDataWriteConnectionOpenAfterCommand)
                {
                    UpdateConnectionSpan(value.GetProperty<Guid>(OperationKey), value.GetProperty<Guid>(ConnectionKey));
                }
                else if ((value.Key == SqlMicrosoftWriteConnectionCloseBeforeCommand ||
                          value.Key == SqlMicrosoftWriteConnectionCloseAfterCommand ||
                          value.Key == SqlDataWriteConnectionCloseBeforeCommand ||
                          value.Key == SqlDataWriteConnectionCloseAfterCommand) &&
                    GetSpan(SentrySqlSpanType.Connection, null, value.GetProperty<Guid>(ConnectionKey)) is { } connectionSpan)
                {
                    TrySetConnectionStatistics(connectionSpan, value);
                    connectionSpan.Finish(SpanStatus.Ok);
                }
            }
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.LogError("Failed to intercept SQL event.", ex);
            }
        }

        private void TrySetConnectionStatistics(ISpan span, KeyValuePair<string, object?> value)
        {
            if (value.GetProperty<Dictionary<object, object>>("Statistics") is { } statistics)
            {
                if (statistics["SelectRows"] is long selectRows)
                {
                    span.SetExtra("rows_send", selectRows);
                }
                if (statistics["BytesReceived"] is long bytesReceived)
                {
                    span.SetExtra("bytes_received", bytesReceived);
                }
                if (statistics["BytesSent"] is long bytesSent)
                {
                    span.SetExtra("bytes_send ", bytesSent);
                }
            }
        }

        private static object? TryGetKey(IReadOnlyDictionary<string, object?> dictionary, string key)
        {
            var found = dictionary.TryGetValue(key, out var result);
            return found ? result : null;
        }
    }
}
