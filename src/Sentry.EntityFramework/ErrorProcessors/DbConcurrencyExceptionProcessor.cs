using System.Data;
using Sentry.Extensibility;

namespace Sentry.EntityFramework.ErrorProcessors
{
    public class DbConcurrencyExceptionProcessor : SentryEventExceptionProcessor<DBConcurrencyException>
    {
        protected override void ProcessException(DBConcurrencyException exception, SentryEvent sentryEvent)
        {
            sentryEvent.SetExtra("Row Count", exception.RowCount);
            sentryEvent.SetExtra("Row Error", exception.Row.RowError);
        }
    }
}
