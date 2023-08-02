using Sentry.Extensibility;
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
    public SentryGraphQLHttpMessageHandler()
        : this(default, default, default) { }

    /// <summary>
    /// Constructs an instance of <see cref="SentryHttpMessageHandler"/>.
    /// </summary>
    /// <param name="innerHandler">An inner message handler to delegate calls to.</param>
    public SentryGraphQLHttpMessageHandler(HttpMessageHandler innerHandler)
        : this(default, default, innerHandler) { }

    /// <summary>
    /// Constructs an instance of <see cref="SentryHttpMessageHandler"/>.
    /// </summary>
    /// <param name="hub">The Sentry hub.</param>
    public SentryGraphQLHttpMessageHandler(IHub hub)
        : this(hub, default)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="SentryHttpMessageHandler"/>.
    /// </summary>
    /// <param name="innerHandler">An inner message handler to delegate calls to.</param>
    /// <param name="hub">The Sentry hub.</param>
    public SentryGraphQLHttpMessageHandler(HttpMessageHandler innerHandler, IHub hub)
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
    protected override ISpan DoStartChildSpan(ISpan parentSpan, HttpRequestMessage request, string method, string url)
    {
        // Need the reverse of:
        // Content = new StringContent(serializer.SerializeToString(this), Encoding.UTF8, options.MediaType)
        // TODO: This is a hack... we should pass a deserialization function in via the constructor
        var json = TryGetJsonContent(request.Content);

        // The operation type should follow the [Span Operation Conventions](https://develop.sentry.dev/sdk/performance/span-operations/).
        var operation = "http.client";
        // GraphQL operation name, operation type (`query`, `mutation` or `subscription`) and status code, if possible.
        // otherwise fallback to something unique that makes sense, e.g. the canonical name of the actual/generated class.
        var description = TryGetSpanDescription(json) ?? $"{method} {url}";

        var span = parentSpan.StartChild(operation, description);
        return span;
    }

    private JsonElement? TryGetJsonContent(HttpContent? content) =>
        content is StringContent stringContent ? stringContent.ReadAsJson() : null;

    /// <summary>
    /// GraphQL operation name, operation type (`query`, `mutation` or `subscription`) and status code, if possible.
    /// otherwise fallback to something unique that makes sense, e.g. the canonical name of the actual/generated class.
    /// </summary>
    private string? TryGetSpanDescription(JsonElement? json)
    {
        if (json is not { } jsonElement)
        {
            return null;
        }

        var parts = new List<string>();
        if (TryGetStringProperty(jsonElement, "operationName") is { } operationName)
        {
            parts.Add(operationName);
        }

        if (jsonElement.TryGetProperty("query", out _))
        {
            parts.Add("query");
        }

        var description = string.Join(" ", parts);
        return description != string.Empty ? description : null;
    }

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
    protected override void DoAddBreadcrumb(IHub hub, HttpResponseMessage response, ISpan? span, string method,
        string url)
    {
        // Additional fields for breadcrumbs:
        // - data (all fields are optional but recommended):
        // - `operation_name` - The GraphQL operation name
        //     - `operation_type` - The GraphQL operation type, i.e: `query`, `mutation`, `subscription`
        // - `operation_id` - The GraphQL operation ID
        //
        //     Avoid setting the query String as part of the data field since the event can be dropped due to size limit.
        //
        //     In case more additional fields are needed, the data field can be used to add more context, e.g. `graphql.path`,
        //     `graphql.field`, `graphql.type`, etc.
        //
        //     The `category` can also be adapted to its own type, e.g. `graphql.resolver`, `graphql.data_loader`, etc.
        //
        //     For resolvers or data fetchers a breadcrumb could have the following fields:
        // - `type` = `graphql`
        // - `category` = `graphql.fetcher`
        // - `path` - Path in the query, e.g. `project/status`
        // - `field` - Field being fetched, e.g. `status`
        // - `type` - Type being fetched, e.g. `String`
        // - `object_type` - Object type being fetched, e.g. `Project`
        //
        // For data loaders a breadcrumb could have the following fields:
        // - `type` = graphql
        //     - `category` = `graphql.data_loader`
        // - `keys` - Keys that should be loaded by the data loader
        //     - `key_type` - Type of the key
        //     - `value_type` - Type of the value
        //     - `name` - Name of the data loader
        var breadcrumbData = new Dictionary<string, string>
        {
            {"url", url},
            {"method", method},
            {"status_code", ((int) response.StatusCode).ToString()}
        };
        // The Breadcrumb `type` should be `graphql` and the `category` should be the operation type, otherwise `graphql.operation`
        // if not available.
        hub.AddBreadcrumb(string.Empty, "graphql.operation", "graphql", breadcrumbData);
    }

    /// <inheritdoc />
    protected override SpanStatus DetermineSpanStatus(HttpResponseMessage response) =>
        // TODO: See how we can determine the span status for a GraphQL request... this is the guidance for Http Requests
        // - span status must match HTTP response status code (see [Span status to HTTP status code mapping](https://develop.sentry.dev/sdk/event-payloads/span/))
        // - when network error occurs, span status must be set to `internal_error`
        SpanStatusConverter.FromHttpStatusCode(response.StatusCode);
}
