using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;

namespace Sentry.EntityFramework
{
    internal class SentryQueryPerformanceListener : IDbCommandInterceptor
    {
        //AsyncLocal
        private Dictionary<string, ISpan?> _mySpans = new Dictionary<string, ISpan?>();
        public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            Console.WriteLine("ReaderExecuting");
            CreateOrUpdateSpan("Reader", command.CommandText);
        }

        public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            Console.WriteLine("ReaderExecuted", command.CommandText);
            Finish("Reader");
        }

        public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            Console.WriteLine("NonQueryExecuting");
            CreateOrUpdateSpan("NonQuery", command.CommandText);
        }

        public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            Console.WriteLine("NonQueryExecuted");
            Finish("NonQuery");
        }

        public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            Console.WriteLine("ScalarExecuting");
            CreateOrUpdateSpan("Scalar", command.CommandText);
        }

        public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            Console.WriteLine("ScalarExecuted");
            Finish("Scalar");
        }

        private void CreateOrUpdateSpan(string key, string? command)
        {
            var span = SentrySdk.GetSpan();
            if (_mySpans.ContainsKey(key) && _mySpans[key] is ISpan oldSpan) {
                oldSpan.Finish();
            }
            _mySpans[key] = span?.StartChild("db", command ?? key);
        }

        private void Finish(string key)
        {
            var span =  _mySpans[key];
            _mySpans[key] = null;
            span?.Finish();
        }
    }
}
