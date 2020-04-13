using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.AspNetCore
{
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
            // With the logger integration, a BeginScope call is made with RequestId. That ends up adding
            // two tags with the same value: RequestId and TraceIdentifier
            if (!scope.Tags.TryGetValue("RequestId", out var requestId) || requestId != context.TraceIdentifier)
            {
                scope.SetTag(nameof(context.TraceIdentifier), context.TraceIdentifier);
            }

            if (options?.SendDefaultPii == true && !scope.HasUser())
            {
                var userFactory = context.RequestServices?.GetService<IUserFactory>();
                if (userFactory != null)
                {
                    scope.User = userFactory.Create(context);
                }
            }

            try
            {
                SetBody(scope, context, options);
            }
            catch (Exception e)
            {
                options?.DiagnosticLogger?.LogError("Failed to extract body.", e);
            }
            SetEnv(scope, context, options);

            // TODO: From MVC route template, ideally
            // TODO: optionally get transaction from request through a dependency
            //scope.Transation = context.Request.Path;

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
                if (options?.SendDefaultPii != true
                // Don't add headers which might contain PII
                && (requestHeader.Key == HeaderNames.Cookie
                    || requestHeader.Key == HeaderNames.Authorization))
                {
                    continue;
                }

                scope.Request.Headers[requestHeader.Key] = requestHeader.Value;
            }

            // TODO: Hide these 'Env' behind some extension method as
            // these might be reported in a non CGI, old-school way
            if (options?.SendDefaultPii == true
                && context.Connection.RemoteIpAddress?.ToString() is string ipAddress)
            {
                scope.Request.Env["REMOTE_ADDR"] = ipAddress;
            }

            scope.Request.Env["SERVER_NAME"] = Environment.MachineName;
            scope.Request.Env["SERVER_PORT"] = context.Connection.LocalPort.ToString();

            if (context.Response.Headers.TryGetValue("Server", out var server))
            {
                scope.Request.Env["SERVER_SOFTWARE"] = server;
            }
        }

        private static void SetBody(BaseScope scope, HttpContext context, SentryAspNetCoreOptions options)
        {
            if (context == null || scope == null || options == null)
            {
                return;
            }

            var extractors = context.RequestServices.GetService<IEnumerable<IRequestPayloadExtractor>>();
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
            if (scope == null || activity == null)
            {
                return;
            }

            //scope.ActivityId = activity.Id;

            // TODO: enumerating Activity.Tags clears the collection and sets field to null?
            scope.SetTags(activity.Tags);
        }

        internal static void SetWebRoot(this Scope scope, string webRoot)
        {
            if (webRoot != null)
            {
                scope.Request.Env["DOCUMENT_ROOT"] = webRoot;
            }
        }
    }
}
