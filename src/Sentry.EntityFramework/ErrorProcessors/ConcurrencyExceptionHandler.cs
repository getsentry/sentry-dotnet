using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sentry.Extensibility;

namespace Sentry.EntityFramework.ErrorProcessors
{
    public class ConcurrencyExceptionHandler : SentryEventExceptionProcessor<DBConcurrencyException>
    {
        protected override void ProcessException(DBConcurrencyException exception, SentryEvent sentryEvent)
        {
            sentryEvent.SetExtra("Row Count", exception.RowCount);
            sentryEvent.SetExtra("Row Error", exception.Row.RowError);
        }
    }
}
