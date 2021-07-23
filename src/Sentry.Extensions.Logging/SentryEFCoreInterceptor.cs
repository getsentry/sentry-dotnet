using System;
using System.Collections.Generic;
using System.Threading;

namespace Sentry.Extensions.Logging
{
    /// <summary>
    /// Class that consumes Entity Framework Core events.
    /// </summary>
    internal class SentryEFCoreInterceptor : IObserver<KeyValuePair<string, object?>>
    {
        enum SentryEFSpanType
        {
            Context,
            Connection,
            QueryExecution,
            QueryCompiler
        }

        internal const string EFContextInitializedKey = "Microsoft.EntityFrameworkCore.Infrastructure.ContextInitialized";
        internal const string EFContextDisposedKey = "EntityFrameworkCore.Infrastructure.ContextDisposed";
        internal const string EFConnectionOpening = "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionOpening";
        internal const string EFConnectionClosed = "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionClosed";
        internal const string EFCommandExecuting = "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting";
        internal const string EFCommandExecuted = "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted";
        internal const string EFCommandFailed = "Microsoft.EntityFrameworkCore.Database.Command.CommandError";
        internal const string EFQueryCompiling = "Microsoft.EntityFrameworkCore.Query.QueryModelCompiling";
        internal const string EFQueryCompiled = "Microsoft.EntityFrameworkCore.Query.QueryExecutionPlanned";

        private IHub _hub { get; }

        private AsyncLocal<Dictionary<SentryEFSpanType, ISpan?>> _spansLocal = new();

        private Dictionary<SentryEFSpanType, ISpan?> _spans => _spansLocal.Value ??= new();

        public SentryEFCoreInterceptor(IHub maHub) => _hub = maHub;

        private void SetSpan(SentryEFSpanType type, string operation, string? description)
            => _spans[type] = _hub.GetSpan()?.StartChild(operation, description);

        private ISpan? GetSpan(SentryEFSpanType type) => _spans.GetValueOrDefault(type);

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key == EFContextInitializedKey)
            {
                SetSpan(SentryEFSpanType.Context, "ef.core", "opening EF Core context.");
            }

            //Query compiler Span
            else if (value.Key == EFQueryCompiling)
            {
                SetSpan(SentryEFSpanType.QueryCompiler, "db.query_compiler", FilterNewLineValue(value));
            }
            else if (value.Key == EFQueryCompiled)
            {
                GetSpan(SentryEFSpanType.QueryCompiler)?.Finish(SpanStatus.Ok);
            }

            //Connection Span
            else if (value.Key == EFConnectionOpening)
            {
                SetSpan(SentryEFSpanType.Connection, "db.connection", null);
            }
            else if (value.Key == EFConnectionClosed)
            {
                GetSpan(SentryEFSpanType.Connection)?.Finish(SpanStatus.Ok);
            }

            //Query Execution Span
            else if (value.Key == EFCommandExecuting)
            {
                SetSpan(SentryEFSpanType.QueryExecution, "db.query", FilterNewLineValue(value));
            }
            else if (value.Key == EFCommandFailed)
            {
                GetSpan(SentryEFSpanType.QueryExecution)?.Finish(SpanStatus.InternalError);
            }
            else if (value.Key == EFCommandExecuted)
            {
                GetSpan(SentryEFSpanType.QueryExecution)?.Finish(SpanStatus.Ok);
            }

            else if (value.Key == EFContextDisposedKey)
            {
                // We finish it here because the transaction will be dispsed once the context is ended.
                GetSpan(SentryEFSpanType.Context)?.Finish(SpanStatus.Ok);

            }
        }

        /// <summary>
        /// Get the Query with error message and remove the uneeded values.
        /// </summary>
        /// <example>
        /// Compiling query model: 
        /// EF Query...
        /// becomes:
        /// EF Query...
        /// </example>
        /// <param name="value">the query with error value</param>
        /// <returns>the filtered query</returns>
        internal string? FilterNewLineValue(object? value)
        {
            var str = value?.ToString();
            return str?.Substring(str.IndexOf('\n') + 1);
        }        
    }
}
