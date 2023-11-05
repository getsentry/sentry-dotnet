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
        sentryEvent.SetExtra("Row Count", exception.RowCount);
        sentryEvent.SetExtra("Row Error", exception.Row?.RowError);
    }
}
