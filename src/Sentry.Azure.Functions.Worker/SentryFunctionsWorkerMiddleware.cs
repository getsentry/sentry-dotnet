using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Azure.Functions.Worker;

internal class SentryFunctionsWorkerMiddleware : IFunctionsWorkerMiddleware
{
    private const string Operation = "function";

    private readonly IHub _hub;
    private readonly IDiagnosticLogger? _logger;
    private static readonly ConcurrentDictionary<string, string> TransactionNameCache = new();

    public SentryFunctionsWorkerMiddleware(IHub hub)
    {
        _hub = hub;
        _logger = hub.GetSentryOptions()?.DiagnosticLogger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var transactionContext = await StartOrContinueTraceAsync(context);
        var transaction = _hub.StartTransaction(transactionContext);
        Exception? unhandledException = null;

        try
        {
            _hub.ConfigureScope(scope =>
            {
                scope.Transaction = transaction;

                scope.Contexts["function"] = new Dictionary<string, string>
                {
                    { "name", context.FunctionDefinition.Name },
                    { "entryPoint", context.FunctionDefinition.EntryPoint },
                    { "invocationId", context.InvocationId }
                };
            });

            context.CancellationToken.ThrowIfCancellationRequested();

            await next(context);
        }
        catch (Exception exception)
        {
            exception.SetSentryMechanism(nameof(SentryFunctionsWorkerMiddleware),
                "This exception was caught by the Sentry Functions middleware. " +
                "The Function has thrown an exception that was not handled by the user code.",
                handled: false);

            unhandledException = exception;

            throw;
        }
        finally
        {
            if (unhandledException is not null)
            {
                transaction.Finish(unhandledException);
            }
            else
            {
                var statusCode = context.GetHttpResponseData()?.StatusCode;

                // For HTTP triggered function, finish transaction with the returned HTTP status code
                if (statusCode is not null)
                {
                    var status = SpanStatusConverter.FromHttpStatusCode(statusCode.Value);

                    transaction.Finish(status);
                }
                else
                {
                    transaction.Finish();
                }
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = AotHelper.SuppressionJustification)]
    private async Task<TransactionContext> StartOrContinueTraceAsync(FunctionContext context)
    {
        var transactionName = context.FunctionDefinition.Name;

        // Get the HTTP request data
        var requestData = await context.GetHttpRequestDataAsync();
        if (requestData is null)
        {
            // not an HTTP trigger
            return SentrySdk.ContinueTrace((SentryTraceHeader?)null, (BaggageHeader?)null, transactionName, Operation);
        }

        var httpMethod = requestData.Method.ToUpperInvariant();
        var transactionNameKey = $"{context.FunctionDefinition.EntryPoint}-{httpMethod}";

        // Note that, when Trimming is enabled, we can't use reflection to read route data from the HttpTrigger
        // attribute. In that case the route name will always be /api/<FUNCTION_NAME>
        // If this is ever a problem for customers, we can potentially see if there are alternate ways to get this info
        // from route tables or something. We're not even sure if anyone will use this functionality for now though. 
        if (!AotHelper.IsNativeAot && !TransactionNameCache.TryGetValue(transactionNameKey, out transactionName))
        {
            // Find the HTTP Trigger attribute via reflection
            var assembly = Assembly.LoadFrom(context.FunctionDefinition.PathToAssembly);
            var entryPointName = context.FunctionDefinition.EntryPoint;

            var typeName = entryPointName[..entryPointName.LastIndexOf('.')];
            var methodName = entryPointName[(typeName.Length + 1)..];
            var attribute = assembly.GetType(typeName)?.GetMethod(methodName)?.GetParameters()
                .Select(p => p.GetCustomAttribute<HttpTriggerAttribute>())
                .FirstOrDefault(a => a is not null);

            transactionName = attribute?.Route is { } route
                // Compose the transaction name from the method and route
                ? $"{httpMethod} /{route.TrimStart('/')}"
                // There's no route provided, so use the absolute path of the URL
                : $"{httpMethod} {requestData.Url.AbsolutePath}";

            TransactionNameCache.TryAdd(transactionNameKey, transactionName);
        }

        var traceHeader = requestData.TryGetSentryTraceHeader(_logger);
        var baggageHeader = requestData.TryGetBaggageHeader(_logger);

        return SentrySdk.ContinueTrace(traceHeader, baggageHeader, transactionName, Operation);
    }
}
