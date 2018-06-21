using System;
using System.Net;
using System.Net.Http;
using Sentry.Internal.Http;

namespace Sentry.Testing
{
    public static class SentryResponses
    {
        private const string ResponseIdString = "fc6d8c0c43fc4630ad850ee518f1b9d0";
        public const string SentryOkResponseBody = "{\n    \"id\": \"" + ResponseIdString + "\"\n    }";

        public static Guid ResponseId => new Guid(ResponseIdString);

        public static HttpContent GetOkContent() => new StringContent(SentryOkResponseBody);
        public static HttpContent GetBadGatewayContent() => new StringContent(string.Empty);

        public static HttpResponseMessage GetOkResponse()
            => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = GetOkContent()
            };

        public static HttpResponseMessage GetErrorResponse(HttpStatusCode code, string errorMessage)
        {
            var response = new HttpResponseMessage(code)
            {
                Content = GetBadGatewayContent()
            };

            if (errorMessage != null)
            {
                response.Headers.Add(SentryHeaders.SentryErrorHeader, errorMessage);
            }

            return response;
        }
    }
}
