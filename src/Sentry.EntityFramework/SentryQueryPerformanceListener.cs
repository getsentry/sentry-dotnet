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
        private Dictionary<string,ISpan?> _mySpans = new Dictionary<string, ISpan?>();
        public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            Console.WriteLine("ReaderExecuting");
            var span = SentrySdk.GetSpan();
            _mySpans["Reader"] = span?.StartChild("A Reader child", command.CommandText);
        }

        public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            Console.WriteLine("ReaderExecuted", command.CommandText);
            _mySpans["Reader"]?.Finish();
        }

        public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            Console.WriteLine("NonQueryExecuting");
            var span = SentrySdk.GetSpan();
            _mySpans["NonQuery"] = span?.StartChild("A NonQuery child", command.CommandText);
        }

        public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            Console.WriteLine("NonQueryExecuted");
            _mySpans["NonQuery"]?.Finish();
        }

        public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            Console.WriteLine("ScalarExecuting");
            var span = SentrySdk.GetSpan();
            _mySpans["Scalar"] = span?.StartChild("A Scalar child");
        }

        public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            Console.WriteLine("ScalarExecuted");
            _mySpans["Scalar"]?.Finish();
        }
    }
}
