using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Sentry.Extensions.Protobuf;
using Sentry.Protocol;

namespace Sentry.AspNetCore.Grpc
{
    /// <summary>
    /// Scope Extensions
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ScopeExtensions
    {
        /// <summary>
        /// Populates the scope with the gRPC data
        /// </summary>
        public static void Populate<TRequest>(this Scope scope, ServerCallContext context, TRequest? request,
            SentryAspNetCoreGrpcOptions options) where TRequest : class
        {
            // Not to throw on code that ignores nullability warnings.
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            if (scope is null || context is null || options is null)
            {
                return;
            }
            // ReSharper restore ConditionIsAlwaysTrueOrFalse

            if (options.SendDefaultPii && !scope.HasUser())
            {
                var httpContext = context.GetHttpContext();
                var userFactory = httpContext.RequestServices?.GetService<IUserFactory>();
                var user = userFactory?.Create(httpContext);

                if (user != null)
                {
                    scope.User = user;
                }
            }

            scope.SetTag("grpc.method", context.Method);

            if (request is IMessage requestMessage)
            {
                SetBody(scope, context, requestMessage, options);
            }

            SetEnv(scope, context, options);
        }

        private static void SetEnv(Scope scope, ServerCallContext context, SentryAspNetCoreOptions options)
        {
            var httpContext = context.GetHttpContext();

            scope.Request.Method = httpContext.Request.Method;

            var host = context.Host;

            scope.Request.Url = $"{httpContext.Request.Scheme}://{host}{httpContext.Request.Path}";
            scope.UnsetTag("RequestPath");

            foreach (var requestHeader in context.RequestHeaders)
            {
                if (!options.SendDefaultPii
                    // Don't add headers which might contain PII
                    // Header field names are lowercase as per http/2 spec https://httpwg.org/specs/rfc7540.html#rfc.section.8.1.2
                    && (requestHeader.Key == HeaderNames.Cookie.ToLower()
                        || requestHeader.Key == HeaderNames.Authorization.ToLower()))
                {
                    continue;
                }

                scope.Request.Headers[requestHeader.Key] = requestHeader.Value;
            }

            // TODO: Hide these 'Env' behind some extension method as
            // these might be reported in a non CGI, old-school way
            if (options.SendDefaultPii
                && httpContext.Connection.RemoteIpAddress?.ToString() is { } ipAddress)
            {
                scope.Request.Env["REMOTE_ADDR"] = ipAddress;
            }

            scope.Request.Env["SERVER_NAME"] = Environment.MachineName;
            scope.Request.Env["SERVER_PORT"] = httpContext.Connection.LocalPort.ToString();

            if (httpContext.Response.Headers.TryGetValue("Server", out var server))
            {
                scope.Request.Env["SERVER_SOFTWARE"] = server;
            }
        }

        private static void SetBody<TRequest>(BaseScope scope, ServerCallContext context, TRequest request,
            SentryAspNetCoreOptions options) where TRequest : class, IMessage
        {
            var httpContext = context.GetHttpContext();
            var extractors = httpContext.RequestServices
                .GetService<IEnumerable<IProtobufRequestPayloadExtractor>>();

            if (extractors == null)
            {
                return;
            }

            var dispatcher =
                new ProtobufRequestExtractionDispatcher(extractors, options,
                    () => options.MaxRequestBodySize);

            var adapter = new GrpcRequestAdapter<TRequest>(request);

            var message = dispatcher.ExtractPayload(adapter);

            if (message != null)
            {
                // Convert message into JSON format for readability

                string jsonData = JsonFormatter.Default.Format(message);

                scope.Request.Data = jsonData;
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
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            if (scope is null || activity is null)
            {
                return;
            }
            // ReSharper restore ConditionIsAlwaysTrueOrFalse

            //scope.ActivityId = activity.Id;

            // TODO: enumerating Activity.Tags clears the collection and sets field to null?
            scope.SetTags(activity.Tags!);
        }

        internal static void SetWebRoot(this Scope scope, string webRoot)
        {
            scope.Request.Env["DOCUMENT_ROOT"] = webRoot;
        }
    }
}
