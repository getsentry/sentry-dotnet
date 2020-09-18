using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Sentry.Protocol
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
    /// <see href="https://docs.sentry.io/clientdev/interfaces/http/"/>
    [DataContract]
    public class Request
    {
        [DataMember(Name = "env", EmitDefaultValue = false)]
        internal Dictionary<string, string>? InternalEnv { get; set; }

        [DataMember(Name = "other", EmitDefaultValue = false)]
        internal Dictionary<string, string>? InternalOther { get; set; }

        [DataMember(Name = "headers", EmitDefaultValue = false)]
        internal Dictionary<string, string>? InternalHeaders { get; set; }

        /// <summary>
        /// Gets or sets the full request URL, if available.
        /// </summary>
        /// <value>The request URL.</value>
        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the method of the request.
        /// </summary>
        /// <value>The HTTP method.</value>
        [DataMember(Name = "method", EmitDefaultValue = false)]
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
        [DataMember(Name = "data", EmitDefaultValue = false)]
        public object? Data { get; set; }

        /// <summary>
        /// Gets or sets the unparsed query string.
        /// </summary>
        /// <value>The query string.</value>
        [DataMember(Name = "query_string", EmitDefaultValue = false)]
        public string? QueryString { get; set; }

        /// <summary>
        /// Gets or sets the cookies.
        /// </summary>
        /// <value>The cookies.</value>
        [DataMember(Name = "cookies", EmitDefaultValue = false)]
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
    }
}
