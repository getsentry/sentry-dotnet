using Sentry.Extensibility;

namespace Sentry.AspNet;

/// <summary>
/// Sentry extensions for <see cref="HttpContext"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class HttpContextExtensions
{
    private const string HttpContextTransactionItemName = "__SentryTransaction";

    private static SentryTraceHeader? TryGetSentryTraceHeader(HttpContext context, SentryOptions? options)
    {
        var value = context.Request.Headers.Get(SentryTraceHeader.HttpHeaderName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        options?.LogDebug("Received Sentry trace header '{0}'.", value);

        try
        {
            return SentryTraceHeader.Parse(value);
        }
        catch (Exception ex)
        {
            options?.LogError("Invalid Sentry trace header '{0}'.", ex, value);
            return null;
        }
    }

    private static BaggageHeader? TryGetBaggageHeader(HttpContext context, SentryOptions? options)
    {
        var value = context.Request.Headers.Get(BaggageHeader.HttpHeaderName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // Note: If there are multiple baggage headers, they will be joined with comma delimiters,
        // and can thus be treated as a single baggage header.

        options?.LogDebug("Received baggage header '{0}'.", value);

        try
        {
            return BaggageHeader.TryParse(value, onlySentry: true);
        }
        catch (Exception ex)
        {
            options?.LogError("Invalid baggage header '{0}'.", ex, value);
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
        var options = SentrySdk.CurrentOptions;

        var traceHeader = TryGetSentryTraceHeader(httpContext, options);

        var transactionName = $"{method} {path}";
        const string transactionOperation = "http.server";

        var transactionContext = traceHeader is not null
            ? new TransactionContext(transactionName, transactionOperation, traceHeader, TransactionNameSource.Url)
            : new TransactionContext(transactionName, transactionOperation, TransactionNameSource.Url);

        var customSamplingContext = new Dictionary<string, object?>(3, StringComparer.Ordinal)
        {
            ["__HttpMethod"] = method,
            ["__HttpPath"] = path,
            ["__HttpContext"] = httpContext,
        };

        // Set the Dynamic Sampling Context from the baggage header, if it exists.
        var baggageHeader = TryGetBaggageHeader(httpContext, options);
        var dynamicSamplingContext = baggageHeader?.CreateDynamicSamplingContext();

        if (traceHeader is { } && baggageHeader is null)
        {
            // We received a sentry-trace header without a baggage header, which indicates the request
            // originated from an older SDK that doesn't support dynamic sampling.
            // Set DynamicSamplingContext.Empty to "freeze" the DSC on the transaction.
            // See:
            // https://develop.sentry.dev/sdk/performance/dynamic-sampling-context/#freezing-dynamic-sampling-context
            // https://develop.sentry.dev/sdk/performance/dynamic-sampling-context/#unified-propagation-mechanism
            dynamicSamplingContext = DynamicSamplingContext.Empty;
        }

        var transaction = SentrySdk.StartTransaction(transactionContext, customSamplingContext, dynamicSamplingContext);

        SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);
        httpContext.Items[HttpContextTransactionItemName] = transaction;

        if (options?.SendDefaultPii is not null)
        {
            transaction.Request.Cookies = string.Join("; ", httpContext.Request.Cookies.AllKeys.Select(x => $"{x}={httpContext.Request.Cookies[x]?.Value}"));
        }

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
