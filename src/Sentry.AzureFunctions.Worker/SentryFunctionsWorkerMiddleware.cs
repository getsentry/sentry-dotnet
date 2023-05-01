using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Sentry.AzureFunctions.Worker;

internal class SentryFunctionsWorkerMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IHub _hub;

    public SentryFunctionsWorkerMiddleware(IHub hub)
    {
        _hub = hub;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            var transactionName = await GetTransactionNameAsync(context) ?? context.FunctionDefinition.Name;

            _hub.ConfigureScope(scope =>
            {
                var transaction = _hub.StartTransaction(transactionName, "function");
                scope.Transaction = transaction;

                context.CancellationToken.Register(() => scope.SetExtra("aborted", true));

                scope.UnsetTag("AzureFunctions_FunctionName");
                scope.UnsetTag("AzureFunctions_InvocationId");

                scope.Contexts["function"] = new Dictionary<string, string>
                {
                    { "name", context.FunctionDefinition.Name },
                    { "entryPoint", context.FunctionDefinition.EntryPoint },
                    { "invocationId", context.InvocationId }
                };
            });

            await next(context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            exception.SetSentryMechanism(nameof(SentryFunctionsWorkerMiddleware),
                "This exception was caught by the Sentry Functions middleware. " +
                "The Function has thrown an exception that was not handled by the user code.",
                handled: false);

            throw;
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

        // Find the HTTP Trigger attribute via reflection
        var assembly = Assembly.LoadFrom(context.FunctionDefinition.PathToAssembly);
        var entryPointName = context.FunctionDefinition.EntryPoint;
        var typeName = entryPointName[..entryPointName.LastIndexOf('.')];
        var methodName = entryPointName[(typeName.Length + 1)..];
        var attribute = assembly.GetType(typeName)?.GetMethod(methodName)?.GetParameters()
            .Select(p => p.GetCustomAttribute<HttpTriggerAttribute>())
            .FirstOrDefault(a => a is not null);

        // Compose the transaction name from the method and route
        var method = requestData.Method.ToUpperInvariant();
        if (attribute?.Route is {} route)
        {
            return $"{method} /{route.TrimStart('/')}";
        }

        // There's no route provided, so use the absolute path of the URL
        return $"{method} {requestData.Url.AbsolutePath}";
    }
}
