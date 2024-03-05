using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.AspNetCore.Extensions;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Reflection;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;

namespace Sentry.AspNetCore;

/// <summary>
/// Sentry middleware for ASP.NET Core
/// </summary>
internal class SentryMiddleware : IMiddleware
{
    internal static readonly object TraceHeaderItemKey = new();
    internal static readonly object BaggageHeaderItemKey = new();
    internal static readonly object TransactionContextItemKey = new();

    private readonly Func<IHub> _getHub;
    private readonly SentryAspNetCoreOptions _options;
    private readonly IHostingEnvironment _hostingEnvironment;
    private readonly ILogger<SentryMiddleware> _logger;
    private readonly IEnumerable<ISentryEventExceptionProcessor> _eventExceptionProcessors;
    private readonly IEnumerable<ISentryEventProcessor> _eventProcessors;
    private readonly IEnumerable<ISentryTransactionProcessor> _transactionProcessors;

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
    /// <param name="transactionProcessors">Custom Transaction Processors</param>
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
        IEnumerable<ISentryEventProcessor> eventProcessors,
        IEnumerable<ISentryTransactionProcessor> transactionProcessors)
    {
        _getHub = getHub ?? throw new ArgumentNullException(nameof(getHub));
        _options = options.Value;
        _hostingEnvironment = hostingEnvironment;
        _logger = logger;
        _eventExceptionProcessors = eventExceptionProcessors;
        _eventProcessors = eventProcessors;
        _transactionProcessors = transactionProcessors;
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
                context.Response.OnCompleted(() => hub.FlushAsync(_options.FlushTimeout));
            }

            var traceHeader = context.TryGetSentryTraceHeader(_options);
            var baggageHeader = context.TryGetBaggageHeader(_options);
            var transactionContext = hub.ContinueTrace(traceHeader, baggageHeader);

            // Adding the headers and the TransactionContext to the context to be picked up by the Sentry tracing middleware
            var didAdd = context.Items.TryAdd(TraceHeaderItemKey, traceHeader);
            if (!didAdd)
            {
                _options.LogWarning("Sentry trace was already added. Did you initialize Sentry twice?");
            }
            context.Items.TryAdd(BaggageHeaderItemKey, baggageHeader);
            context.Items.TryAdd(TransactionContextItemKey, transactionContext);

            hub.ConfigureScope(scope =>
            {
                // At the point lots of stuff from the request are not yet filled
                // Identity for example is added later on in the pipeline
                // Subscribing to the event so that HTTP data is only read in case an event is going to be
                // sent to Sentry. This avoid the cost on error-free requests.
                // In case of event, all data made available through the HTTP Context at the time of the
                // event creation will be sent to Sentry

                // Important: The scope that the event is attached to is not necessarily the same one that is active
                // when the event fires.  Use `activeScope`, not `scope` or `hub`.
                scope.OnEvaluating += (_, activeScope) =>
                {
                    SyncOptionsScope(activeScope);
                    PopulateScope(context, activeScope);
                };
            });

            // Pre-create the Sentry Event ID and save it on the scope it so it's available throughout the pipeline,
            // even if there's no event actually being sent to Sentry.  This allows for things like a custom exception
            // handler page to access the event ID, enabling user feedback, etc.
            var eventId = SentryId.Create();
            hub.ConfigureScope(scope => scope.LastEventId = eventId);

            try
            {
                var originalMethod = context.Request.Method;
                await next(context).ConfigureAwait(false);
                if (_options.Instrumenter == Instrumenter.OpenTelemetry && Activity.Current is { } activity)
                {
                    // The middleware pipeline finishes up before the Otel Activity.OnEnd callback is invoked so we need
                    // so save a copy of the scope that can be restored by our SentrySpanProcessor
                    hub.ConfigureScope(scope => activity.SetFused(scope));
                }

                // When an exception was handled by other component (i.e: UseExceptionHandler feature).
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature?>();
                if (exceptionFeature?.Error != null)
                {
                    const string description =
                        "This exception was caught by an ASP.NET Core custom error handler. " +
                        "The web server likely returned a customized error page as a result of this exception.";

#if NET6_0_OR_GREATER
                    hub.ConfigureScope(scope =>
                    {
                        scope.ExceptionProcessors.Add(
                            new ExceptionHandlerFeatureProcessor(originalMethod, exceptionFeature)
                            );
                    });
#endif
                    CaptureException(exceptionFeature.Error, eventId, "IExceptionHandlerFeature", description);
                }

                if (_options.FlushBeforeRequestCompleted)
                {
                    await FlushBeforeCompleted().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                const string description =
                    "This exception was captured by the Sentry ASP.NET Core middleware, and then re-thrown." +
                    "The web server likely returned a 5xx error code as a result of this exception.";

                CaptureException(e, eventId, "SentryMiddleware.UnhandledException", description);

                if (_options.FlushBeforeRequestCompleted)
                {
                    await FlushBeforeCompleted().ConfigureAwait(false);
                }

                ExceptionDispatchInfo.Capture(e).Throw();
            }

            // Some environments disables the application after sending a request,
            // preventing OnCompleted flush from working.
            Task FlushBeforeCompleted() => hub.FlushAsync(_options.FlushTimeout);

            void CaptureException(Exception e, SentryId evtId, string mechanism, string description)
            {
                e.SetSentryMechanism(mechanism, description, handled: false);

                var evt = new SentryEvent(e, eventId: evtId);

                _logger.LogTrace("Sending event '{SentryEvent}' to Sentry.", evt);

                var id = hub.CaptureEvent(evt);

                _logger.LogInformation("Event '{id}' queued.", id);
            }
        }
    }

    private void SyncOptionsScope(Scope scope)
    {
        foreach (var callback in _options.ConfigureScopeCallbacks)
        {
            callback.Invoke(scope);
        }
    }

    internal void PopulateScope(HttpContext context, Scope scope)
    {
        scope.AddEventProcessors(_eventProcessors.Except(scope.GetAllEventProcessors()));
        scope.AddExceptionProcessors(_eventExceptionProcessors.Except(scope.GetAllExceptionProcessors()));
        scope.AddTransactionProcessors(_transactionProcessors.Except(scope.GetAllTransactionProcessors()));
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
