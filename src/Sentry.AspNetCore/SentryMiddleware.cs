using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Diagnostics;
#if NETSTANDARD2_0
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
#endif
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensibility;
using Sentry.Protocol;
using Sentry.Reflection;

namespace Sentry.AspNetCore;

/// <summary>
/// Sentry middleware for ASP.NET Core
/// </summary>
internal class SentryMiddleware : IMiddleware
{
    private readonly Func<IHub> _getHub;
    private readonly SentryAspNetCoreOptions _options;
    private readonly IHostingEnvironment _hostingEnvironment;
    private readonly ILogger<SentryMiddleware> _logger;
    private readonly IEnumerable<ISentryEventExceptionProcessor> _eventExceptionProcessors;
    private readonly IEnumerable<ISentryEventProcessor> _eventProcessors;
    internal static readonly SdkVersion NameAndVersion
        = typeof(SentryMiddleware).Assembly.GetNameAndVersion();

    private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="SentryMiddleware"/> class.
    /// </summary>
    /// <param name="getHub">The sentry Hub accessor.</param>
    /// <param name="options">The options for this integration</param>
    /// <param name="hostingEnvironment">The hosting environment.</param>
    /// <param name="logger">Sentry logger.</param>
    /// <param name="eventExceptionProcessors">Custom Event Exception Processors</param>
    /// <param name="eventProcessors">Custom Event Processors</param>
    /// <exception cref="ArgumentNullException">
    /// next
    /// or
    /// sentry
    /// </exception>
    public SentryMiddleware(
        Func<IHub> getHub,
        IOptions<SentryAspNetCoreOptions> options,
        IHostingEnvironment hostingEnvironment,
        ILogger<SentryMiddleware> logger,
        IEnumerable<ISentryEventExceptionProcessor> eventExceptionProcessors,
        IEnumerable<ISentryEventProcessor> eventProcessors)
    {
        _getHub = getHub ?? throw new ArgumentNullException(nameof(getHub));
        _options = options.Value;
        _hostingEnvironment = hostingEnvironment;
        _logger = logger;
        _eventExceptionProcessors = eventExceptionProcessors;
        _eventProcessors = eventProcessors;
    }

    /// <summary>
    /// Handles the <see cref="HttpContext"/> while capturing any errors
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="next">Delegate to next middleware.</param>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var hub = _getHub();
        if (!hub.IsEnabled)
        {
            await next(context).ConfigureAwait(false);
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
                // Serverless environments flush the queue at the end of each request
                context.Response.OnCompleted(() => hub.FlushAsync(timeout: _options.FlushTimeout));
            }

            hub.ConfigureScope(scope =>
            {
                // At the point lots of stuff from the request are not yet filled
                // Identity for example is added later on in the pipeline
                // Subscribing to the event so that HTTP data is only read in case an event is going to be
                // sent to Sentry. This avoid the cost on error-free requests.
                // In case of event, all data made available through the HTTP Context at the time of the
                // event creation will be sent to Sentry
                scope.OnEvaluating += (_, _) =>
                {
                    SyncOptionsScope(hub);
                    PopulateScope(context, scope);
                };
            });

            // Pre-create the Sentry Event ID and save it on the scope it so it's available throughout the pipeline,
            // even if there's no event actually being sent to Sentry.  This allows for things like a custom exception
            // handler page to access the event ID, enabling user feedback, etc.
            var eventId = SentryId.Create();
            hub.ConfigureScope(scope => scope.LastEventId = eventId);

            try
            {
                await next(context).ConfigureAwait(false);

                // When an exception was handled by other component (i.e: UseExceptionHandler feature).
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature?>();
                if (exceptionFeature?.Error != null)
                {
                    CaptureException(exceptionFeature.Error, eventId, "IExceptionHandlerFeature");
                }
                if (_options.FlushBeforeRequestCompleted)
                {
                    await FlushBeforeCompleted().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                CaptureException(e, eventId, "SentryMiddleware.UnhandledException");
                if (_options.FlushBeforeRequestCompleted)
                {
                    await FlushBeforeCompleted().ConfigureAwait(false);
                }

                ExceptionDispatchInfo.Capture(e).Throw();
            }

            // Some environments disables the application after sending a request,
            // making the OnCompleted flush to not work.
            Task FlushBeforeCompleted() => hub.FlushAsync(timeout: _options.FlushTimeout);

            void CaptureException(Exception e, SentryId eventId, string mechanism)
            {
                e.Data[Mechanism.HandledKey] = false;
                e.Data[Mechanism.MechanismKey] = mechanism;

                var evt = new SentryEvent(e, eventId: eventId);

                _logger.LogTrace("Sending event '{SentryEvent}' to Sentry.", evt);

                var id = hub.CaptureEvent(evt);

                _logger.LogInformation("Event '{id}' queued.", id);
            }
        }
    }

    private void SyncOptionsScope(IHub newHub)
    {
        foreach (var callback in _options.ConfigureScopeCallbacks)
        {
            newHub.ConfigureScope(callback);
        }
    }

    internal void PopulateScope(HttpContext context, Scope scope)
    {
        scope.AddEventProcessors(_eventProcessors.Except(scope.GetAllEventProcessors()));
        scope.AddExceptionProcessors(_eventExceptionProcessors.Except(scope.GetAllExceptionProcessors()));
        scope.Sdk.Name = Constants.SdkName;
        scope.Sdk.Version = NameAndVersion.Version;

        if (NameAndVersion.Version is { } version)
        {
            scope.Sdk.AddPackage(ProtocolPackageName, version);
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
}
