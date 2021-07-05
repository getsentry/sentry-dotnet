using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sentry.EntityFramework
{
    internal class SentryQueryPerformanceListener : IDbCommandInterceptor
    {
        private ISpan? _mySpan;
        public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            var span = SentrySdk.GetSpan();
            _mySpan = span?.StartChild("A Reader child");
        }

        public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            _mySpan?.Finish();
        }

        public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            var span = SentrySdk.GetSpan();
            _mySpan = span?.StartChild("A NonQuery child");
        }

        public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            _mySpan?.Finish();
        }

        public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            var span = SentrySdk.GetSpan();
            _mySpan = span?.StartChild("A Scalar child");
        }

        public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            _mySpan?.Finish();
        }
    }
}
