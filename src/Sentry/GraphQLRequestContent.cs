using System.Text.Encodings.Web;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Internal.GraphQL;

namespace Sentry;

internal class GraphQLRequestContent
{
    private const string QueryKey = "query";
    private const string VariablesKey = "variables";

    private static JsonSerializerOptions SerializerOptions => new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static readonly Regex Expression = new(
        @"\s*(?<operationType>\bquery\b|\bmutation\b|\bsubscription\b)\s*(?<operationName>\w+)?\s*(?<query>{.*})\s*",
        RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.IgnoreCase
        );

    private readonly SentryOptions? _options;

    private IReadOnlyDictionary<string, object> Items { get; }

    public GraphQLRequestContent(string? requestContent, SentryOptions? options = null)
    {
        RequestContent = requestContent;
        _options = options;
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
        if (Items.TryGetValue(QueryKey, out var query))
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

    internal string? FilterRequestContent(bool includeDocument, bool includeVariables)
    {
        if (RequestContent is null || (includeDocument && includeVariables))
        {
            return RequestContent;
        }

        if (!includeDocument && !includeVariables)
        {
            return null;
        }

        var excludedKey = includeDocument ? VariablesKey : QueryKey;
        var filtered = WithoutMember(RequestContent, excludedKey);
        if (filtered is null)
        {
            _options?.LogDebug("Dropping the GraphQL request body, as it isn't a JSON GraphQL request that '{0}' can be removed from.", excludedKey);
        }
        return filtered;
    }

    private static string? WithoutMember(string requestContent, string excludedKey)
    {
        try
        {
            using var document = JsonDocument.Parse(requestContent);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            {
                var root = document.RootElement;
                switch (root.ValueKind)
                {
                    case JsonValueKind.Object:
                        WriteWithout(writer, root, excludedKey);
                        break;
                    case JsonValueKind.Array:
                        writer.WriteStartArray();
                        foreach (var request in root.EnumerateArray())
                        {
                            if (request.ValueKind != JsonValueKind.Object)
                            {
                                return null;
                            }
                            WriteWithout(writer, request, excludedKey);
                        }
                        writer.WriteEndArray();
                        break;
                    default:
                        return null;
                }
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void WriteWithout(Utf8JsonWriter writer, JsonElement request, string excludedKey)
    {
        writer.WriteStartObject();
        foreach (var property in request.EnumerateObject())
        {
            if (!property.NameEquals(excludedKey))
            {
                property.WriteTo(writer);
            }
        }
        writer.WriteEndObject();
    }

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
