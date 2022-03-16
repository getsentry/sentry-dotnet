using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Internals.DiagnosticSource
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

        internal const string SqlMicrosoftWriteConnectionOpenAfterCommand = "Microsoft.Data.SqlClient.WriteConnectionOpenAfter";
        internal const string SqlDataWriteConnectionOpenAfterCommand = "System.Data.SqlClient.WriteConnectionOpenAfter";

        internal const string SqlMicrosoftWriteConnectionCloseAfterCommand = "Microsoft.Data.SqlClient.WriteConnectionCloseAfter";
        internal const string SqlDataWriteConnectionCloseAfterCommand = "System.Data.SqlClient.WriteConnectionCloseAfter";

        internal const string SqlMicrosoftWriteTransactionCommitAfter = "Microsoft.Data.SqlClient.WriteTransactionCommitAfter";
        internal const string SqlDataWriteTransactionCommitAfter = "System.Data.SqlClient.WriteTransactionCommitAfter";

        internal const string SqlDataBeforeExecuteCommand = "System.Data.SqlClient.WriteCommandBefore";
        internal const string SqlMicrosoftBeforeExecuteCommand = "Microsoft.Data.SqlClient.WriteCommandBefore";

        internal const string SqlDataAfterExecuteCommand = "System.Data.SqlClient.WriteCommandAfter";
        internal const string SqlMicrosoftAfterExecuteCommand = "Microsoft.Data.SqlClient.WriteCommandAfter";

        internal const string SqlDataWriteCommandError = "System.Data.SqlClient.WriteCommandError";
        internal const string SqlMicrosoftWriteCommandError = "Microsoft.Data.SqlClient.WriteCommandError";

        private IHub _hub;
        private SentryOptions _options;

        public SentrySqlListener(IHub hub, SentryOptions options)
        {
            _hub = hub;
            _options = options;
        }

        private static void SetConnectionId(ISpan span, Guid? connectionId)
        {
            Debug.Assert(connectionId != Guid.Empty);

            span.SetExtra(ConnectionExtraKey, connectionId);
        }

        private static void SetOperationId(ISpan span, Guid? operationId)
        {
            Debug.Assert(operationId != Guid.Empty);

            span.SetExtra(OperationExtraKey, operationId);
        }

        private static Guid? TryGetOperationId(ISpan span)
        {
            if (span.Extra.TryGetValue(OperationExtraKey, out var key) && key is Guid guid)
            {
                return guid;
            }
            return null;
        }

        private static Guid? TryGetConnectionId(ISpan span)
        {
            if (span.Extra.TryGetValue(ConnectionExtraKey, out var key) && key is Guid guid)
            {
                return guid;
            }
            return null;
        }

        private void AddSpan(SentrySqlSpanType type, string operation, KeyValuePair<string, object?> value)
        {
            _hub.ConfigureScope(scope =>
            {
                if (scope.Transaction is { } transaction)
                {
                    if (type == SentrySqlSpanType.Connection &&
                        transaction?.StartChild(operation) is { } connectionSpan)
                    {
                        SetOperationId(connectionSpan, value.GetProperty<Guid>(OperationKey));
                    }
                    else if (type == SentrySqlSpanType.Execution && value.GetProperty<Guid>(ConnectionKey) is { } connectionId)
                    {
                        var span = TryStartChild(
                            TryGetConnectionSpan(scope, connectionId) ?? transaction,
                            operation,
                            null);
                        if (span is not null)
                        {
                            SetOperationId(span, value.GetProperty<Guid>(OperationKey));
                            SetConnectionId(span, connectionId);
                        }
                    }
                }
            });
        }

        private ISpan? GetSpan(SentrySqlSpanType type, KeyValuePair<string, object?> value)
        {
            ISpan? span = null;
            _hub.ConfigureScope(scope =>
            {
                if (scope.Transaction == null)
                {
                    return;
                }

                if (type == SentrySqlSpanType.Execution)
                {
                    var operationId = value.GetProperty<Guid>(OperationKey);
                    if (TryGetQuerySpan(scope, operationId) is { } querySpan)
                    {
                        span = querySpan;

                        if (span.ParentSpanId == scope.Transaction?.SpanId &&
                            TryGetConnectionId(span) is { } spanConnectionId &&
                            spanConnectionId is Guid spanConnectionGuid &&
                            span is SpanTracer executionTracer &&
                            TryGetConnectionSpan(scope, spanConnectionGuid) is { } spanConnectionRef)
                        {
                            // Connection Span exist but wasn't set as the parent of the current Span.
                            executionTracer.ParentSpanId = spanConnectionRef.SpanId;
                        }
                    }
                    else
                    {
                        _options.DiagnosticLogger?.LogWarning("Trying to get a span of type {0} with operation id {1}, but it was not found.",
                            type,
                            operationId);
                    }
                }
                else if ((value.Key == SqlMicrosoftWriteConnectionCloseAfterCommand ||
                          value.Key == SqlDataWriteConnectionCloseAfterCommand) &&
                    value.GetProperty<Guid>(ConnectionKey) is { } id &&
                    TryGetConnectionSpan(scope, id) is { } connectionSpan)
                {
                    span = connectionSpan;
                }
                else if ((value.Key is SqlMicrosoftWriteTransactionCommitAfter ||
                          value.Key is SqlDataWriteTransactionCommitAfter) &&
                    value.GetSubProperty<Guid>("Connection", "ClientConnectionId") is { } commitId &&
                    TryGetConnectionSpan(scope, commitId) is { } commitSpan)
                {
                    span = commitSpan;
                }
                else
                {
                    _options.LogWarning("Trying to get a span of type {0} with operation id {1}, but it was not found.",
                        type,
                        value.GetProperty<Guid>(OperationKey));
                }
            });
            return span;
        }

        private static ISpan? TryStartChild(ISpan? parent, string operation, string? description)
            => parent?.StartChild(operation, description);

        private static ISpan? TryGetConnectionSpan(Scope scope, Guid connectionId)
            => scope.Transaction?.Spans.FirstOrDefault(span => !span.IsFinished && span.Operation is "db.connection" && TryGetConnectionId(span) == connectionId);

        private static ISpan? TryGetQuerySpan(Scope scope, Guid operationId)
            => scope.Transaction?.Spans.FirstOrDefault(span => TryGetOperationId(span) == operationId);

        private void UpdateConnectionSpan(Guid operationId, Guid connectionId)
            => _hub.ConfigureScope(scope =>
            {
                var connectionSpans = scope.Transaction?.Spans?.Where(span => span.Operation is "db.connection").ToList();
                if (connectionSpans?.FirstOrDefault(span => !span.IsFinished && TryGetOperationId(span) == operationId) is { } span)
                {
                    SetConnectionId(span, connectionId);
                }
            });

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            try
            {
                // Query.
                if (value.Key == SqlMicrosoftBeforeExecuteCommand || value.Key == SqlDataBeforeExecuteCommand)
                {
                    AddSpan(SentrySqlSpanType.Execution, "db.query", value);
                }
                else if ((value.Key == SqlMicrosoftAfterExecuteCommand || value.Key == SqlDataAfterExecuteCommand) &&
                    GetSpan(SentrySqlSpanType.Execution, value) is { } commandSpan)
                {
                    commandSpan.Description = value.GetSubProperty<string>("Command", "CommandText");
                    commandSpan.Finish(SpanStatus.Ok);
                }
                else if ((value.Key == SqlMicrosoftWriteCommandError || value.Key == SqlDataWriteCommandError) &&
                    GetSpan(SentrySqlSpanType.Execution, value) is { } errorSpan)
                {
                    errorSpan.Description = value.GetSubProperty<string>("Command", "CommandText");
                    errorSpan.Finish(SpanStatus.InternalError);
                }

                // Connection.
                else if (value.Key == SqlMicrosoftWriteConnectionOpenBeforeCommand || value.Key == SqlDataWriteConnectionOpenBeforeCommand)
                {
                    AddSpan(SentrySqlSpanType.Connection, "db.connection", value);
                }
                else if (value.Key == SqlMicrosoftWriteConnectionOpenAfterCommand || value.Key == SqlDataWriteConnectionOpenAfterCommand)
                {
                    UpdateConnectionSpan(value.GetProperty<Guid>(OperationKey), value.GetProperty<Guid>(ConnectionKey));
                }
                else if ((value.Key == SqlMicrosoftWriteConnectionCloseAfterCommand ||
                          value.Key == SqlDataWriteConnectionCloseAfterCommand) &&
                    GetSpan(SentrySqlSpanType.Connection, value) is { } connectionSpan)
                {
                    TrySetConnectionStatistics(connectionSpan, value);
                    connectionSpan.Finish(SpanStatus.Ok);
                }
                else if ((value.Key is SqlMicrosoftWriteTransactionCommitAfter || value.Key is SqlDataWriteTransactionCommitAfter) &&
                    GetSpan(SentrySqlSpanType.Connection, value) is { } connectionSpan2)
                {
                    // If some query makes changes to the Database data, CloseAfterCommand event will not be invoked,
                    // instead, TransactionCommitAfter is invoked.
                    connectionSpan2.Finish(SpanStatus.Ok);
                }
            }
            catch (Exception ex)
            {
                _options.LogError("Failed to intercept SQL event.", ex);
            }
        }

        private static void TrySetConnectionStatistics(ISpan span, KeyValuePair<string, object?> value)
        {
            if (value.GetProperty<Dictionary<object, object>>("Statistics") is { } statistics)
            {
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
    }
}
