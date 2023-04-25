using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Sentry Response context interface.
/// </summary>
/// <example>
///{
///  "contexts": {
///    "response": {
///      "cookies": "PHPSESSID=298zf09hf012fh2; csrftoken=u32t4o3tb3gg43; _gat=1;",
///      "headers": {
///        "content-type": "text/html"
///        /// ...
///      },
///      "status_code": 500,
///      "body_size": 1000, // in bytes
///    }
///  }
///}
/// </example>
/// <see href="https://develop.sentry.dev/sdk/event-payloads/types/#responsecontext"/>
public sealed class ResponseContext : IJsonSerializable
{
    internal Dictionary<string, string>? InternalHeaders { get; set; }

    /// <summary>
    /// Gets or sets the HTTP response body size.
    /// </summary>
    /// <value>The request URL.</value>
    public long? BodySize { get; set; }

    /// <summary>
    /// Gets or sets (optional) cookie values
    /// </summary>
    /// <value>The other.</value>
    public string? Cookies { get; set; }

    /// <summary>
    /// Gets or sets the headers.
    /// </summary>
    /// <remarks>
    /// If a header appears multiple times it needs to be merged according to the HTTP standard for header merging.
    /// </remarks>
    /// <value>The headers.</value>
    public IDictionary<string, string> Headers => InternalHeaders ??= new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the HTTP Status response code
    /// </summary>
    /// <value>The HTTP method.</value>
    public short? StatusCode { get; set; }

    internal void AddHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
    {
        foreach (var header in headers)
        {
            Headers.Add(
                header.Key,
                string.Join("; ", header.Value)
            );
        }
    }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    public ResponseContext Clone()
    {
        var response = new ResponseContext();

        CopyTo(response);

        return response;
    }

    internal void CopyTo(ResponseContext? response)
    {
        if (response == null)
        {
            return;
        }

        response.BodySize ??= BodySize;
        response.Cookies ??= Cookies;
        response.StatusCode ??= StatusCode;

        InternalHeaders?.TryCopyTo(response.Headers);
    }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteNumberIfNotNull("body_size", BodySize);
        writer.WriteStringIfNotWhiteSpace("cookies", Cookies);
        writer.WriteStringDictionaryIfNotEmpty("headers", InternalHeaders!);
        writer.WriteNumberIfNotNull("status_code", StatusCode);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static ResponseContext FromJson(JsonElement json)
    {
        var bodySize = json.GetPropertyOrNull("body_size")?.GetInt64();
        var cookies = json.GetPropertyOrNull("cookies")?.GetString();
        var headers = json.GetPropertyOrNull("headers")?.GetStringDictionaryOrNull();
        var statusCode = json.GetPropertyOrNull("status_code")?.GetInt16();

        return new ResponseContext
        {
            BodySize = bodySize,
            Cookies = cookies,
            InternalHeaders = headers?.WhereNotNullValue().ToDictionary(),
            StatusCode = statusCode
        };
    }
}
