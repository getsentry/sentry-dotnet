#if !NETSTANDARD2_0
using System;
using Microsoft.Extensions.Http;

namespace Sentry.AspNetCore
{
    // Injects Sentry's HTTP handler into HttpClientFactory
    internal class SentryHttpMessageHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
    {
        private readonly IHub _hub;

        public SentryHttpMessageHandlerBuilderFilter(IHub hub) => _hub = hub;

        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next) =>
            handlerBuilder =>
            {
                handlerBuilder.AdditionalHandlers.Add(new SentryHttpMessageHandler(_hub));
                next(handlerBuilder);
            };
    }
}
#endif
