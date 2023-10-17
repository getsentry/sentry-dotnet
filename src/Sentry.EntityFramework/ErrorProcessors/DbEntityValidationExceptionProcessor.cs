namespace Sentry.EntityFramework.ErrorProcessors;

/// <summary>
/// Exception processor for <see cref="DbEntityValidationException"/>.
/// </summary>
public class DbEntityValidationExceptionProcessor : SentryEventExceptionProcessor<DbEntityValidationException>
{
    /// <summary>
    /// Extracts details from <see cref="DbEntityValidationException"/> into the <see cref="SentryEvent"/>.
    /// </summary>
    protected internal override void ProcessException(DbEntityValidationException exception, SentryEvent sentryEvent)
    {
        var errorList = new Dictionary<string, List<string>>();
        foreach (var error in exception.EntityValidationErrors.SelectMany(x => x.ValidationErrors))
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

        sentryEvent.SetExtra("EntityValidationErrors", errorList);
    }
}
