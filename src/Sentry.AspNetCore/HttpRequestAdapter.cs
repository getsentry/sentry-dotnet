using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Sentry.Extensibility;

namespace Sentry.AspNetCore
{
    internal class HttpRequestAdapter : IHttpRequest
    {
        private readonly HttpRequest _request;

        public HttpRequestAdapter(HttpRequest request) => _request = request;

        public long? ContentLength => _request.ContentLength;
        public string? ContentType => _request.ContentType;
        public Stream? Body => _request.Body;

        public IEnumerable<KeyValuePair<string, IEnumerable<string>>> Form =>
            _request?.Form.Select(k => new KeyValuePair<string, IEnumerable<string>>(k.Key, k.Value))
            ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>();
    }
}
