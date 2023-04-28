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
            _hub.ConfigureScope(scope =>
            {
                var transaction = _hub.StartTransaction(context.FunctionDefinition.Name, "function");
                scope.Transaction = transaction;

                // TODO: how to indicate transaction was aborted
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
}
