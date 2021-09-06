using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using Sentry.Extensibility;
using Sentry.EntityFramework.Internals.Extensions;

namespace Sentry.EntityFramework
{
    internal class SentryQueryPerformanceListener : IDbCommandInterceptor
    {
        internal const string SentryUserStateKey = "SentrySpanRef";

        private SentryOptions _options { get; }
        private IHub _hub { get; }

        internal SentryQueryPerformanceListener(IHub hub, SentryOptions options)
        {
            _hub = hub;
            _options = options;
        }

        public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
            => CreateOrUpdateSpan("ef.reader", command.CommandText, interceptionContext);

        public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
            => Finish("ef.reader", interceptionContext);

        public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
            => CreateOrUpdateSpan("ef.non-query", command.CommandText, interceptionContext);

        public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
            => Finish("ef.non-query", interceptionContext);

        public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
            => CreateOrUpdateSpan("ef.scalar", command.CommandText, interceptionContext);

        public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
            => Finish("ef.scalar", interceptionContext);

        private void CreateOrUpdateSpan<T>(string key, string? command,
            DbCommandInterceptionContext<T> interceptionContext)
        {
            if (_hub.GetSpan()?.StartChild(key, command) is { } span)
            {
                interceptionContext.AttachSpan(span);
            }
        }

        private void Finish<T>(string key, DbCommandInterceptionContext<T> interceptionContext)
        {
            //Recover direct reference of the Span.
            if (interceptionContext.GetSpanFromContext() is {} span)
            {
                span.Finish();
            }
            else
            {
                _options.DiagnosticLogger?.LogWarning("Span with key {0} was not found on interceptionContext.", key);
            }
        }
    }
}
