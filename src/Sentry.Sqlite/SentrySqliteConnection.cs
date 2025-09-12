using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace Sentry.Sqlite;

/// <inheritdoc cref="SqliteConnection"/>
public class SentrySqliteConnection : SqliteConnection
{
    private ITransactionTracer? TransactionTracer { get; set; } = null;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SentrySqliteConnection" /> class.
    /// </summary>
    /// <param name="connectionString">The string used to open the connection.</param>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/connection-strings">Connection Strings</seealso>
    /// <seealso cref="SqliteConnectionStringBuilder" />
    public SentrySqliteConnection(string? connectionString) : base(connectionString)
    {
    }

    /// <inheritdoc cref="SqliteConnection.Open"/>
    public override void Open()
    {
        TransactionTracer = SentrySdk.StartTransaction(
            "Open SQLite Connection",
            "db.sqlite.open"
        );
        base.Open();
    }

    /// <inheritdoc cref="SqliteConnection.CreateCommand"/>
    public new virtual SentrySqliteCommand CreateCommand()
        => new()
        {
            Connection = this,
            CommandTimeout = DefaultTimeout,
            Transaction = Transaction,
            TransactionTracer = TransactionTracer
        };

    /// <inheritdoc cref="SqliteConnection.CreateDbCommand"/>
    protected override DbCommand CreateDbCommand()
        => CreateCommand();

    /// <inheritdoc cref="SqliteConnection.Close"/>
    public override void Close()
    {
        base.Close();
        TransactionTracer?.Finish();
        TransactionTracer = null;
    }
}
