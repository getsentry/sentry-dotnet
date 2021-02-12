using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sentry.Internal.Extensions;

namespace Sentry
{
    /// <summary>
    /// Sentry HTTP interface.
    /// </summary>
    /// <example>
    /// "request": {
    ///     "url": "http://absolute.uri/foo",
    ///     "method": "POST",
    ///     "data": {
    ///         "foo": "bar"
    ///     },
    ///     "query_string": "hello=world",
    ///     "cookies": "foo=bar",
    ///     "headers": {
    ///         "Content-Type": "text/html"
    ///     },
    ///     "env": {
    ///         "REMOTE_ADDR": "192.168.0.1"
    ///     }
    /// }
    /// </example>
    /// <see href="https://develop.sentry.dev/sdk/event-payloads/request/"/>
    public sealed class Request : IJsonSerializable
    {
        internal Dictionary<string, string>? InternalEnv { get; set; }

        internal Dictionary<string, string>? InternalOther { get; set; }

        internal Dictionary<string, string>? InternalHeaders { get; set; }

        /// <summary>
        /// Gets or sets the full request URL, if available.
        /// </summary>
        /// <value>The request URL.</value>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the method of the request.
        /// </summary>
        /// <value>The HTTP method.</value>
        public string? Method { get; set; }

        // byte[] or Memory<T>?
        // TODO: serializable object or string?
        /// <summary>
        /// Submitted data in whatever format makes most sense.
        /// </summary>
        /// <remarks>
        /// This data should not be provided by default as it can get quite large.
        /// </remarks>
        /// <value>The request payload.</value>
        public object? Data { get; set; }

        /// <summary>
        /// Gets or sets the unparsed query string.
        /// </summary>
        /// <value>The query string.</value>
        public string? QueryString { get; set; }

        /// <summary>
        /// Gets or sets the cookies.
        /// </summary>
        /// <value>The cookies.</value>
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
        /// Gets or sets the optional environment data.
        /// </summary>
        /// <remarks>
        /// This is where information such as IIS/CGI keys go that are not HTTP headers.
        /// </remarks>
        /// <value>The env.</value>
        public IDictionary<string, string> Env => InternalEnv ??= new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets some optional other data.
        /// </summary>
        /// <value>The other.</value>
        public IDictionary<string, string> Other => InternalOther ??= new Dictionary<string, string>();

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <remarks>
        /// This is a shallow copy.
        /// References like <see cref="Data"/> could hold a mutable, non-thread-safe object.
        /// </remarks>
        public Request Clone()
        {
            var request = new Request();

            CopyTo(request);

            return request;
        }

        internal void CopyTo(Request? request)
        {
            if (request == null)
            {
                return;
            }

            request.Url ??= Url;
            request.Method ??= Method;
            request.Data ??= Data;
            request.QueryString ??= QueryString;
            request.Cookies ??= Cookies;

            InternalEnv?.TryCopyTo(request.Env);
            InternalOther?.TryCopyTo(request.Other);
            InternalHeaders?.TryCopyTo(request.Headers);
        }

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            // Env
            if (InternalEnv is {} env && env.Any())
            {
                writer.WriteDictionary("env", env!);
            }

            // Other
            if (InternalOther is {} other && other.Any())
            {
                writer.WriteDictionary("other", other!);
            }

            // Headers
            if (InternalHeaders is {} headers && headers.Any())
            {
                writer.WriteDictionary("headers", headers!);
            }

            // Url
            if (!string.IsNullOrWhiteSpace(Url))
            {
                writer.WriteString("url", Url);
            }

            // Method
            if (!string.IsNullOrWhiteSpace(Method))
            {
                writer.WriteString("method", Method);
            }

            // Data
            if (Data is {} data)
            {
                writer.WriteDynamic("data", data);
            }

            // Query
            if (!string.IsNullOrWhiteSpace(QueryString))
            {
                writer.WriteString("query_string", QueryString);
            }

            // Cookies
            if (!string.IsNullOrWhiteSpace(Cookies))
            {
                writer.WriteString("cookies", Cookies);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses from JSON.
        /// </summary>
        public static Request FromJson(JsonElement json)
        {
            var env = json.GetPropertyOrNull("env")?.GetDictionary();
            var other = json.GetPropertyOrNull("other")?.GetDictionary();
            var headers = json.GetPropertyOrNull("headers")?.GetDictionary();
            var url = json.GetPropertyOrNull("url")?.GetString();
            var method = json.GetPropertyOrNull("method")?.GetString();
            var data = json.GetPropertyOrNull("data")?.GetDynamic();
            var query = json.GetPropertyOrNull("query_string")?.GetString();
            var cookies = json.GetPropertyOrNull("cookies")?.GetString();

            return new Request
            {
                InternalEnv = env?.ToDictionary()!,
                InternalOther = other?.ToDictionary()!,
                InternalHeaders = headers?.ToDictionary()!,
                Url = url,
                Method = method,
                Data = data,
                QueryString = query,
                Cookies = cookies
            };
        }
    }
}
