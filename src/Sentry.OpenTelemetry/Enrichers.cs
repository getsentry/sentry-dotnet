using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry.OpenTelemetry;

internal static class Enrichers
{
    public static void CaptureGraphQlFailedRequests(Activity data, ISpan span, SentryOptions? options)
    {
        // Only capture request context if the user has opted in
        if (!(options?.SendDefaultPii ?? false))
        {
            return;
        }

        if (span is SpanTracer)
        {
            // Store the GraphQL document on the transaction to include in the Request Context for bad requests
            if (span.Extra.TryGetReadOnlyTypedValue(SemanticConventions.AttributeGraphQlDocument, out string document))
            {
                var parent = span.GetTransaction();
                parent.SetFused(SemanticConventions.AttributeGraphQlDocument, document);
            }
        }
        else if (span is TransactionTracer transaction)
        {
            // We only capture this for 4xx or 5xx requests
            if (GetStatusCode(transaction) >= 400)
            {
                // Retrieve the GraphQL document to include in the Request Context
                var document = transaction.GetFused<string>(SemanticConventions.AttributeGraphQlDocument);
                transaction.Request.Data = document;
            }
        }

        int? GetStatusCode(TransactionTracer transaction)
        {
            if (!transaction.Contexts.TryGetValue("otel", out var otel))
            {
                return null;
            }

            if (!((Dictionary<string, object?>)otel).TryGetTypedValue("attributes", out IDictionary<string, object?> attributes))
            {
                return null;
            }

            if (!attributes.TryGetValue("http.status_code", out object? statusCode))
            {
                return null;
            }

            return (statusCode is int code) ? code : null;
        }
    }
}
