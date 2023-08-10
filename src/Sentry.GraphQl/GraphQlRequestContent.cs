namespace Sentry.GraphQl;

internal class GraphQlRequestContent
{
    private static JsonSerializerOptions SerializerOptions => new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static readonly Regex Expression = new (
        @"\s*(?<operationType>\bquery\b|\bmutation\b|\bsubscription\b)\s*(?<operationName>\w+)?\s*(?<query>{.*})\s*",
        RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.IgnoreCase
        );

    private string? RequestContent { get; }
    private IReadOnlyDictionary<string, object> Items { get; }

    public GraphQlRequestContent(string? requestContent)
    {
        RequestContent = requestContent;
        if (requestContent is null)
        {
            Items = new Dictionary<string, object>().AsReadOnly();
            return;
        }

        var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(requestContent, SerializerOptions);
        Items = (deserialized ?? new Dictionary<string, object>()).AsReadOnly();

        // Try to read the values directly from the array (in case they've been supplied explicitly)
        if (Items.TryGetValue("operationName", out var operationName))
        {
            OperationName = operationName.ToString();
        }
        // TODO: The query can be null... see https://www.apollographql.com/docs/apollo-server/performance/apq/
        if (Items.TryGetValue("query", out var query))
        {
            Query = query.ToString();
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

    /// <summary>
    /// Document containing GraphQL to execute.
    /// It can be null for automatic persisted queries, in which case a SHA-256 hash of the query would be sent in the
    /// Extensions. See https://www.apollographql.com/docs/apollo-server/performance/apq/ for details.
    /// </summary>
    public string? Query { get; }
    public string? OperationName { get; }
    public string? OperationType { get; }
}
