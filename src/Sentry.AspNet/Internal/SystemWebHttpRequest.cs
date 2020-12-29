using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Sentry.Extensibility;

namespace Sentry.AspNet.Internal
{
    internal class SystemWebHttpRequest : IHttpRequest
    {
        private readonly HttpRequest _request;

        public long? ContentLength => _request?.ContentLength;

        public string? ContentType => _request?.ContentType;

        public Stream? Body => _request?.InputStream;

        public IEnumerable<KeyValuePair<string, IEnumerable<string>>>? Form
            => _request.Form.AllKeys.Select(kv => new KeyValuePair<string, IEnumerable<string>>(kv, _request.Form.GetValues(kv)));

        public SystemWebHttpRequest(HttpRequest request) => _request = request;
    }
}
