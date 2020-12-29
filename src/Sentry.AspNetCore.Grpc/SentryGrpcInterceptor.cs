using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;
using Sentry.Extensibility;

namespace Sentry.AspNetCore.Grpc
{
    /// <summary>
    /// Sentry interceptor for ASP.NET Core gRPC
    /// </summary>
    public class SentryGrpcInterceptor : Interceptor
    {
        private readonly Func<IHub> _hubAccessor;
        private readonly SentryAspNetCoreOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SentryGrpcInterceptor"/> class.
        /// </summary>
        /// <param name="hubAccessor">The sentry Hub accessor.</param>
        /// <param name="options">The options for this integration</param>
        /// <exception cref="ArgumentNullException">
        /// continuation
        /// or
        /// sentry
        /// </exception>
        public SentryGrpcInterceptor(
            Func<IHub> hubAccessor,
            IOptions<SentryAspNetCoreOptions> options)
        {
            _hubAccessor = hubAccessor ?? throw new ArgumentNullException(nameof(hubAccessor));
            _options = options.Value;
            var hub = _hubAccessor();
            foreach (var callback in _options.ConfigureScopeCallbacks)
            {
                hub.ConfigureScope(callback);
            }
        }

        /// <summary>
        /// Handles the <see cref="Interceptor.UnaryServerHandler{TRequest,TResponse}"/> while capturing any errors
        /// </summary>
        /// <param name="request">The request</param>
        /// <param name="context">The server call context</param>
        /// <param name="continuation">The continuation</param>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <returns></returns>
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            var hub = _hubAccessor();
            if (!hub.IsEnabled)
            {
                return await continuation(request, context).ConfigureAwait(false);
            }

            using (hub.PushAndLockScope())
            {
                hub.ConfigureScope(scope =>
                {
                    scope.OnEvaluating += (_, _) => scope.Populate(context, request, _options);
                });

                try
                {
                    return await continuation(request, context).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    CaptureException(hub, e);

                    ExceptionDispatchInfo.Capture(e).Throw();
                }
            }

            return null;
        }

        /// <summary>
        /// Handles the <see cref="Interceptor.ServerStreamingServerHandler{TRequest,TResponse}"/> while capturing any errors
        /// </summary>
        /// <param name="request">The request</param>
        /// <param name="responseStream">The response stream</param>
        /// <param name="context">The server call context</param>
        /// <param name="continuation">The continuation</param>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <returns></returns>
        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var hub = _hubAccessor();
            if (!hub.IsEnabled)
            {
                await continuation(request, responseStream, context).ConfigureAwait(false);

                return;
            }

            using (hub.PushAndLockScope())
            {
                hub.ConfigureScope(scope =>
                {
                    scope.OnEvaluating += (_, _) => scope.Populate(context, request, _options);
                });

                try
                {
                    await continuation(request, responseStream, context).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    CaptureException(hub, e);

                    ExceptionDispatchInfo.Capture(e).Throw();
                }
            }
        }

        /// <summary>
        /// Handles the <see cref="ClientStreamingServerHandler{TRequest,TResponse}"/> while capturing any errors
        /// </summary>
        /// <param name="requestStream">The request stream</param>
        /// <param name="context">The server call context</param>
        /// <param name="continuation">The continuation</param>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream, ServerCallContext context,
            ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var hub = _hubAccessor();
            if (!hub.IsEnabled)
            {
                return await continuation(requestStream, context).ConfigureAwait(false);
            }

            using (hub.PushAndLockScope())
            {
                hub.ConfigureScope(scope =>
                {
                    scope.OnEvaluating += (_, _) => scope.Populate<TRequest>(context, null, _options);
                });

                try
                {
                    return await continuation(requestStream, context).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    CaptureException(hub, e);

                    ExceptionDispatchInfo.Capture(e).Throw();
                }
            }

            return null;
        }

        /// <summary>
        /// Handles the <see cref="Interceptor.ServerStreamingServerHandler{TRequest,TResponse}"/> while capturing any errors
        /// </summary>
        /// <param name="requestStream">The request stream</param>
        /// <param name="responseStream">The response stream</param>
        /// <param name="context">The server call context</param>
        /// <param name="continuation">The continuation</param>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <returns></returns>
        public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var hub = _hubAccessor();
            if (!hub.IsEnabled)
            {
                await continuation(requestStream, responseStream, context).ConfigureAwait(false);

                return;
            }

            using (hub.PushAndLockScope())
            {
                hub.ConfigureScope(scope =>
                {
                    scope.OnEvaluating += (_, _) => scope.Populate<TRequest>(context, null, _options);
                });

                try
                {
                    await continuation(requestStream, responseStream, context).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    CaptureException(hub, e);

                    ExceptionDispatchInfo.Capture(e).Throw();
                }
            }
        }

        private void CaptureException(IHub hub, Exception e)
        {
            var evt = new SentryEvent(e);

            _options.DiagnosticLogger?.LogDebug("Sending event '{SentryEvent}' to Sentry.", evt);

            var id = hub.CaptureEvent(evt);

            _options.DiagnosticLogger?.LogInfo("Event '{id}' queued.", id);
        }
    }
}
