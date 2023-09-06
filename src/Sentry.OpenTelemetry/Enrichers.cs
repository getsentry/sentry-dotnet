using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry.OpenTelemetry;

internal static class Enrichers
{
    public static void GraphQl(Activity data, ISpan span, SentryOptions? options)
    {
        if (span.Extra.ContainsKey(SemanticConventions.AttributeGraphQlOperationType))
        {
            var transaction = span.GetTransaction();
            transaction.Request.ApiTarget = "graphql";
        }
    }
}
