namespace Sentry.EntityFramework.ErrorProcessors;

/// <summary>
/// Exception processor for Entity Framework <see cref="DBConcurrencyException"/>.
/// </summary>
public class DbConcurrencyExceptionProcessor : SentryEventExceptionProcessor<DBConcurrencyException>
{
    /// <summary>
    /// Extracts RowCount and RowError from <see cref="DBConcurrencyException"/>.
    /// </summary>
    protected internal override void ProcessException(DBConcurrencyException exception, SentryEvent sentryEvent)
    {
        sentryEvent.Extra["Row Count"] = exception.RowCount;
        if (exception.Row is { } row)
        {
            sentryEvent.Extra["Row Error"] = exception.Row.RowError;
        }
    }
}
