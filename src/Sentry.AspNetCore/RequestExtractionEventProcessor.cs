
//using System;
//using System.Web;
//using Sentry.Extensibility;

//namespace Sentry.AspNetCore
//{
//    internal class RequestExtractionEventProcessor : ISentryEventProcessor
//    {
//        private readonly RequestBodyExtractionDispatcher _dispatcher;

//        public RequestExtractionEventProcessor(SentryOptions options)
//        {
//            if (options == null) throw new ArgumentNullException(nameof(options));
//            _dispatcher = new RequestBodyExtractionDispatcher(new IRequestPayloadExtractor[] { }, options);
//        }

//        public SentryEvent Process(SentryEvent @event)
//        {
//            //if (HttpContext.Current != null)
//            {
//                //_dispatcher.Dispatch(@event, new SystemWebHttpContext(HttpContext.Current));
//            }
//            return @event;
//        }
//    }
//}
