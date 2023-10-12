using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

internal class GraphQLRequestContent
{
    private static JsonSerializerOptions SerializerOptions => new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static readonly Regex Expression = new (
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

/// <summary>
/// Adapted from https://github.com/graphql-dotnet/graphql-dotnet/blob/42a299e77748ec588bf34c33334e985098563298/src/GraphQL.SystemTextJson/GraphQLRequestJsonConverter.cs#L64
/// </summary>
internal static class GraphQLRequestContentReader
{
    /// <summary>
    /// Name for the operation name parameter.
    /// See https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#request-parameters
    /// </summary>
    private const string OperationNameKey = "operationName";

    /// <summary>
    /// Name for the query parameter.
    /// See https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#request-parameters
    /// </summary>
    private const string QueryKey = "query";


    public static IReadOnlyDictionary<string, object> Read(string requestContent)
    {
        Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(requestContent));
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of object");
        }

        var request = new Dictionary<string, object>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return request;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name");
            }

            var key = reader.GetString()!;

            if (!reader.Read())
            {
                throw new JsonException("unexpected end of data");
            }

            switch (key)
            {
                case QueryKey:
                case OperationNameKey:
                    request[key] = reader.GetString()!;
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException("unexpected end of data");
    }
}
