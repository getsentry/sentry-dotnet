using System.Collections.Specialized;
using System.ComponentModel;
using System.Web;

namespace Sentry.AspNet;

/// <summary>
/// Sentry extensions for <see cref="HttpContext"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class HttpContextExtensions
{
    private const string HttpContextTransactionItemName = "__SentryTransaction";

    private static SentryTraceHeader? TryGetTraceHeader(NameValueCollection headers)
    {
        try
        {
            var traceHeader = headers.Get(SentryTraceHeader.HttpHeaderName);
            return SentryTraceHeader.Parse(traceHeader);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Starts a new Sentry transaction that encompasses the currently executing HTTP request.
    /// </summary>
    public static ITransaction StartSentryTransaction(this HttpContext httpContext)
    {
        var method = httpContext.Request.HttpMethod;
        var path = httpContext.Request.Path;

        var traceHeader = TryGetTraceHeader(httpContext.Request.Headers);

        var transactionName = $"{method} {path}";
        const string transactionOperation = "http.server";

        var transactionContext = traceHeader is not null
            ? new TransactionContext(transactionName, transactionOperation, traceHeader)
            : new TransactionContext(transactionName, transactionOperation);

        var transaction = SentrySdk.StartTransaction(transactionContext);

        SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);
        httpContext.Items[HttpContextTransactionItemName] = transaction;

        return transaction;
    }

    /// <summary>
    /// Finishes an active Sentry transaction that encompasses the currently executing HTTP request (if present).
    /// </summary>
    public static void FinishSentryTransaction(this HttpContext httpContext)
    {
        if (!httpContext.Items.Contains(HttpContextTransactionItemName))
        {
            return;
        }

        if (httpContext.Items[HttpContextTransactionItemName] is not ISpan transaction)
        {
            return;
        }

        var status = SpanStatusConverter.FromHttpStatusCode(httpContext.Response.StatusCode);
        transaction.Finish(status);
    }
}
