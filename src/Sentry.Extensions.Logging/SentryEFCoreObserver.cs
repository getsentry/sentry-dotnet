using System;
using System.Collections.Generic;
using System.Threading;

namespace Sentry.Extensions.Logging
{
    /// <summary>
    /// Class that consumes Entity Framework Core events.
    /// </summary>
    internal class SentryEFCoreObserver : IObserver<KeyValuePair<string, object?>>
    {
        private enum SentryEFSpanType
        {
            Connection,
            QueryExecution,
            QueryCompiler
        }

        internal const string EFConnectionOpening = "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionOpening";
        internal const string EFConnectionClosed = "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionClosed";
        internal const string EFCommandExecuting = "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting";
        internal const string EFCommandExecuted = "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted";
        internal const string EFCommandFailed = "Microsoft.EntityFrameworkCore.Database.Command.CommandError";

        /// <summary>
        /// Used for EF Core 5.X,
        ///  <a href="https://docs.microsoft.com/dotnet/api/microsoft.entityframeworkcore.diagnostics.coreloggerextensions.querycompilationstarting?view=efcore-5.0">See</a>.
        /// </summary>
        internal const string EFQueryStartCompiling = "Microsoft.EntityFrameworkCore.Query.QueryCompilationStarting";
        /// <summary>
        /// Used for EF Core 2.X and 3.X, 
        ///  <a href="https://docs.microsoft.com/dotnet/api/microsoft.entityframeworkcore.diagnostics.coreeventid.querymodelcompiling?view=efcore-3.1">See</a>.
        /// </summary>
        internal const string EFQueryCompiling = "Microsoft.EntityFrameworkCore.Query.QueryModelCompiling";
        internal const string EFQueryCompiled = "Microsoft.EntityFrameworkCore.Query.QueryExecutionPlanned";

        private IHub _hub { get; }
        private SentryOptions _options { get; }

        private AsyncLocal<Dictionary<SentryEFSpanType, ISpan?>> _spansLocal = new();

        private Dictionary<SentryEFSpanType, ISpan?> _spans => _spansLocal.Value ??= new();

        public SentryEFCoreObserver(IHub hub, SentryOptions options)
        {
            _hub = hub;
            _options = options;
        }

        private ISpan? SetSpan(SentryEFSpanType type, string operation, string? description)
            => _spans[type] = _hub.GetSpan()?.StartChild(operation, description);

        private ISpan? GetSpan(SentryEFSpanType type) => _spans.GetValueOrDefault(type);

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            try
            {
                //Query compiler Span           
                if (value.Key == EFQueryStartCompiling || value.Key == EFQueryCompiling)
                {
                    // There are no events when an error happens to the query compiler so we assume it's an errored span
                    // if not compiled. Also, this query doesn't generate any children so this is good to go.
                    SetSpan(SentryEFSpanType.QueryCompiler, "db.query_compiler", FilterNewLineValue(value.Value))
                        ?.Finish(SpanStatus.InternalError);
                }
                else if (value.Key == EFQueryCompiled)
                {
                    GetSpan(SentryEFSpanType.QueryCompiler)?.Finish(SpanStatus.Ok);
                }

                //Connection Span
                //A transaction may or may not show a connection with it.
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
                    SetSpan(SentryEFSpanType.QueryExecution, "db.query", FilterNewLineValue(value.Value))?.Finish();
                }
                else if (value.Key == EFCommandFailed)
                {
                    GetSpan(SentryEFSpanType.QueryExecution)?.Finish(SpanStatus.InternalError);
                }
                else if (value.Key == EFCommandExecuted)
                {
                    GetSpan(SentryEFSpanType.QueryExecution)?.Finish(SpanStatus.Ok);
                }
            }
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.Log(SentryLevel.Error, "Failed to intercept EF Core event.", ex);
            }
        }

        /// <summary>
        /// Get the Query with error message and remove the uneeded values.
        /// </summary>
        /// <example>
        /// Compiling query model:
        /// EF intialize...\r\nEF Query...
        /// becomes:
        /// EF Query...
        /// </example>
        /// <param name="value">the query to be parsed value</param>
        /// <returns>the filtered query</returns>
        internal static string? FilterNewLineValue(object? value)
        {
            var str = value?.ToString();
            return str?.Substring(str.IndexOf('\n') + 1);
        }
    }
}
