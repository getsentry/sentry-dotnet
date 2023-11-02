namespace Sentry.Internal.GraphQL;

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
