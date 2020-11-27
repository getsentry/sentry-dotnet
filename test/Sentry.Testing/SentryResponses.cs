using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace Sentry.Testing
{
    public static class SentryResponses
    {
        private const string ResponseIdString = "fc6d8c0c43fc4630ad850ee518f1b9d0";
        public const string SentryOkResponseBody = "{\n    \"id\": \"" + ResponseIdString + "\"\n    }";

        public static Guid ResponseId => new Guid(ResponseIdString);

        public static HttpContent GetOkContent() => new StringContent(SentryOkResponseBody);

        public static HttpResponseMessage GetOkResponse()
            => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = GetOkContent()
            };

        public static HttpResponseMessage GetErrorResponse(HttpStatusCode code, string errorMessage)
        {
            var responseContent = JsonSerializer.Serialize(new {detail = errorMessage});
            return new HttpResponseMessage(code) {Content = new StringContent(responseContent)};
        }

        public static HttpResponseMessage GetRateLimitResponse(string rateLimitHeaderValue)
        {
            return new HttpResponseMessage((HttpStatusCode)429)
            {
                Headers = {{"X-Sentry-Rate-Limits", rateLimitHeaderValue}}
            };
        }
    }
}
