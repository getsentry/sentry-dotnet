using Microsoft.Extensions.Http;

namespace Sentry.Extensions.Logging;

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
#pragma warning disable CS0618
            var enableHandler = hub.GetSentryOptions()?.DisableSentryHttpMessageHandler == false;
#pragma warning restore CS0618
            if (enableHandler && !handlerBuilder.AdditionalHandlers.Any(h => h is SentryHttpMessageHandler))
            {
                handlerBuilder.AdditionalHandlers.Add(
                    new SentryHttpMessageHandler(hub)
                );
            }

            next(handlerBuilder);
        };
}
