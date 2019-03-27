using System.Collections.Generic;

namespace Sentry.Extensibility
{
    public class RequestBodyExtractionDispatcher : IRequestPayloadExtractor
    {
        private readonly SentryOptions _options;
        private readonly RequestSize _size;

        internal IEnumerable<IRequestPayloadExtractor> Extractors { get; }

        public RequestBodyExtractionDispatcher(IEnumerable<IRequestPayloadExtractor> extractors, SentryOptions options, RequestSize size)
        {
            Extractors = extractors;
            _options = options;
            _size = size;
        }

        public object ExtractPayload(IHttpRequest request)
        {
            if (request == null)
            {
                return null;
            }

            var extractRequestBody = false;
            switch (_size)
            {
                case RequestSize.Small when request.ContentLength < 1_000:
                    extractRequestBody = true;
                    break;
                case RequestSize.Medium when request.ContentLength < 10_000:
                    extractRequestBody = true;
                    break;
                case RequestSize.Large:
                    extractRequestBody = true;
                    break;
                // Request body extraction is opt-in
                case RequestSize.None:
                    _options.DiagnosticLogger.LogDebug("Skipping request body extraction.");
                    break;
                default:
                    _options.DiagnosticLogger.LogWarning("Unknown RequestSize {0}", _size);
                    break;
            }

            if (extractRequestBody)
            {
                _options.DiagnosticLogger.LogDebug("Attempting to read request body of size: {0}, configured max: {1}.",
                    request.ContentLength, _size);

                foreach (var extractor in Extractors)
                {
                    var data = extractor.ExtractPayload(request);

                    if (data == null
                        || data is string dataString
                        && string.IsNullOrEmpty(dataString))
                    {
                        continue;
                    }

                    return data;
                }
            }

            return null;
        }
    }
}
