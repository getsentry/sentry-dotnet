namespace Sentry.Tests;

internal static class SentryGraphQlTestHelpers
{
    /// <summary>
    /// GraphQL Queries get sent in a dictionary using the GraphQL over HTTP protocol
    /// </summary>
    public static string WrapRequestContent(string queryText)
    {
        var wrapped = new Dictionary<string, object>()
        {
            { "query", queryText }
        };
        return wrapped.ToJsonString();
    }

    public static HttpRequestMessage GetRequestQuery(string query, string url = "http://foo")
    {
        var content = query is not null
            ? new StringContent(WrapRequestContent(query))
            : null;
        return GetRequest(content, url);
    }

    public static HttpRequestMessage GetRequest(HttpContent content, string url = "http://foo") => new(HttpMethod.Post, new Uri(url))
    {
        Content = content
    };

    public static StringContent JsonContent(dynamic json)
    {
        var serialised = JsonSerializer.Serialize(json);
        return new(serialised, Encoding.UTF8,
            "application/json")
        {
            Headers = { ContentLength = serialised.Length }
        };
    }

    public static StringContent ResponesContent(string responseText) => JsonContent(
        new
        {
            data = responseText
        }
    );

    /// <summary>
    /// e.g.
    /// "[{"message":"Query does not contain operation \u0027getAllNotes\u0027.","extensions":{"code":"INVALID_OPERATION","codes":["INVALID_OPERATION"]}}]"
    /// </summary>
    public static StringContent ErrorContent(string errorMessage, string errorCode) => JsonContent(
        new dynamic[]
        {
            new
            {
                message = errorMessage,
                extensions = new {
                    code = errorCode,
                    codes = new dynamic[]{ errorCode }
                }
            }
        }
    );
}
