using System;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Sentry.Protocol;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore
{
    /// <summary>
    /// HttpContext extensions
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HttpContextExtensions
    {
        private const string SentryHttpContextKey = "SentryHttpContextKey";

        /// <summary>
        /// Configures the scope.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="configureScope">The configure scope.</param>
        public static void ConfigureScope(this HttpContext context, Action<Scope> configureScope)
        {
            var scope = GetOrCreate(context);
            configureScope(scope);
        }

        /// <summary>
        /// Gets a Sentry scope for the current <see cref="HttpContext"/>
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="Scope"/></returns>
        public static Scope ToSentryScope(this HttpContext context)
        {
            var scope = GetOrCreate(context);

            PopulateFromContext(context, scope);
            PopulateFromActivity(Activity.Current, scope);

            return scope;
        }

        private static Scope GetOrCreate(HttpContext context)
        {
            Scope scope;
            if (context.Items.TryGetValue(SentryHttpContextKey, out var scopeObject))
            {
                scope = scopeObject as Scope;
                Debug.Assert(scope != null);
            }
            else
            {
                scope = new Scope(null);
                context.Items.Add(SentryHttpContextKey, scope);
            }

            return scope;
        }

        private static void PopulateFromContext(HttpContext context, Scope scope)
        {
            scope.SetTag("TraceIdentifier", context.TraceIdentifier);

            if (context.Request.Headers.TryGetValue("User-Agent", out var userAgent))
            {
                scope.Contexts.Browser.Name = userAgent;
            }

            // TODO: Send users claim types?
            var identity = context.User?.Identity;
            if (identity != null)
            {
                //scope.SetUser(identity.Name;)

                // TODO: Account for X-Forwarded-For.. Configurable?
                //scope.User.IpAddress = context.Connection.RemoteIpAddress.ToString();

                // TOOD: Consider also:
                //identity.AuthenticationType
                //identity.IsAuthenticated
                //scope.User.Id
                //scope.User.Email
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
