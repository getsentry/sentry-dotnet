using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Sentry.Protocol;

namespace Sentry.AspNetCore
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ScopeExtensions
    {
        /// <summary>
        /// Populates the scope with the HTTP data
        /// </summary>
        public static void Populate(this Scope scope, HttpContext context)
        {
            scope.SetTag(nameof(context.TraceIdentifier), context.TraceIdentifier);

            var options = context.RequestServices?.GetService<SentryAspNetCoreOptions>();
            // TODO: should be elsewhere.
            if (options?.IncludeRequestPayload == true)
            {
                var extractors = context.RequestServices.GetServices<IRequestPayloadExtractor>();
                foreach (var extractor in extractors)
                {
                    var data = extractor.ExtractPayload(context.Request);
                    if (!string.IsNullOrWhiteSpace(data as string) || data != null)
                    {
                        scope.Request.Data = data;
                        break;
                    }
                }
            }

            scope.Request.Method = context.Request.Method;
            scope.Request.Url = context.Request.Path;
            scope.Request.QueryString = context.Request.QueryString.ToString();
            scope.Request.Headers = context.Request.Headers
                .Select(p => new KeyValuePair<string, string>(p.Key, string.Join(", ", p.Value)))
                .ToImmutableDictionary(k => k.Key, v => v.Value);

            // TODO: Hide these 'Env' behind some extension method as
            // these might be reported in a non CGI, old-school way
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            if (ipAddress != null)
            {
                scope.Request.Env = scope.Request.Env.SetItem("REMOTE_ADDR", ipAddress);
            }

            scope.Request.Env = scope.Request.Env.SetItem("SERVER_NAME", Environment.MachineName);
            scope.Request.Env = scope.Request.Env.SetItem("SERVER_PORT", context.Connection.LocalPort.ToString());

            // TODO: likely a better way to do this as if the response didn't start yet nothing is found
            if (context.Response.Headers.TryGetValue("Server", out var server))
            {
                scope.Request.Env = scope.Request.Env.SetItem("SERVER_SOFTWARE", server);
            }

            // Don't send the user if all we have of him/her is the IP address
            // TODO: Send users claim types?
            var identity = context.User?.Identity;
            var name = identity?.Name;

            // TODO: Account for X-Forwarded-For.. Configurable?
            if (name != null)
            {
                // TODO: Just make user mutable? Like the HttpContext,
                // it's just known not to be thread-safe
                scope.User = new User(
                    //username:
                    //email:
                    id: name,
                    ipAddress: ipAddress);

                // TOOD: Consider also:
                //identity.AuthenticationType
                //identity.IsAuthenticated
                //scope.User.Id
                //scope.User.Email
            }

            // TODO: From MVC route template, ideally
            //scope.Transation = context.Request.Path;

            // TODO: Get context stuff into scope
            //context.User
            //context.Connection
            //context.Session
            //context.Response
            //context.Items
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

        public static void SetWebRoot(this Scope scope, string webRoot)
        {
            scope.Request.Env = scope.Request.Env.SetItem("DOCUMENT_ROOT", webRoot);
        }
    }
}
