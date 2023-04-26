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
                var transaction = _hub.StartTransaction(context.FunctionDefinition.Name, context.FunctionDefinition.EntryPoint);
                scope.Transaction = transaction;

                // TODO: how to indicate transaction was aborted
                context.CancellationToken.Register(() => scope.SetExtra("aborted", true));

                scope.SetTag("function.name", context.FunctionDefinition.Name);
                scope.SetTag("function.entryPoint", context.FunctionDefinition.EntryPoint);
                scope.SetTag("function.invocationId", context.InvocationId);

                scope.UnsetTag("AzureFunctions_FunctionName");
                scope.UnsetTag("AzureFunctions_InvocationId");
                scope.UnsetTag("functionName");
                scope.UnsetTag("invocationId");
            });

            await next(context);
        }
        catch (Exception exception)
        {
            // TODO: do I need to do here anything at all?

            var ex = exception;

            throw;
        }

    }
}
