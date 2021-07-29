using System;
using System.Collections.Generic;
using System.Threading;
using Sentry.Extensibility;

namespace Sentry.Extensions.Logging
{
    /// <summary>
    /// Class that consumes Entity Framework Core events.
    /// </summary>
    internal class SentryEFCoreListener : IObserver<KeyValuePair<string, object?>>
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
        /// Used for EF Core 2.X and 3.X. 
        /// <seealso href="https://docs.microsoft.com/dotnet/api/microsoft.entityframeworkcore.diagnostics.coreeventid.querymodelcompiling?view=efcore-3.1"></seealso>
        /// </summary>
        internal const string EFQueryStartCompiling = "Microsoft.EntityFrameworkCore.Query.QueryCompilationStarting";
        /// <summary>
        /// Used for EF Core 2.X and 3.X.
        /// <seealso href="https://docs.microsoft.com/dotnet/api/microsoft.entityframeworkcore.diagnostics.coreeventid.querymodelcompiling?view=efcore-3.1"></seealso>
        /// </summary>
        internal const string EFQueryCompiling = "Microsoft.EntityFrameworkCore.Query.QueryModelCompiling";
        internal const string EFQueryCompiled = "Microsoft.EntityFrameworkCore.Query.QueryExecutionPlanned";

        private IHub _hub { get; }
        private SentryOptions _options { get; }

        private AsyncLocal<WeakReference<ISpan>> _spansCompilerLocal = new();
        private AsyncLocal<WeakReference<ISpan>> _spansQueryLocal = new();
        private AsyncLocal<WeakReference<ISpan>> _spansConnectionLocal = new();

        public SentryEFCoreListener(IHub hub, SentryOptions options)
        {
            _hub = hub;
            _options = options;
        }

        private ISpan? AddSpan(SentryEFSpanType type, string operation, string? description)
        {
            if (_hub.GetSpan()?.StartChild(operation, description) is { } span)
            {
                if (type switch
                {
                    SentryEFSpanType.QueryCompiler => _spansCompilerLocal,
                    SentryEFSpanType.QueryExecution => _spansQueryLocal,
                    SentryEFSpanType.Connection => _spansConnectionLocal,
                    _ => null
                } is { } asyncLocalSpan)
                {
                    asyncLocalSpan.Value = new WeakReference<ISpan>(span);
                }
                return span;
            }
            return null;
        }

        private ISpan? TakeSpan(SentryEFSpanType type)
        {
            if (type switch
            {
                SentryEFSpanType.QueryCompiler => _spansCompilerLocal.Value,
                SentryEFSpanType.QueryExecution => _spansQueryLocal.Value,
                SentryEFSpanType.Connection => _spansConnectionLocal.Value,
                _ => null
            } is { } reference && reference.TryGetTarget(out var span))
            {
                return span;
            }
            _options.DiagnosticLogger?.LogWarning("Trying to close a span that was already garbage collected. {0}", type);
            return null;
        }


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
                    AddSpan(SentryEFSpanType.QueryCompiler, "db.query_compiler", FilterNewLineValue(value.Value));
                }
                else if (value.Key == EFQueryCompiled)
                {
                    TakeSpan(SentryEFSpanType.QueryCompiler)?.Finish(SpanStatus.Ok);
                }

                //Connection Span
                //A transaction may or may not show a connection with it.
                else if (value.Key == EFConnectionOpening)
                {
                    AddSpan(SentryEFSpanType.Connection, "db.connection", null);
                }
                else if (value.Key == EFConnectionClosed)
                {
                    TakeSpan(SentryEFSpanType.Connection)?.Finish(SpanStatus.Ok);
                }

                //Query Execution Span
                else if (value.Key == EFCommandExecuting)
                {
                    AddSpan(SentryEFSpanType.QueryExecution, "db.query", FilterNewLineValue(value.Value));
                }
                else if (value.Key == EFCommandFailed)
                {
                    TakeSpan(SentryEFSpanType.QueryExecution)?.Finish(SpanStatus.InternalError);
                }
                else if (value.Key == EFCommandExecuted)
                {
                    TakeSpan(SentryEFSpanType.QueryExecution)?.Finish(SpanStatus.Ok);
                }
            }
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.LogError("Failed to intercept EF Core event.", ex);
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
