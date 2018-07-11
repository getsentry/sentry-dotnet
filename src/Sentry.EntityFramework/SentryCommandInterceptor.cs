using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using Sentry.Protocol;

namespace Sentry.EntityFramework
{
    public class SentryCommandInterceptor : IDbCommandInterceptor
    {
        private readonly IQueryLogger _queryLogger;

        public SentryCommandInterceptor(IQueryLogger queryLogger) => _queryLogger = queryLogger;

        public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
            => Log(command, interceptionContext);

        public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext) { }

        public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
            => Log(command, interceptionContext);

        public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext) { }

        public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
            => Log(command, interceptionContext);

        public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext) { }

        public virtual void Log<T>(DbCommand command, DbCommandInterceptionContext<T> interceptionContext)
        {
            if (interceptionContext.Exception != null)
            {
                _queryLogger.Log(command.CommandText, BreadcrumbLevel.Error);
            }
            else
            {
                _queryLogger.Log(command.CommandText);
            }
        }
    }
}
