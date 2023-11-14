using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Sentry.AspNetCore.Extensions;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.AspNetCore;

/// <summary>
/// Scope Extensions
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ScopeExtensions
{
    /// <summary>
    /// Populates the scope with the HTTP data
    /// </summary>
    /// <remarks>
    /// NOTE: The scope is applied to the event BEFORE running the event processors/exception processors.
    /// The main Sentry SDK has processors which run right before any additional processors to the Event
    /// </remarks>
    public static void Populate(this Scope scope, HttpContext context, SentryAspNetCoreOptions options)
    {
        // Not to throw on code that ignores nullability warnings.
        if (scope.IsNull() || context.IsNull() || options.IsNull())
        {
            return;
        }

        // With the logger integration, a BeginScope call is made with RequestId. That ends up adding
        // two tags with the same value: RequestId and TraceIdentifier
        if (!scope.Tags.TryGetValue("RequestId", out var requestId) || requestId != context.TraceIdentifier)
        {
            scope.SetTag(nameof(context.TraceIdentifier), context.TraceIdentifier);
        }

        if (options.SendDefaultPii && !scope.HasUser())
        {
#pragma warning disable CS0618
            var userFactory = context.RequestServices.GetService<IUserFactory>();
            var user = userFactory?.Create(context);
#pragma warning restore CS0618

            if (user != null)
            {
                scope.User = user;
            }
        }

        try
        {
            SetBody(scope, context, options);
        }
#if NET5_0_OR_GREATER
        catch (BadHttpRequestException) when (context.RequestAborted.IsCancellationRequested)
#else
        catch (IOException e) when (context.RequestAborted.IsCancellationRequested &&
                                    e.GetType().Name == "BadHttpRequestException")
#endif
        {
            options.LogDebug("Failed to extract body because the request was aborted.");
        }
        catch (Exception e)
        {
            options.LogError(e, "Failed to extract body.");
        }

        SetEnv(scope, context, options);

        // Extract the route data
        try
        {
            var routeData = context.GetRouteData();
            var values = routeData.Values;

            if (values["controller"] is string controller)
            {
                scope.SetTag("route.controller", controller);
            }

            if (values["action"] is string action)
            {
                scope.SetTag("route.action", action);
            }

            if (values["area"] is string area)
            {
                scope.SetTag("route.area", area);
            }

            if (values["version"] is string version)
            {
                scope.SetTag("route.version", version);
            }

            // Transaction Name may only be available afterward the creation of the Transaction.
            // In this case, the event will update the transaction name if captured during the
            // pipeline execution, allowing it to match the correct transaction name as the current
            // active transaction.
            if (string.IsNullOrEmpty(scope.TransactionName))
            {
                scope.TransactionName = context.TryGetTransactionName();
            }
        }
        catch (Exception e)
        {
            // Suppress the error here; we expect an ArgumentNullException if httpContext.Request.RouteValues is null from GetRouteData()
            // TODO: Consider adding a bool to the Sentry options to make route data extraction optional in case they don't use a routing middleware?
            options.LogDebug("Failed to extract route data.", e);
        }

        // TODO: Get context stuff into scope
        //context.Session
        //context.Response
        //context.Items
    }

    private static void SetEnv(Scope scope, HttpContext context, SentryAspNetCoreOptions options)
    {
        scope.Request.Method = context.Request.Method;

        // Logging integration, if enabled, sets the following tag which ends up as duplicate
        // to Request.Url. Prefer the interface value and remove tag.
        var host = context.Request.Host.Host;
        if (context.Request.Host.Port != null)
        {
            host += $":{context.Request.Host.Port}";
        }
        scope.Request.Url = $"{context.Request.Scheme}://{host}{context.Request.Path}";
        scope.UnsetTag("RequestPath");

        scope.Request.QueryString = context.Request.QueryString.ToString();
        foreach (var requestHeader in context.Request.Headers)
        {
            if (!options.SendDefaultPii
                // Don't add headers which might contain PII
                && (requestHeader.Key == HeaderNames.Cookie
                    || requestHeader.Key == HeaderNames.Authorization))
            {
                continue;
            }

            scope.Request.Headers[requestHeader.Key] = requestHeader.Value!;

            if (requestHeader.Key == HeaderNames.Cookie)
            {
                scope.Request.Cookies = requestHeader.Value;
            }
        }

        // TODO: Hide these 'Env' behind some extension method as
        // these might be reported in a non CGI, old-school way
        if (options.SendDefaultPii
            && context.Connection.RemoteIpAddress?.ToString() is { } ipAddress)
        {
            scope.Request.Env["REMOTE_ADDR"] = ipAddress;
        }

        scope.Request.Env["SERVER_NAME"] = Environment.MachineName;
        scope.Request.Env["SERVER_PORT"] = context.Connection.LocalPort.ToString();

        if (context.Response.Headers.TryGetValue("Server", out var server))
        {
            scope.Request.Env["SERVER_SOFTWARE"] = server!;
        }
    }

    private static void SetBody(Scope scope, HttpContext context, SentryAspNetCoreOptions options)
    {
        // Resharper says this can't be null, but actually it can if SetBody is called multiple times in a single request
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        var extractors = context.RequestServices?.GetService<IEnumerable<IRequestPayloadExtractor>>();
        if (extractors == null)
        {
            return;
        }
        var dispatcher = new RequestBodyExtractionDispatcher(extractors, options, () => options.MaxRequestBodySize);

        var body = dispatcher.ExtractPayload(new HttpRequestAdapter(context.Request));
        if (body != null)
        {
            scope.Request.Data = body;
        }
    }

    /// <summary>
    /// Populates the scope with the System.Diagnostics.Activity
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <param name="activity">The activity.</param>
    public static void Populate(this Scope scope, Activity activity)
    {
        // Not to throw on code that ignores nullability warnings.
        if (scope.IsNull() || activity.IsNull())
        {
            return;
        }

        //scope.ActivityId = activity.Id;

        // TODO: enumerating Activity.Tags clears the collection and sets field to null?
        scope.SetTags(activity.Tags
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(k => new KeyValuePair<string, string>(k.Key, k.Value!)));
    }

    internal static void SetWebRoot(this Scope scope, string webRoot)
    {
        scope.Request.Env["DOCUMENT_ROOT"] = webRoot;
    }
}
