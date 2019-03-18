#if SYSTEM_WEB
using System;
using System.Web;
using Sentry.Extensibility;

namespace Sentry.Internal.Web
{
    internal class SystemWebRequestEventProcessor : ISentryEventProcessor
    {
        private readonly RequestBodyExtractionDispatcher _dispatcher;

        public SystemWebRequestEventProcessor(SentryOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            _dispatcher = new RequestBodyExtractionDispatcher(new IRequestPayloadExtractor[] { }, options, options.MaxRequestBodySize);
        }

        public SentryEvent Process(SentryEvent @event)
        {
            if (HttpContext.Current?.Request is HttpRequest request)
            {
                @event.Request.Data = _dispatcher.Dispatch(new SystemWebHttpRequest(request));
            }
            return @event;
        }
    }
}
#endif
