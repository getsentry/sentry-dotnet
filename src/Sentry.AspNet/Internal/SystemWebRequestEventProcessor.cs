using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Web;
using Sentry.Extensibility;
using Sentry.Protocol;
using Sentry.Reflection;

namespace Sentry.AspNet.Internal
{
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
                _options.DiagnosticLogger?.LogDebug("HttpException not available to retrieve context.");
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
                _options.DiagnosticLogger?.LogDebug("Ignored NRE thrown on System.Web.HttpContext.Request.QueryString");
            }

            foreach (var key in context.Request.Headers.AllKeys)
            {
                if (!_options.SendDefaultPii
                    // Don't add headers which might contain PII
                    && (key == "Cookie"
                        || key == "Authorization"))
                {
                    continue;
                }
                @event.Request.Headers[key] = context.Request.Headers[key];
            }

            if (_options?.SendDefaultPii == true)
            {
                if (@event.User.Username == Environment.UserName)
                {
                    // if SendDefaultPii is true, Sentry SDK will send the current logged on user
                    // which doesn't make sense in a server apps
                    @event.User.Username = null;
                }

                @event.User.IpAddress = context.Request.UserHostAddress;
                if (context.User.Identity is { } identity)
                {
                    @event.User.Username = identity.Name;
                    @event.User.Other.Add("IsAuthenticated", identity.IsAuthenticated.ToString());
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

            // Move 'runtime' under key 'server-runtime' as User-Agent parsing done at
            // Sentry will represent the client's
            if (@event.Contexts.TryRemove(Runtime.Type, out var runtime))
            {
                @event.Contexts["server-runtime"] = runtime;
            }

            if (@event.Contexts.TryRemove(Protocol.OperatingSystem.Type, out var os))
            {
                @event.Contexts["server-os"] = os;
            }

            var body = PayloadExtractor.ExtractPayload(new SystemWebHttpRequest(context.Request));
            if (body != null)
            {
                @event.Request.Data = body;
            }

            if (@event.Sdk.Version is null && @event.Sdk.Name is null)
            {
                @event.Sdk.Name = "sentry.dotnet.aspnet";
                @event.Sdk.Version = SdkVersion.Version;
            }

            if (SdkVersion.Version != null)
            {
                @event.Sdk.AddPackage(
                    $"nuget:{SdkVersion.Name}",
                    SdkVersion.Version
                );
            }

            return @event;
        }
    }
}
