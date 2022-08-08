namespace Sentry.EntityFramework;

/// <summary>
/// A command interceptor to augment data for Sentry.
/// </summary>
public class SentryCommandInterceptor : IDbCommandInterceptor
{
    private readonly IQueryLogger _queryLogger;

    /// <summary>
    /// Creates a new instance of <see cref="SentryCommandInterceptor"/>.
    /// </summary>
    /// <param name="queryLogger"></param>
    public SentryCommandInterceptor(IQueryLogger queryLogger) => _queryLogger = queryLogger;

    /// <summary>
    /// Logs a call to <see cref="NonQueryExecuting"/>.
    /// </summary>
    public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        => Log(command, interceptionContext);

    /// <summary>
    /// No Op.
    /// </summary>
    public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext) { }

    /// <summary>
    /// Logs a call to <see cref="ReaderExecuting"/>.
    /// </summary>
    public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        => Log(command, interceptionContext);

    /// <summary>
    /// No Op.
    /// </summary>
    public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext) { }

    /// <summary>
    /// Logs a call to <see cref="ScalarExecuting"/>.
    /// </summary>
    public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        => Log(command, interceptionContext);

    /// <summary>
    /// Logs a call to <see cref="ScalarExecuted"/>.
    /// </summary>
    public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext) { }

    /// <summary>
    /// Logs the <see cref="DbCommand"/> with the specified context.
    /// </summary>
    public virtual void Log<T>(DbCommand command, DbCommandInterceptionContext<T> interceptionContext)
    {
        if (string.IsNullOrEmpty(command.CommandText))
        {
            return;
        }

        if (interceptionContext.Exception == null)
        {
            _queryLogger.Log(command.CommandText);
        }
        else
        {
            _queryLogger.Log(command.CommandText, BreadcrumbLevel.Error);
        }
    }
}
