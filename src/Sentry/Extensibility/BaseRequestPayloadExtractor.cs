namespace Sentry.Extensibility
{
    public abstract class BaseRequestPayloadExtractor : IRequestPayloadExtractor
    {
        public object ExtractPayload(IHttpRequest request)
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

        protected abstract bool IsSupported(IHttpRequest request);

        protected abstract object DoExtractPayLoad(IHttpRequest request);
    }
}
