using System.Collections.Generic;
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
            var options = context.RequestServices.GetService<SentryAspNetCoreOptions>();

            scope.SetTag(nameof(context.TraceIdentifier), context.TraceIdentifier);

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
                .ToDictionary(k => k.Key, v => v.Value);

            // TODO: Send users claim types?
            var identity = context.User?.Identity;
            if (identity != null)
            {
                var id = identity.Name;
                // TODO: Account for X-Forwarded-For.. Configurable?
                var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                if (id != null || ipAddress != null)
                {
                    // TODO: Just make user mutable? Like the HttpContext,
                    // it's just known not to be thread-safe
                    scope.User = new User(
                        id: id,
                        ipAddress: ipAddress);

                    // TOOD: Consider also:
                    //identity.AuthenticationType
                    //identity.IsAuthenticated
                    //scope.User.Id
                    //scope.User.Email
                }
            }

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
            scope.Tags.AddRange(activity.Tags);
        }
    }
}
