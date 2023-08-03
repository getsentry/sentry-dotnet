using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Special HTTP message handler that can be used to propagate Sentry headers and other contextual information.
/// </summary>
public class SentryGraphQLHttpMessageHandler : SentryMessageHandler
{
    /// <summary>
    /// Constructs an instance of <see cref="SentryHttpMessageHandler"/>.
    /// </summary>
    /// <param name="innerHandler">An inner message handler to delegate calls to.</param>
    /// <param name="hub">The Sentry hub.</param>
    public SentryGraphQLHttpMessageHandler(HttpMessageHandler? innerHandler = default, IHub? hub = default)
        : this(hub, default, innerHandler)
    {
    }

    internal SentryGraphQLHttpMessageHandler(
        IHub? hub, SentryOptions? options, HttpMessageHandler? innerHandler = default,
        ISentryFailedRequestHandler? failedRequestHandler = null
    ) : base(hub, options, innerHandler, failedRequestHandler, SentryGraphQLHttpFailedRequestHandler.Create)
    {
    }

    /// <inheritdoc />
    protected internal override void OnProcessRequest(HttpRequestMessage request, ISpan? span, string method, string url)
    {
        UnpackGraphQLRequest(request);
    }

    internal class GraphQLRequestInfo
    {
        public string? OperationName { get; set; }
        public string? OperationType { get; set; }
        public string? Query { get; set; }

        /// <summary>
        /// GraphQL operation name, type (`query`, `mutation` or `subscription`) and status code, if possible.
        /// Fallback to something sensible/unique otherwise (e.g. the canonical name of the actual/generated class.)
        /// </summary>
        public string? GetSpanDescriptionOrDefault(HttpStatusCode statusCode)
        {
            var parts = new List<string>();
            if (OperationName is { } operationName)
            {
                parts.Add(operationName);
            }

            if (OperationType is { } operationType)
            {
                parts.Add(operationType);
            }

            var description = string.Join(" ", parts);
            return description != string.Empty ? $"{description} {(int)statusCode}" : null;
        }

        public void AddToBreadcrumbData(Dictionary<string, string> data)
        {
            AddIfNotNull("operation_name", OperationName); // The GraphQL operation name
            AddIfNotNull("operation_type", OperationType); // i.e. `query`, `mutation`, `subscription`
            // AddIfNotNull("operation_id", ???); // The GraphQL operation ID... not included in the request/response

            void AddIfNotNull(string key, string? possibleValue)
            {
                if (possibleValue is { } value)
                {
                    data[key] = value;
                }
            }
        }
    }

    private void UnpackGraphQLRequest(HttpRequestMessage request)
    {
        // Need the reverse of:
        // Content = new StringContent(serializer.SerializeToString(this), Encoding.UTF8, options.MediaType)
        // TODO: This is a hack... we should pass a deserialization function in via the constructor
        var json = TryGetJsonContent(request.Content);

        if (json is not { } jsonElement)
        {
            return;
        }

        var graphqlInfo = request.With<GraphQLRequestInfo>();
        if (TryGetStringProperty(jsonElement, "operationName") is { } operationName)
        {
            graphqlInfo.OperationName = operationName;
        }
        if (TryGetStringProperty(jsonElement, "query") is { } query)
        {
            graphqlInfo.OperationType = "query";
            graphqlInfo.Query = query;
        }
    }

    private JsonElement? TryGetJsonContent(HttpContent? content) =>
        content is StringContent stringContent ? stringContent.ReadAsJson() : null;

    private string? TryGetStringProperty(JsonElement jsonElement, string propertyName)
    {
        if (jsonElement.TryGetProperty(propertyName, out var operationElement))
        {
            var value = operationElement.GetString();
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }
        return null;
    }

    /// <inheritdoc />
    protected internal override Breadcrumb GetBreadcrumb(HttpResponseMessage response, ISpan? span, string method,
        string url)
    {
        var breadcrumbData = new Dictionary<string, string>
        {
            {"url", url},
            {"method", method},
            {"status_code", ((int) response.StatusCode).ToString()}
        };
        var graphqlRequestInfo = response.RequestMessage?.With<GraphQLRequestInfo>();
        graphqlRequestInfo?.AddToBreadcrumbData(breadcrumbData);

        var category = graphqlRequestInfo?.OperationType ?? "graphql.operation";

        return new Breadcrumb(
            SystemClock.Clock.GetUtcNow(),
            type: "graphql",
            category: category,
            data: breadcrumbData
        );
    }

    /// <inheritdoc />
    protected internal override void OnBeforeFinishSpan(HttpResponseMessage response, ISpan span, string method, string url)
    {
        // TODO: See how we can determine the span status for a GraphQL request... this is the guidance for Http Requests
        span.Status = SpanStatusConverter.FromHttpStatusCode(response.StatusCode); // TODO: Don't do this if the span is errored
        span.Description = response.RequestMessage?.With<GraphQLRequestInfo>()
            .GetSpanDescriptionOrDefault(response.StatusCode) ?? $"{method} {url}";
    }
}
