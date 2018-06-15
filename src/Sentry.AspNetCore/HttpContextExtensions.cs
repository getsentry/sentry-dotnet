using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Protocol;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// HttpContext extensions
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class HttpContextExtensions
    {
        /// <summary>
        /// Applies the HTTP data to the Sentry scope
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="scope">The Sentry scope</param>
        /// <param name="options">Options</param>
        public static void SentryScopeApply(
            this HttpContext context,
            Scope scope,
            SentryAspNetCoreOptions options)
        {
            PopulateFromContext(context, scope, options);
            PopulateFromActivity(Activity.Current, scope);
        }

        private static void PopulateFromContext(
            HttpContext context,
            Scope scope,
            SentryAspNetCoreOptions options)
        {
            scope.SetTag(nameof(context.TraceIdentifier), context.TraceIdentifier);

            // TODO: should be elsewhere.
            if (options.IncludeRequestPayload && options.RequestPayloadExtractors != null)
            {
                foreach (var extractor in options.RequestPayloadExtractors)
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

        private static void PopulateFromActivity(Activity activity, Scope scope)
        {
            if (activity == null)
            {
                return;
            }

            //scope.ActivityId = activity.Id;

            // TODO: enumerating Activity.Tags clears the collection and sets field to null?
            scope.Tags.AddRange(activity.Tags);
        }
    }
}
