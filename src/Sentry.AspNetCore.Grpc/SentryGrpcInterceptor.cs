using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Protocol;
using Sentry.Reflection;

namespace Sentry.AspNetCore.Grpc
{
    /// <summary>
    /// Sentry interceptor for ASP.NET Core gRPC
    /// </summary>
    public class SentryGrpcInterceptor : Interceptor
    {
        private readonly Func<IHub> _hubAccessor;
        private readonly SentryAspNetCoreGrpcOptions _options;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger<SentryGrpcInterceptor> _logger;

        internal static readonly SdkVersion NameAndVersion
            = typeof(SentryGrpcInterceptor).Assembly.GetNameAndVersion();

        private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="SentryGrpcInterceptor"/> class.
        /// </summary>
        /// <param name="hubAccessor">The sentry Hub accessor.</param>
        /// <param name="options">The options for this integration</param>
        /// <param name="hostingEnvironment">The hosting environment.</param>
        /// <param name="logger">Sentry logger.</param>
        /// <exception cref="ArgumentNullException">
        /// continuation
        /// or
        /// sentry
        /// </exception>
        public SentryGrpcInterceptor(
            Func<IHub> hubAccessor,
            IOptions<SentryAspNetCoreGrpcOptions> options,
            IWebHostEnvironment hostingEnvironment,
            ILogger<SentryGrpcInterceptor> logger)
        {
            _hubAccessor = hubAccessor ?? throw new ArgumentNullException(nameof(hubAccessor));
            _options = options.Value;
            var hub = _hubAccessor();
            foreach (var callback in _options.ConfigureScopeCallbacks)
            {
                hub.ConfigureScope(callback);
            }

            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
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
                    PopulateScope(context, request, scope);
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
            }

            using (hub.PushAndLockScope())
            {
                hub.ConfigureScope(scope =>
                {
                    PopulateScope(context, request, scope);
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
                await continuation(requestStream, context).ConfigureAwait(false);
            }

            using (hub.PushAndLockScope())
            {
                hub.ConfigureScope(scope =>
                {
                    PopulateScope<TRequest>(context, null, scope);
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
            }

            using (hub.PushAndLockScope())
            {
                hub.ConfigureScope(scope =>
                {
                    PopulateScope<TRequest>(context, null, scope);
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

            _logger.LogTrace("Sending event '{SentryEvent}' to Sentry.", evt);

            var id = hub.CaptureEvent(evt);

            _logger.LogInformation("Event '{id}' queued.", id);
        }

        internal void PopulateScope<TRequest>(ServerCallContext context, TRequest? request, Scope scope)
            where TRequest : class
        {
            if (scope.Sdk is { })
            {
                scope.Sdk.Name = Constants.SdkName;
                scope.Sdk.Version = NameAndVersion.Version;

                if (NameAndVersion.Version is { } version)
                {
                    scope.Sdk.AddPackage(ProtocolPackageName, version);
                }
            }

            if (_hostingEnvironment.WebRootPath is { } webRootPath)
            {
                scope.SetWebRoot(webRootPath);
            }

            scope.Populate(context, request, _options);

            if (_options.IncludeActivityData)
            {
                scope.Populate(Activity.Current!);
            }
        }
    }
}
