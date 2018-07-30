using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using Sentry.Extensibility;

namespace Sentry.EntityFramework.ErrorProcessors
{
    public class DbEntityValidationExceptionProcessor : SentryEventExceptionProcessor<DbEntityValidationException>
    {
        internal const string EntityValidationErrors = "EntityValidationErrors";

        protected override void ProcessException(DbEntityValidationException exception, SentryEvent sentryEvent)
        {
            var errorList = new Dictionary<string, List<string>>();
            foreach (var error in exception.EntityValidationErrors.SelectMany(x=>x.ValidationErrors))
            {
                if (errorList.TryGetValue(error.PropertyName, out var list))
                {
                    list.Add(error.ErrorMessage);
                }
                else
                {
                    list = new List<string> { error.ErrorMessage };
                    errorList.Add(error.PropertyName, list);
                }
            }

            sentryEvent.SetExtra(EntityValidationErrors, errorList);
        }
    }
}
