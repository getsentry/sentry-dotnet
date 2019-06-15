using System;
using System.Linq;

namespace Sentry.Extensibility
{
    public class FormRequestPayloadExtractor : BaseRequestPayloadExtractor
    {
        private const string SupportedContentType = "application/x-www-form-urlencoded";

        protected override bool IsSupported(IHttpRequest request)
            => SupportedContentType
                .Equals(request.ContentType, StringComparison.InvariantCulture);

        protected override object? DoExtractPayLoad(IHttpRequest request)
            => request.Form?.ToDictionary(k => k.Key, v => v.Value);
    }
}
