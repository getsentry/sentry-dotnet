using System;
using System.Collections.Generic;
using System.Linq;
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

        private void SetSpan(SentryEFSpanType type, ISpan? span) => _spans[type] = span;

        private ISpan? GetSpan(SentryEFSpanType type) => _spans.GetValueOrDefault(type);

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key == "Microsoft.EntityFrameworkCore.ChangeTracking.StartedTracking")
                return;

            if (value.Key == EFContextInitializedKey)
            {
                SetSpan(SentryEFSpanType.Context, _hub.GetSpan()?.StartChild("ef.core", "opening EF Core context."));
            }

            else if (value.Key == EFConnectionOpening)
            {
                SetSpan(SentryEFSpanType.Connection, _hub.GetSpan()?.StartChild("db", "connection"));
            }
            else if (value.Key == EFCommandExecuting &&
                _hub.GetSpan()?.StartChild("db", null) is { } querySpanExecuting)
            {
                SetSpan(SentryEFSpanType.QueryExecution, querySpanExecuting);
            }
            else if (value.Key == EFCommandFailed &&
                GetSpan(SentryEFSpanType.QueryExecution) is { } errorQuerySpan)
            {
                errorQuerySpan.Description = FilterExecutedQueryWithErrorValue(value);
                errorQuerySpan.Finish(SpanStatus.InternalError);
            }
            else if (value.Key == EFCommandExecuted &&
                GetSpan(SentryEFSpanType.QueryExecution) is { } executedQuerySpan)
            {
                executedQuerySpan.Description = value.Value?.ToString();
                executedQuerySpan.Finish(SpanStatus.Ok);
            }
            else if (value.Key == EFConnectionClosed &&
                     GetSpan(SentryEFSpanType.Connection) is { } connectionSpan)
            {
                connectionSpan.Finish(SpanStatus.Ok);
            }
            else if (value.Key == EFContextDisposedKey &&
                GetSpan(SentryEFSpanType.Context) is { } bla)
            {
                // We finish it here because the transaction will be dispsed once the context is ended.
                GetSpan(SentryEFSpanType.Context)?.Finish(SpanStatus.Ok);

            }
            else if (value.Key == EFQueryCompiling)
            {
                SetSpan(SentryEFSpanType.QueryCompiler, _hub.GetSpan()?.StartChild("db", value.Value?.ToString()));
            }
            else if (value.Key == EFQueryCompiled)
            {
                GetSpan(SentryEFSpanType.QueryCompiler)?.Finish(SpanStatus.Ok);
            }
        }

        /// <summary>
        /// Get the Query with error message and remove the uneeded values.
        /// </summary>
        /// <example>
        /// Failed executing DbCommand (8ms) [Parameters=[@__ef_filter__isAuthenticated_0='?' (DbType = Boolean)], CommandType='Text', CommandTimeout='30']
        /// becomes:  [Parameters=[@__ef_filter__isAuthenticated_0='?' (DbType = Boolean)], CommandType='Text', CommandTimeout='30']
        /// Executed DbCommand (9ms) [Parameters=[@__ef_filter__isAuthenticated_0='?' (DbType = Boolean)], CommandType='Text', CommandTimeout='30']...
        /// becomes:  [Parameters=[@__ef_filter__isAuthenticated_0='?' (DbType = Boolean)], CommandType='Text', CommandTimeout='30']...
        /// </example>
        /// <param name="value">the query with error value</param>
        /// <returns>the filtered query</returns>
        internal string? FilterQueryWithTimeValue(object? value)
            => value?.ToString()?.Split(')', 1)[1];
    }
}
