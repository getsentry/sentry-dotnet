using System;
using System.Collections.Generic;
using System.Threading;
using Sentry.Extensibility;

namespace Sentry.Internals.DiagnosticSource
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

        private bool _logConnectionEnabled = true;
        private bool _logQueryEnabled = true;

        public SentryEFCoreListener(IHub hub, SentryOptions options)
        {
            _hub = hub;
            _options = options;
        }

        internal void DisableConnectionSpan() => _logConnectionEnabled = false;

        internal bool DisableQuerySpan() => _logQueryEnabled = false;

        private ISpan? AddSpan(SentryEFSpanType type, string operation, string? description)
        {
            ISpan? span = null;
            _hub.ConfigureScope(scope =>
            {
                if (scope.Transaction?.IsSampled != true)
                {
                    return;
                }

                if (scope.GetSpan()?.StartChild(operation, description) is not { } startedChild)
                {
                    return;
                }

                if (GetSpanBucket(type) is not { } asyncLocalSpan)
                {
                    return;
                }

                asyncLocalSpan.Value = new WeakReference<ISpan>(startedChild);
                span = startedChild;
            });
            return span;
        }

        private ISpan? TakeSpan(SentryEFSpanType type)
        {
            ISpan? span = null;
            _hub.ConfigureScope(scope =>
            {
                if (scope.Transaction?.IsSampled == true)
                {
                    if (GetSpanBucket(type)?.Value is { } reference &&
                        reference.TryGetTarget(out var startedSpan))
                    {
                        span = startedSpan;
                    }
                    _options.LogWarning("Trying to close a span that was already garbage collected. {0}", type);
                }
            });
            return span;
        }

        private AsyncLocal<WeakReference<ISpan>>? GetSpanBucket(SentryEFSpanType type)
            => type switch
            {
                SentryEFSpanType.QueryCompiler => _spansCompilerLocal,
                SentryEFSpanType.QueryExecution => _spansQueryLocal,
                SentryEFSpanType.Connection => _spansConnectionLocal,
                _ => null
            };

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            try
            {
                //Query compiler Span
                if (value.Key == EFQueryStartCompiling || value.Key == EFQueryCompiling)
                {
                    AddSpan(SentryEFSpanType.QueryCompiler, "db.query_compiler", FilterNewLineValue(value.Value));
                }
                else if (value.Key == EFQueryCompiled)
                {
                    TakeSpan(SentryEFSpanType.QueryCompiler)?.Finish(SpanStatus.Ok);
                }

                //Connection Span
                //A transaction may or may not show a connection with it.
                else if (_logConnectionEnabled && value.Key == EFConnectionOpening)
                {
                    AddSpan(SentryEFSpanType.Connection, "db.connection", null);
                }
                else if (_logConnectionEnabled && value.Key == EFConnectionClosed)
                {
                    TakeSpan(SentryEFSpanType.Connection)?.Finish(SpanStatus.Ok);
                }

                //Query Execution Span
                else if (_logQueryEnabled)
                {
                    if (value.Key == EFCommandExecuting)
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
            }
            catch (Exception ex)
            {
                _options.LogError("Failed to intercept EF Core event.", ex);
            }
        }

        /// <summary>
        /// Get the Query with error message and remove the uneeded values.
        /// </summary>
        /// <example>
        /// Compiling query model:
        /// EF initialize...\r\nEF Query...
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
