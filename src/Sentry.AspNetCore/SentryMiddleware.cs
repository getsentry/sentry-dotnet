using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Sentry.AspNetCore
{
    /// <summary>
    /// Sentry middleware for ASP.NET Core
    /// </summary>
    public class SentryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHub _sentry;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ILogger<SentryMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SentryMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next.</param>
        /// <param name="sentry">The sentry.</param>
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
            IHostingEnvironment hostingEnvironment,
            ILogger<SentryMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _sentry = sentry ?? throw new ArgumentNullException(nameof(sentry));
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
            var scopeGuard = _sentry.PushScope();
            _sentry.ConfigureScope(s =>
            {
                s.OnEvaluating += (sender, args) =>
                {
                    s.Environment = _hostingEnvironment?.EnvironmentName;

                    // At the point lots of stuff from the request are not yet filled
                    // Identity for example is added later on in the pipeline
                    // Evaluating this callback must be done prior to an event being sent
                    // also to avoid paying the cost to get it run when no event is sent at all
                    context.SentryScopeApply(s);
                };
            });
            try
            {
                await _next(context).ConfigureAwait(false);

                // TODO: Consider IExceptionHandlerFeature instead. Isn't Path the same as context route Path?
                // When an exception was handled by other component (i.e: UseExceptionHandler feature).
                var exceptionFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                if (exceptionFeature?.Error != null)
                {
                    if (exceptionFeature.Path != null)
                    {
                        // TODO: Transaction field instead?
                        _sentry.ConfigureScope(p => p.SetTag("Path", exceptionFeature.Path));
                    }

                    CaptureException(context, exceptionFeature.Error);
                }
            }
            catch (Exception e)
            {
                CaptureException(context, e);

                ExceptionDispatchInfo.Capture(e).Throw();
            }
            finally
            {
                scopeGuard.Dispose();
            }
        }

        // TODO: extend Hub?
        private void CaptureException(HttpContext context, Exception e)
        {
            var evt = new SentryEvent(e);

            // TODO: Ignore logs from Sentry by Sentry MEL integration
            _logger?.LogTrace("Sending event to Sentry '{SentryEvent}'.", evt);

            var response = _sentry.CaptureEvent(evt);

            _logger?.LogInformation("Event sent to Sentry '{SentryResponse}'.", response);
            Debug.WriteLine(response);

            // TODO: Set Id on response header?
            // i.e: middlewares behind can retrieve it
            // and an SPA could read it
        }
    }
}
