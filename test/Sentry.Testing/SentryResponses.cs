using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Sentry.Testing
{
    public static class SentryResponses
    {
        private const string ResponseIdString = "fc6d8c0c43fc4630ad850ee518f1b9d0";
        public const string SentryOkResponseBody = "{\n    \"id\": \"" + ResponseIdString + "\"\n    }";

        public static Guid ResponseId => new(ResponseIdString);

        public static HttpContent GetOkContent() => new StringContent(SentryOkResponseBody);

        public static HttpResponseMessage GetOkResponse()
            => new(HttpStatusCode.OK)
            {
                Content = GetOkContent()
            };

        public static HttpResponseMessage GetJsonErrorResponse(HttpStatusCode code, string detail, string[] causes = null)
        {
            var responseContent = causes != null
                ? JsonSerializer.Serialize(new {detail, causes})
                : JsonSerializer.Serialize(new {detail});

            return new HttpResponseMessage(code) {Content = new StringContent(responseContent, Encoding.UTF8, "application/json") };
        }

        public static HttpResponseMessage GetTextErrorResponse(HttpStatusCode code, string detail)
            => new HttpResponseMessage(code)
            {
                Content = new StringContent(detail)
            };

        public static HttpResponseMessage GetRateLimitResponse(string rateLimitHeaderValue)
        {
            return new((HttpStatusCode)429)
            {
                Headers = {{"X-Sentry-Rate-Limits", rateLimitHeaderValue}}
            };
        }
    }
}
