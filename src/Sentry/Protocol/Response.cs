using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

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
public sealed class Response : ISentryJsonSerializable, ICloneable<Response>, IUpdatable<Response>
{
    /// <summary>
    /// Tells Sentry which type of context this is.
    /// </summary>
    public const string Type = "response";

    internal Dictionary<string, string>? InternalHeaders { get; private set; }

    /// <summary>
    /// Gets or sets the HTTP response body size.
    /// </summary>
    public long? BodySize { get; set; }

    /// <summary>
    /// Gets or sets (optional) cookie values
    /// </summary>
    public string? Cookies { get; set; }

    /// <summary>
    /// Submitted data in whatever format makes most sense.
    /// </summary>
    /// <remarks>
    /// This data should not be provided by default as it can get quite large.
    /// </remarks>
    /// <value>The request payload.</value>
    public object? Data { get; set; }

    /// <summary>
    /// Gets or sets the headers.
    /// </summary>
    /// <remarks>
    /// If a header appears multiple times it needs to be merged according to the HTTP standard for header merging.
    /// </remarks>
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
    public Response Clone()
    {
        var response = new Response();

        response.UpdateFrom(this);

        return response;
    }

    /// <summary>
    /// Updates this instance with data from the properties in the <paramref name="source"/>,
    /// unless there is already a value in the existing property.
    /// </summary>
    public void UpdateFrom(Response source)
    {
        BodySize ??= source.BodySize;
        Cookies ??= source.Cookies;
        Data ??= source.Data;
        StatusCode ??= source.StatusCode;
        source.InternalHeaders?.TryCopyTo(Headers);
    }

    /// <summary>
    /// Updates this instance with data from the properties in the <paramref name="source"/>,
    /// unless there is already a value in the existing property.
    /// </summary>
    public void UpdateFrom(object source)
    {
        if (source is Response response)
        {
            UpdateFrom(response);
        }
    }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteString("type", Type);
        writer.WriteNumberIfNotNull("body_size", BodySize);
        writer.WriteStringIfNotWhiteSpace("cookies", Cookies);
        writer.WriteDynamicIfNotNull("data", Data, logger);
        writer.WriteStringDictionaryIfNotEmpty("headers", InternalHeaders!);
        writer.WriteNumberIfNotNull("status_code", StatusCode);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static Response FromJson(JsonElement json)
    {
        var bodySize = json.GetPropertyOrNull("body_size")?.GetInt64();
        var cookies = json.GetPropertyOrNull("cookies")?.GetString();
        var data = json.GetPropertyOrNull("data")?.GetDynamicOrNull();
        var headers = json.GetPropertyOrNull("headers")?.GetStringDictionaryOrNull();
        var statusCode = json.GetPropertyOrNull("status_code")?.GetInt16();

        return new Response
        {
            BodySize = bodySize,
            Cookies = cookies,
            Data = data,
            InternalHeaders = headers?.WhereNotNullValue().ToDict(),
            StatusCode = statusCode
        };
    }
}
