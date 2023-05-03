using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Sentry.AzureFunctions.Worker.Tests;

public class SentryFunctionsWorkerMiddlewareTests
{
    [Fact]
    public async Task X()
    {
        // TODO: is there a testing IHub?

        var hub = Substitute.For<IHub>();
        var functionContext = Substitute.For<FunctionContext>();

        FunctionExecutionDelegate functionExecutionDelegate = context => Task.CompletedTask;

        var sut = new SentryFunctionsWorkerMiddleware(hub);

        await sut.Invoke(functionContext, functionExecutionDelegate);
    }
}
