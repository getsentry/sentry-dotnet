using Microsoft.Data.Sqlite;

namespace Sentry.Sqlite;

/// <inheritdoc cref="SqliteCommand"/>
public class SentrySqliteCommand : SqliteCommand
{
    internal ITransactionTracer? TransactionTracer { get; set; } = null;

    /// <inheritdoc cref="SqliteCommand.ExecuteNonQuery"/>
    public override int ExecuteNonQuery()
    {
        var span = TransactionTracer?.StartChild("db.query", CommandText);
        try
        {
            return base.ExecuteNonQuery();
        }
        finally
        {
            span?.Finish();
        }
    }

    /// <inheritdoc cref="SqliteCommand.ExecuteScalar"/>
    public override object? ExecuteScalar()
    {
        var span = TransactionTracer?.StartChild("db.query", CommandText);
        try
        {
            return base.ExecuteScalar();
        }
        finally
        {
            span?.Finish();
        }
    }
}
