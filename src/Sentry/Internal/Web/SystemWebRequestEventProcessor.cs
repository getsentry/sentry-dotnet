#if SYSTEM_WEB
using System;
using System.Web;
using Sentry.Extensibility;

namespace Sentry.Internal.Web
{
    internal class SystemWebRequestEventProcessor : ISentryEventProcessor
    {
        internal IRequestPayloadExtractor PayloadExtractor { get; }

        public SystemWebRequestEventProcessor(IRequestPayloadExtractor payloadExtractor)
            => PayloadExtractor = payloadExtractor ?? throw new ArgumentNullException(nameof(payloadExtractor));

        public SentryEvent Process(SentryEvent @event)
        {
            if (@event != null && HttpContext.Current?.Request is HttpRequest request)
            {
                var body = PayloadExtractor.ExtractPayload(new SystemWebHttpRequest(request));
                if (body != null)
                {
                    @event.Request.Data = body;
                }
            }
            return @event;
        }
    }
}
#endif
