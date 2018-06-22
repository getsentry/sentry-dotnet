using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sentry.Protocol;

namespace Sentry.AspNetCore
{
    /// <summary>
    /// Sentry middleware for ASP.NET Core
    /// </summary>
    public class SentryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHub _sentry;
        private readonly SentryAspNetCoreOptions _options;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ILogger<SentryMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SentryMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next.</param>
        /// <param name="sentry">The sentry.</param>
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
            IHub sentry,
            SentryAspNetCoreOptions options,
            IHostingEnvironment hostingEnvironment,
            ILogger<SentryMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _sentry = sentry ?? throw new ArgumentNullException(nameof(sentry));
            _options = options;
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
            using (_sentry.PushScope())
            {
                _sentry.ConfigureScope(scope =>
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

                void CaptureException(Exception e)
                {
                    var evt = new SentryEvent(e);

                    _logger?.LogTrace("Sending event '{SentryEvent}' to Sentry.", evt);

                    var id = _sentry.CaptureEvent(evt);

                    _logger?.LogInformation("Event '{id}' queued .", id);
                }
            }
        }

        internal void PopulateScope(HttpContext context, Scope scope)
        {
            if (_hostingEnvironment != null)
            {
                scope.Environment = _hostingEnvironment.EnvironmentName;
                scope.SetWebRoot(_hostingEnvironment.WebRootPath);
            }

            // TODO: Find route template (MVC integration)
            // TODO: optionally get transaction from request through a dependency
            //scope.Transaction = context.Request.PathBase;

            scope.Populate(context);

            if (_options?.IncludeActivityData == true)
            {
                scope.Populate(Activity.Current);
            }
        }
    }
}
