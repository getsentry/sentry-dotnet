using System.Collections.Generic;
using System.ComponentModel;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

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
            SentryAspNetCoreOptions options) where TRequest : class
        {
            // Not to throw on code that ignores nullability warnings.
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            if (scope is null || context is null || options is null)
            {
                return;
            }
            // ReSharper restore ConditionIsAlwaysTrueOrFalse

            scope.SetTag("grpc.method", context.Method);

            if (request is IMessage requestMessage)
            {
                SetBody(scope, context, requestMessage, options);
            }
        }

        private static void SetBody<TRequest>(Scope scope, ServerCallContext context, TRequest request,
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
    }
}
