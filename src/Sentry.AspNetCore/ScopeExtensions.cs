using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Sentry.Protocol;

namespace Sentry.AspNetCore
{
    ///
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ScopeExtensions
    {
        /// <summary>
        /// Populates the scope with the HTTP data
        /// </summary>
        public static void Populate(this Scope scope, HttpContext context)
        {
            // With the logger integration, a BeginScope call is made with RequestId. That ends up adding
            // two tags with the same value: RequestId and TraceIdentifier
            if (!scope.Tags.TryGetValue("RequestId", out var requestId) || requestId != context.TraceIdentifier)
            {
                scope.SetTag(nameof(context.TraceIdentifier), context.TraceIdentifier);
            }

            var userFactory = context.RequestServices?.GetService<IUserFactory>();
            if (userFactory != null)
            {
                scope.User = userFactory.Create(context);
            }

            SetBody(scope, context);
            SetEnv(scope, context);

            // TODO: From MVC route template, ideally
            //scope.Transation = context.Request.Path;

            // TODO: Get context stuff into scope
            //context.Session
            //context.Response
            //context.Items
        }

        private static void SetEnv(Scope scope, HttpContext context)
        {
            scope.Request.Method = context.Request.Method;

            // Logging integration, if enabled, sets the following tag which ends up as duplicate
            // to Request.Url. Prefer the interface value and remove tag.
            scope.Request.Url = context.Request.Path;
            scope.UnsetTag("RequestPath");

            scope.Request.QueryString = context.Request.QueryString.ToString();
            scope.Request.Headers = context.Request.Headers
                .ToImmutableDictionary(k => k.Key, v => v.Value.ToString());

            // TODO: Hide these 'Env' behind some extension method as
            // these might be reported in a non CGI, old-school way
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            if (ipAddress != null)
            {
                scope.Request.Env = scope.Request.Env.SetItem("REMOTE_ADDR", ipAddress);
            }

            scope.Request.Env = scope.Request.Env.SetItem("SERVER_NAME", Environment.MachineName);
            scope.Request.Env = scope.Request.Env.SetItem("SERVER_PORT", context.Connection.LocalPort.ToString());

            if (context.Response.Headers.TryGetValue("Server", out var server))
            {
                scope.Request.Env = scope.Request.Env.SetItem("SERVER_SOFTWARE", server);
            }
        }

        private static void SetBody(Scope scope, HttpContext context)
        {
            var options = context.RequestServices?.GetService<SentryAspNetCoreOptions>();
            if (options?.IncludeRequestPayload == true)
            {
                // GetServices<IRequestPayloadExtractor> throws! Shouldn't it return Enumerable.Empty<T>?
                var extractors = context.RequestServices.GetService<IEnumerable<IRequestPayloadExtractor>>();
                if (extractors == null)
                {
                    return;
                }

                foreach (var extractor in extractors)
                {
                    var data = extractor.ExtractPayload(context.Request);

                    if (data == null
                        || data is string dataString
                        && string.IsNullOrEmpty(dataString))
                    {
                        continue;
                    }

                    scope.Request.Data = data;
                    break;
                }
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
                scope.Request.Env = scope.Request.Env.SetItem("DOCUMENT_ROOT", webRoot);
            }
        }
    }
}
