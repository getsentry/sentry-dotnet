using System;
using System.ComponentModel;
using System.Diagnostics;
using Sentry;
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
        public static void SentryScopeApply(this HttpContext context, Scope scope)
        {
            PopulateFromContext(context, scope);
            PopulateFromActivity(Activity.Current, scope);
        }

        private static void PopulateFromContext(HttpContext context, Scope scope)
        {
            scope.SetTag(nameof(context.TraceIdentifier), context.TraceIdentifier);
            if (context.Request.Headers.TryGetValue("User-Agent", out var userAgent))
            {
                scope.Contexts.Browser.Name = userAgent;
            }

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
