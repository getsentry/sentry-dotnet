namespace Sentry.Extensibility
{
    /// <summary>
    /// Base type for payload extraction.
    /// </summary>
    public abstract class BaseRequestPayloadExtractor : IRequestPayloadExtractor
    {
        /// <summary>
        /// Extract the payload of the <see cref="IHttpRequest"/>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Whether this implementation supports the <see cref="IHttpRequest"/>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract bool IsSupported(IHttpRequest request);

        /// <summary>
        /// The extraction that gets called in case <see cref="IsSupported"/> is true.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract object DoExtractPayLoad(IHttpRequest request);
    }
}
