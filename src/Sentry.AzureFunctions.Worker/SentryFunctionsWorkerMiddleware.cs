using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Sentry.AzureFunctions.Worker;

internal class SentryFunctionsWorkerMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IHub _hub;
    private static readonly ConcurrentDictionary<string, string> TransactionNameCache = new();

    public SentryFunctionsWorkerMiddleware(IHub hub)
    {
        _hub = hub;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var transactionName = await GetTransactionNameAsync(context).ConfigureAwait(false) ?? context.FunctionDefinition.Name;
        var transaction = _hub.StartTransaction(transactionName, "function");
        Exception? unhandledException = null;

        try
        {
            _hub.ConfigureScope(scope =>
            {
                scope.Transaction = transaction;

                // AzureFunctions_FunctionName and AzureFunctions_InvocationId are already set by the time we get here.
                // Clear those and replace with "function" scope context.
                scope.UnsetTag("AzureFunctions_FunctionName");
                scope.UnsetTag("AzureFunctions_InvocationId");

                scope.Contexts["function"] = new Dictionary<string, string>
                {
                    { "name", context.FunctionDefinition.Name },
                    { "entryPoint", context.FunctionDefinition.EntryPoint },
                    { "invocationId", context.InvocationId }
                };
            });

            context.CancellationToken.ThrowIfCancellationRequested();

            await next(context).ConfigureAwait(false);
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
                transaction.Finish();
            }
        }
    }

    private static async Task<string?> GetTransactionNameAsync(FunctionContext context)
    {
        // Get the HTTP request data
        var requestData = await context.GetHttpRequestDataAsync().ConfigureAwait(false);
        if (requestData is null)
        {
            // not an HTTP trigger
            return null;
        }

        var httpMethod = requestData.Method.ToUpperInvariant();

        var transactionNameKey = $"{context.FunctionDefinition.EntryPoint}-{httpMethod}";
        if (TransactionNameCache.TryGetValue(transactionNameKey, out var value))
        {
            return value;
        }

        // Find the HTTP Trigger attribute via reflection
        var assembly = Assembly.LoadFrom(context.FunctionDefinition.PathToAssembly);
        var entryPointName = context.FunctionDefinition.EntryPoint;

        var typeName = entryPointName[..entryPointName.LastIndexOf('.')];
        var methodName = entryPointName[(typeName.Length + 1)..];
        var attribute = assembly.GetType(typeName)?.GetMethod(methodName)?.GetParameters()
            .Select(p => p.GetCustomAttribute<HttpTriggerAttribute>())
            .FirstOrDefault(a => a is not null);

        var transactionName = attribute?.Route is { } route
            // Compose the transaction name from the method and route
            ? $"{httpMethod} /{route.TrimStart('/')}"
            // There's no route provided, so use the absolute path of the URL
            : $"{httpMethod} {requestData.Url.AbsolutePath}";

        TransactionNameCache.TryAdd(transactionNameKey, transactionName);

        return transactionName;
    }
}
