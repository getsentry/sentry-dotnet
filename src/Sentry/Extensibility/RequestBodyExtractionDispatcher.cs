using System;
using System.Collections.Generic;

namespace Sentry.Extensibility
{
    /// <summary>
    /// Dispatches request body extractions if enabled and within limits.
    /// </summary>
    public class RequestBodyExtractionDispatcher : IRequestPayloadExtractor
    {
        private readonly SentryOptions _options;
        private readonly Func<RequestSize> _sizeSwitch;

        internal IEnumerable<IRequestPayloadExtractor> Extractors { get; }

        /// <summary>
        /// Creates a new instance of <see cref="RequestBodyExtractionDispatcher"/>.
        /// </summary>
        /// <param name="extractors">Extractors to use.</param>
        /// <param name="options">Sentry Options.</param>
        /// <param name="sizeSwitch">The max request size to capture.</param>
        public RequestBodyExtractionDispatcher(IEnumerable<IRequestPayloadExtractor> extractors, SentryOptions options, Func<RequestSize> sizeSwitch)
        {
            Extractors = extractors ?? throw new ArgumentNullException(nameof(extractors));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _sizeSwitch = sizeSwitch ?? throw new ArgumentNullException(nameof(sizeSwitch));
        }

        /// <summary>
        /// Extract the payload using the provided extractors.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>A serializable representation of the payload.</returns>
        public object? ExtractPayload(IHttpRequest request)
        {
            // Not to throw on code that ignores nullability warnings.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (request is null)
            {
                return null;
            }

            var size = _sizeSwitch();

            switch (size)
            {
                case RequestSize.Small when request.ContentLength < 1_000:
                case RequestSize.Medium when request.ContentLength < 10_000:
                case RequestSize.Always:
                    _options.DiagnosticLogger?.LogDebug("Attempting to read request body of size: {0}, configured max: {1}.",
                        request.ContentLength, size);

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
                    break;
                // Request body extraction is opt-in
                case RequestSize.None:
                    _options.DiagnosticLogger?.LogDebug("Skipping request body extraction.");
                    return null;
            }

            _options.DiagnosticLogger?.LogWarning("Ignoring request with Size {0} and configuration RequestSize {1}",
                request.ContentLength, size);

            return null;
        }
    }
}
