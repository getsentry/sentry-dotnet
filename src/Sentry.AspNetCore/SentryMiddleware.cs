using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.AspNetCore.Diagnostics;
#if NETSTANDARD2_0
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
#endif
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensibility;
using Sentry.Protocol;
using Sentry.Reflection;
using Transaction = Sentry.Protocol.Transaction;

namespace Sentry.AspNetCore
{
    /// <summary>
    /// Sentry middleware for ASP.NET Core
    /// </summary>
    internal class SentryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Func<IHub> _hubAccessor;
        private readonly SentryAspNetCoreOptions _options;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ILogger<SentryMiddleware> _logger;

        internal static readonly SdkVersion NameAndVersion
            = typeof(SentryMiddleware).Assembly.GetNameAndVersion();

        private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="SentryMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next.</param>
        /// <param name="hubAccessor">The sentry Hub accessor.</param>
        /// <param name="options">The options for this integration</param>
        /// <param name="hostingEnvironment">The hosting environment.</param>
        /// <param name="logger">Sentry logger.</param>
        /// <exception cref="ArgumentNullException">
        /// next
        /// or
        /// sentry
        /// </exception>
        public SentryMiddleware(
            RequestDelegate next,
            Func<IHub> hubAccessor,
            IOptions<SentryAspNetCoreOptions> options,
            IHostingEnvironment hostingEnvironment,
            ILogger<SentryMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
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
        /// Handles the <see cref="HttpContext"/> while capturing any errors
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var hub = _hubAccessor();
            if (!hub.IsEnabled)
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            using (hub.PushAndLockScope())
            {
                if (_options.MaxRequestBodySize != RequestSize.None)
                {
                    context.Request.EnableBuffering();
                }

                if (_options.FlushOnCompletedRequest)
                {
                    context.Response.OnCompleted(async () =>
                    {
                        // Serverless environments flush the queue at the end of each request
                        await hub.FlushAsync(timeout: _options.FlushTimeout).ConfigureAwait(false);
                    });
                }

                var routeData = context.GetRouteData();
                var controller = routeData.Values["controller"]?.ToString();
                var action = routeData.Values["action"]?.ToString();
                var area = routeData.Values["area"]?.ToString();

                // TODO: What if it's not using controllers (i.e. endpoints)?

                var transactionName = area == null
                    ? $"{controller}.{action}"
                    : $"{area}.{controller}.{action}";

                var transaction = hub.CreateTransaction(transactionName, "http.server");

                hub.ConfigureScope(scope =>
                {
                    // At the point lots of stuff from the request are not yet filled
                    // Identity for example is added later on in the pipeline
                    // Subscribing to the event so that HTTP data is only read in case an event is going to be
                    // sent to Sentry. This avoid the cost on error-free requests.
                    // In case of event, all data made available through the HTTP Context at the time of the
                    // event creation will be sent to Sentry

                    scope.OnEvaluating += (_, __) => PopulateScope(context, scope);
                });

                try
                {
                    transaction.StartTimestamp = DateTimeOffset.Now;
                    await _next(context).ConfigureAwait(false);

                    // When an exception was handled by other component (i.e: UseExceptionHandler feature).
                    var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (exceptionFeature?.Error != null)
                    {
                        CaptureException(exceptionFeature.Error);
                    }
                }
                catch (Exception e)
                {
                    CaptureException(e);

                    ExceptionDispatchInfo.Capture(e).Throw();
                }
                finally
                {
                    transaction.Finish(
                        GetSpanStatusFromCode(context.Response.StatusCode)
                    );
                }

                void CaptureException(Exception e)
                {
                    var evt = new SentryEvent(e);

                    _logger.LogTrace("Sending event '{SentryEvent}' to Sentry.", evt);

                    var id = hub.CaptureEvent(evt);

                    _logger.LogInformation("Event '{id}' queued.", id);
                }
            }
        }

        internal void PopulateScope(HttpContext context, Scope scope)
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

            scope.Populate(context, _options);

            if (_options.IncludeActivityData && Activity.Current is not null)
            {
                scope.Populate(Activity.Current);
            }
        }

        private static SpanStatus GetSpanStatusFromCode(int statusCode) => statusCode switch
        {
            < 400 => SpanStatus.Ok,
            400 => SpanStatus.InvalidArgument,
            401 => SpanStatus.Unauthenticated,
            403 => SpanStatus.PermissionDenied,
            404 => SpanStatus.NotFound,
            409 => SpanStatus.AlreadyExists,
            429 => SpanStatus.ResourceExhausted,
            499 => SpanStatus.Cancelled,
            < 500 => SpanStatus.InvalidArgument,
            500 => SpanStatus.InternalError,
            501 => SpanStatus.Unimplemented,
            503 => SpanStatus.Unavailable,
            504 => SpanStatus.DeadlineExceeded,
            < 600 => SpanStatus.InternalError,
            _ => SpanStatus.UnknownError
        };
    }
}
