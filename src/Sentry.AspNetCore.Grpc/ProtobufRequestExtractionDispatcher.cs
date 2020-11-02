using System;
using System.Collections.Generic;
using Google.Protobuf;
using Sentry.Extensibility;

namespace Sentry.AspNetCore.Grpc
{
    /// <summary>
    /// Dispatches request message extractions if enabled and within limits.
    /// </summary>
    public class ProtobufRequestExtractionDispatcher : IProtobufRequestPayloadExtractor
    {
        private readonly SentryOptions _options;
        private readonly Func<RequestSize> _sizeSwitch;

        internal IEnumerable<IProtobufRequestPayloadExtractor> Extractors { get; }

        /// <summary>
        /// Creates a new instance of <see cref="ProtobufRequestExtractionDispatcher"/>.
        /// </summary>
        /// <param name="extractors">Extractors to use.</param>
        /// <param name="options">Sentry Options.</param>
        /// <param name="sizeSwitch">The max request size to capture.</param>
        public ProtobufRequestExtractionDispatcher(IEnumerable<IProtobufRequestPayloadExtractor> extractors,
            SentryOptions options, Func<RequestSize> sizeSwitch)
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
        public IMessage? ExtractPayload<TRequest>(IProtobufRequest<TRequest> request)
            where TRequest : class, IMessage
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
                    _options.DiagnosticLogger?.Log(SentryLevel.Debug,
                        "Attempting to read request body of size: {0}, configured max: {1}.",
                        null, request.ContentLength, size);

                    foreach (var extractor in Extractors)
                    {
                        var data = extractor.ExtractPayload(request);

                        if (data == null)
                        {
                            continue;
                        }

                        return data;
                    }

                    break;
                // Request body extraction is opt-in
                case RequestSize.None:
                    _options.DiagnosticLogger?.Log(SentryLevel.Debug, "Skipping request body extraction.");
                    return null;
            }

            _options.DiagnosticLogger?.Log(SentryLevel.Warning,
                "Ignoring request with Size {0} and configuration RequestSize {1}", null, request.ContentLength, size);

            return null;
        }
    }
}
