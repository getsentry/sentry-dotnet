using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.DataCollection;

namespace Sentry.AspNet.Internal;

internal class SystemWebRequestEventProcessor : ISentryEventProcessor, ISdkProcessor
{
    private readonly SentryOptions _options;
    internal IRequestPayloadExtractor PayloadExtractor { get; }

    public SystemWebRequestEventProcessor(IRequestPayloadExtractor payloadExtractor, SentryOptions options)
    {
        ArgumentNullException.ThrowIfNull(payloadExtractor);
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        PayloadExtractor = payloadExtractor;
    }

    public SentryEvent? Process(SentryEvent? @event)
    {
        var context = HttpContext.Current;
        if (context is null || @event is null)
        {
            return @event;
        }

        try
        {
            // During Application initialization we might have an event to send but no HTTP Request.
            // Request getter throws and doesn't seem there's a way to query for it.
            _ = context.Request;
        }
        catch (HttpException)
        {
            _options.LogDebug("HttpException not available to retrieve context.");
            return @event;
        }

        var dataCollection = _options.DataCollectionResolver;

        @event.Request.Method = context.Request.HttpMethod;
        @event.Request.Url = context.Request.Url.GetLeftPart(UriPartial.Path);

        try
        {
            @event.Request.QueryString = QueryStringFilter.Filter(
                GetRawQueryString(context.Request.Unvalidated.RawUrl), dataCollection.UrlQueryParams);
        }
        catch (NullReferenceException)
        {
            // Ignored since it can throw on WCF on the first event.
            // See #390
            _options.LogDebug("Ignored NRE thrown on System.Web.HttpContext.Request.QueryString");
        }

        var headerBehavior = dataCollection.RequestHeaders;
        var cookieBehavior = dataCollection.Cookies;
        var collectsCookies = cookieBehavior.Mode != KeyValueCollectionMode.Off;
        if (headerBehavior.Mode != KeyValueCollectionMode.Off)
        {
            foreach (var key in context.Request.Headers.AllKeys)
            {
                if (!collectsCookies && key is "Cookie")
                {
                    continue;
                }

                @event.Request.Headers[key] = SensitiveDataFilter.FilterHeaderValue(key, context.Request.Headers[key], headerBehavior);
            }
        }

        if (collectsCookies && context.Request.GetFilteredCookies(cookieBehavior) is { Length: > 0 } cookies)
        {
            @event.Request.Cookies = CookieFilter.ToEventCookies(cookies);
        }

        if (dataCollection.UserInfo)
        {
            if (@event.User.Username == Environment.UserName)
            {
                // The core SDK reports the logged-on OS user, which doesn't make sense in a server app
                @event.User.Username = null;
            }

            @event.User.IpAddress ??= context.Request.UserHostAddress;
            if (context.User?.Identity is { } identity)
            {
                @event.User.Username = identity.Name;
                @event.User.Other["IsAuthenticated"] = identity.IsAuthenticated.ToString();
            }
            if (context.User is ClaimsPrincipal claimsPrincipal)
            {
                if (claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) is { } claim)
                {
                    @event.User.Id = claim.Value;
                }
            }
        }

        @event.ServerName = Environment.MachineName;

        if (dataCollection.IncomingRequestBody &&
            PayloadExtractor.ExtractPayload(new SystemWebHttpRequest(context.Request)) is { } body)
        {
            @event.Request.Data = body;
        }

        return @event;
    }

    // HttpRequest.QueryString.ToString() re-encodes the parameters, so read them from the raw URL.
    private static string? GetRawQueryString(string rawUrl)
    {
        var queryStart = rawUrl.IndexOf('?');
        return queryStart < 0 ? null : rawUrl[(queryStart + 1)..];
    }
}
