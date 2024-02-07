using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Internal.GraphQL;

namespace Sentry;

internal class GraphQLRequestContent
{
    private static JsonSerializerOptions SerializerOptions => new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static readonly Regex Expression = new(
        @"\s*(?<operationType>\bquery\b|\bmutation\b|\bsubscription\b)\s*(?<operationName>\w+)?\s*(?<query>{.*})\s*",
        RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.IgnoreCase
        );

    private IReadOnlyDictionary<string, object> Items { get; }

    public GraphQLRequestContent(string? requestContent, SentryOptions? options = null)
    {
        RequestContent = requestContent;
        if (requestContent is null)
        {
            Items = new Dictionary<string, object>().AsReadOnly();
            return;
        }

        try
        {
            Items = GraphQLRequestContentReader.Read(requestContent);
        }
        catch (Exception e)
        {
            options?.LogDebug($"Unable to parse GraphQL request content: {e.Message}");
            Items = new Dictionary<string, object>().AsReadOnly();
            return;
        }

        // Try to read the values directly from the array (in case they've been supplied explicitly)
        if (Items.TryGetValue("operationName", out var operationName))
        {
            OperationName = operationName?.ToString();
        }
        // TODO: The query can be null... see https://www.apollographql.com/docs/apollo-server/performance/apq/
        if (Items.TryGetValue("query", out var query))
        {
            Query = query?.ToString();
        }

        var match = Expression.Match(Query ?? requestContent);

        if (match.Success)
        {
            OperationType ??= match.Groups["operationType"].Value;
            OperationName ??= match.Groups["operationName"].Value;
        }

        // Default to "query" if the operation type wasn't explicitly specified
        if (string.IsNullOrEmpty(OperationType))
        {
            OperationType = "query";
        }
    }

    internal string? RequestContent { get; }

    /// <summary>
    /// Document containing GraphQL to execute.
    /// It can be null for automatic persisted queries, in which case a SHA-256 hash of the query would be sent in the
    /// Extensions. See https://www.apollographql.com/docs/apollo-server/performance/apq/ for details.
    /// </summary>
    public string? Query { get; }
    public string? OperationName { get; }
    public string? OperationType { get; }

    /// <summary>
    /// Returns the OperationName if present or "graphql" otherwise.
    /// </summary>
    public string OperationNameOrFallback() => OperationName ?? "graphql";

    /// <summary>
    /// Returns the OperationType if present or "graphql.operation" otherwise.
    /// </summary>
    public string OperationTypeOrFallback() => OperationType ?? "graphql.operation";
}
