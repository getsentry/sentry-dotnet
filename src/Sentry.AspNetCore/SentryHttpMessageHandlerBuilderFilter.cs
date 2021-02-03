#if !NETSTANDARD2_0
using System;
using Microsoft.Extensions.Http;

namespace Sentry.AspNetCore
{
    // Injects Sentry's HTTP handler into HttpClientFactory
    internal class SentryHttpMessageHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
    {
        private readonly Func<IHub> _getHub;

        public SentryHttpMessageHandlerBuilderFilter(Func<IHub> getHub) =>
            _getHub = getHub;

        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next) =>
            handlerBuilder =>
            {
                var hub = _getHub();
                handlerBuilder.AdditionalHandlers.Add(new SentryHttpMessageHandler(hub));
                next(handlerBuilder);
            };
    }
}
#endif
