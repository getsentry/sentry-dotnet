using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Reflection;

namespace Sentry.AspNet.Internal;

internal class SystemWebRequestEventProcessor : ISentryEventProcessor
{
    private static readonly SdkVersion SdkVersion =
        typeof(SystemWebRequestEventProcessor).Assembly.GetNameAndVersion();

    private readonly SentryOptions _options;
    internal IRequestPayloadExtractor PayloadExtractor { get; }

    public SystemWebRequestEventProcessor(IRequestPayloadExtractor payloadExtractor, SentryOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        PayloadExtractor = payloadExtractor ?? throw new ArgumentNullException(nameof(payloadExtractor));
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

        @event.Request.Method = context.Request.HttpMethod;
        @event.Request.Url = context.Request.Url.AbsoluteUri;

        try
        {
            // ReSharper disable once ConstantConditionalAccessQualifier
            @event.Request.QueryString = context.Request.QueryString?.ToString();
        }
        catch (NullReferenceException)
        {
            // Ignored since it can throw on WCF on the first event.
            // See #390
            _options.LogDebug("Ignored NRE thrown on System.Web.HttpContext.Request.QueryString");
        }

        foreach (var key in context.Request.Headers.AllKeys)
        {
            // Don't add cookies that might contain PII
            if (!_options.SendDefaultPii && key is "Cookie")
            {
                continue;
            }

            @event.Request.Headers[key] = context.Request.Headers[key];
        }

        if (_options.SendDefaultPii)
        {
            if (@event.User.Username == Environment.UserName)
            {
                // if SendDefaultPii is true, Sentry SDK will send the current logged on user
                // which doesn't make sense in a server apps
                @event.User.Username = null;
            }

            @event.User.IpAddress ??= context.Request.UserHostAddress;
            if (context.User.Identity is { } identity)
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

        var body = PayloadExtractor.ExtractPayload(new SystemWebHttpRequest(context.Request));
        if (body != null)
        {
            @event.Request.Data = body;
        }

        // Always set the SDK info
        @event.Sdk.Name = "sentry.dotnet.aspnet";
        @event.Sdk.Version = SdkVersion.Version;
        @event.Sdk.AddPackage($"nuget:{SdkVersion.Name}", SdkVersion.Version);
        return @event;
    }
}
