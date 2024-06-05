using Sentry.Internal;

namespace Sentry.EntityFramework;

internal class SentryQueryPerformanceListener : IDbCommandInterceptor
{
    internal const string SentryUserStateKey = "SentrySpanRef";
    internal const string DbReaderKey = "db.query";
    internal const string DbNonQueryKey = "db.execute";
    internal const string DbScalarKey = "db.query.scalar";

    internal static readonly string EntityFrameworkOrigin = "auto.db.entity-framework";

    private SentryOptions _options;
    private IHub _hub;

    internal SentryQueryPerformanceListener(IHub hub, SentryOptions options)
    {
        _hub = hub;
        _options = options;
    }

    public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        => CreateSpan(DbReaderKey, command.CommandText, interceptionContext);

    public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        => Finish(DbReaderKey, interceptionContext);

    public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        => CreateSpan(DbNonQueryKey, command.CommandText, interceptionContext);

    public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        => Finish(DbNonQueryKey, interceptionContext);

    public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        => CreateSpan(DbScalarKey, command.CommandText, interceptionContext);

    public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        => Finish(DbScalarKey, interceptionContext);

    private void CreateSpan<T>(string key, string? command,
        DbCommandInterceptionContext<T> interceptionContext)
    {
        if (_hub.GetSpan()?.StartChild(key, command) is { } span)
        {
            span.SetOrigin(EntityFrameworkOrigin);
            interceptionContext.AttachSpan(span);
        }
    }

    /// <summary>
    /// Finishes the span contained on interceptionContext.
    /// </summary>
    /// <typeparam name="T">(unused) The TResult from the Interception.</typeparam>
    /// <param name="key">The key operation, used for logging.</param>
    /// <param name="interceptionContext">The data that must contain a Span reference.</param>
    private void Finish<T>(string key, DbCommandInterceptionContext<T> interceptionContext)
    {
        //Recover direct reference of the Span.
        if (interceptionContext.GetSpanFromContext() is { } span)
        {
            if (interceptionContext.Exception is null)
            {
                span.Finish(SpanStatus.Ok);
            }
            else
            {
                span.Finish(interceptionContext.Exception);
            }
        }
        //Only log if there was a transaction on the Hub.
        else if (_options.DiagnosticLevel == SentryLevel.Debug && _hub.GetSpan() is { })
        {
            _options.DiagnosticLogger?.LogDebug("Span with key {0} was not found on interceptionContext.", key);
        }
    }
}
