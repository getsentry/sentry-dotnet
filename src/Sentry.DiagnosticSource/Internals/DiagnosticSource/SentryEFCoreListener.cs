using System.Threading;
using System.Collections.Generic;
using System;
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
        /// <seealso href="https://docs.microsoft.com/dotnet/api/microsoft.entityframeworkcore.diagnostics.coreeventid.querymodelcompiling?view=efcore-3.1"/>
        /// </summary>
        internal const string EFQueryStartCompiling = "Microsoft.EntityFrameworkCore.Query.QueryCompilationStarting";
        /// <summary>
        /// Used for EF Core 2.X and 3.X.
        /// <seealso href="https://docs.microsoft.com/dotnet/api/microsoft.entityframeworkcore.diagnostics.coreeventid.querymodelcompiling?view=efcore-3.1"/>
        /// </summary>
        internal const string EFQueryCompiling = "Microsoft.EntityFrameworkCore.Query.QueryModelCompiling";
        internal const string EFQueryCompiled = "Microsoft.EntityFrameworkCore.Query.QueryExecutionPlanned";

        private readonly IHub _hub;
        private readonly SentryOptions _options;

        private readonly AsyncLocal<WeakReference<ISpan>> _spansCompilerLocal = new();
        private readonly AsyncLocal<WeakReference<ISpan>> _spansQueryLocal = new();
        private readonly AsyncLocal<WeakReference<ISpan>> _spansConnectionLocal = new();

        private bool _logConnectionEnabled = true;
        private bool _logQueryEnabled = true;

        public SentryEFCoreListener(IHub hub, SentryOptions options)
        {
            _hub = hub;
            _options = options;
        }

        internal void DisableConnectionSpan() => _logConnectionEnabled = false;

        internal void DisableQuerySpan() => _logQueryEnabled = false;

        private static ISpan? GetParent(SentryEFSpanType type, Scope scope)
        {
            if (type == SentryEFSpanType.QueryExecution)
            {
                return scope.GetSpan();
            }

            return scope.Transaction;
        }

        private void AddSpan(SentryEFSpanType type, string operation, string? description)
        {
            _hub.ConfigureScope(scope =>
            {
                if (scope.Transaction?.IsSampled != true)
                {
                    return;
                }

                if (GetParent(type, scope)?.StartChild(operation, description) is not { } startedChild)
                {
                    return;
                }

                var asyncLocalSpan = GetSpanBucket(type);
                asyncLocalSpan.Value = new WeakReference<ISpan>(startedChild);
            });
        }

        private ISpan? TakeSpan(SentryEFSpanType type)
        {
            ISpan? span = null;
            _hub.ConfigureScope(scope =>
            {
                if (scope.Transaction?.IsSampled != true)
                {
                    return;
                }

                if (GetSpanBucket(type).Value is { } reference &&
                    reference.TryGetTarget(out var startedSpan))
                {
                    span = startedSpan;
                    return;
                }

                _options.LogWarning("Trying to close a span that was already garbage collected. {0}", type);
            });
            return span;
        }

        private AsyncLocal<WeakReference<ISpan>> GetSpanBucket(SentryEFSpanType type)
            => type switch
            {
                SentryEFSpanType.QueryCompiler => _spansCompilerLocal,
                SentryEFSpanType.QueryExecution => _spansQueryLocal,
                SentryEFSpanType.Connection => _spansConnectionLocal,
                _ => throw new ($"Unknown SentryEFSpanType: {type}")
            };

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            try
            {
                //Query compiler Span
                if (value.Key is EFQueryStartCompiling or EFQueryCompiling)
                {
                    AddSpan(SentryEFSpanType.QueryCompiler, "db.query.compile", FilterNewLineValue(value.Value));
                    return;
                }

                if (value.Key == EFQueryCompiled)
                {
                    TakeSpan(SentryEFSpanType.QueryCompiler)?.Finish(SpanStatus.Ok);
                    return;
                }

                //Connection Span
                //A transaction may or may not show a connection with it.

                if (_logConnectionEnabled && value.Key == EFConnectionOpening)
                {
                    AddSpan(SentryEFSpanType.Connection, "db.connection", null);
                    return;
                }

                if (_logConnectionEnabled && value.Key == EFConnectionClosed)
                {
                    TakeSpan(SentryEFSpanType.Connection)?.Finish(SpanStatus.Ok);
                    return;
                }

                // return if not Query Execution Span
                if (!_logQueryEnabled)
                {
                    return;
                }

                if (value.Key == EFCommandExecuting)
                {
                    AddSpan(SentryEFSpanType.QueryExecution, "db.query", FilterNewLineValue(value.Value));
                    return;
                }

                if (value.Key == EFCommandFailed)
                {
                    TakeSpan(SentryEFSpanType.QueryExecution)?.Finish(SpanStatus.InternalError);
                    return;
                }

                if (value.Key == EFCommandExecuted)
                {
                    TakeSpan(SentryEFSpanType.QueryExecution)?.Finish(SpanStatus.Ok);
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
