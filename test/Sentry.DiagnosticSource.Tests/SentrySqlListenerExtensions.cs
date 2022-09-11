using Sentry.Internals.DiagnosticSource;

namespace Sentry.DiagnosticSource.Tests;

internal static class SentrySqlListenerExtensions
{
    public static void OpenConnectionStart(this SentrySqlListener listener, Guid operationId)
        => listener.OnNext(new(
            SentrySqlListener.SqlMicrosoftWriteConnectionOpenBeforeCommand,
            new { OperationId = operationId }));

    public static void OpenConnectionStarted(this SentrySqlListener listener, Guid operationId, Guid connectionId)
        => listener.OnNext(new(
            SentrySqlListener.SqlMicrosoftWriteConnectionOpenAfterCommand,
            new { OperationId = operationId, ConnectionId = connectionId }));

    public static void OpenConnectionClose(this SentrySqlListener listener, Guid operationId, Guid connectionId)
        => listener.OnNext(new(
            SentrySqlListener.SqlMicrosoftWriteConnectionCloseAfterCommand,
            new { OperationId = operationId, ConnectionId = connectionId }));

    public static void ExecuteQueryStart(this SentrySqlListener listener, Guid operationId, Guid connectionId)
        => listener.OnNext(new(
            SentrySqlListener.SqlDataBeforeExecuteCommand,
            new { OperationId = operationId, ConnectionId = connectionId }));

    public static void ExecuteQueryFinish(this SentrySqlListener listener, Guid operationId, Guid connectionId, string query)
        => listener.OnNext(new(
            SentrySqlListener.SqlDataAfterExecuteCommand,
            new { OperationId = operationId, ConnectionId = connectionId, Command = new { CommandText = query } }));

    public static void ExecuteQueryFinishWithError(this SentrySqlListener listener, Guid operationId, Guid connectionId, string query)
        => listener.OnNext(new(
            SentrySqlListener.SqlDataWriteCommandError,
            new { OperationId = operationId, ConnectionId = connectionId, Command = new { CommandText = query } }));
}
