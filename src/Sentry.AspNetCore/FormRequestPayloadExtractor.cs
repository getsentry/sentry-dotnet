using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Sentry.AspNetCore
{
    public class FormRequestPayloadExtractor : BaseRequestPayloadExtractor
    {
        private const string SupportedContentType = "application/x-www-form-urlencoded";

        protected override bool IsSupported(HttpRequest request)
            => SupportedContentType
                .Equals(request.ContentType, StringComparison.InvariantCulture);

        protected override object DoExtractPayLoad(HttpRequest request)
            => request.Form?.ToDictionary(k => k.Key, v => v.Value);
    }
}
