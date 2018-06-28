using Microsoft.AspNetCore.Http;

namespace Sentry.AspNetCore
{
    public abstract class BaseRequestPayloadExtractor : IRequestPayloadExtractor
    {
        public object ExtractPayload(HttpRequest request)
        {
            if (!request.Body.CanSeek
                || !request.Body.CanRead
                || !IsSupported(request))
            {
                return null;
            }

            var originalPosition = request.Body.Position;
            try
            {
                request.Body.Position = 0;

                return DoExtractPayLoad(request);
            }
            finally
            {
                request.Body.Position = originalPosition;
            }
        }

        protected abstract bool IsSupported(HttpRequest request);

        protected abstract object DoExtractPayLoad(HttpRequest request);
    }
}
