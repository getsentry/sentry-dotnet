using Microsoft.Extensions.AI;
using Sentry.Extensibility;

namespace Sentry.Extensions.AI;

internal sealed class SentryInstrumentedFunction(AIFunction innerFunction, ChatOptions? aiOptions = null)
    : DelegatingAIFunction(innerFunction)
{
    private readonly HubAdapter _hub = HubAdapter.Instance;

    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        const string operation = "gen_ai.execute_tool";
        var spanName = $"execute_tool {Name}";
        var currSpan = SentryChatClient.RootSpan == null ?
            _hub.StartSpan(operation, spanName) :
            SentryChatClient.RootSpan.StartChild(operation, spanName);

        currSpan.SetData("gen_ai.request.model", aiOptions?.ModelId);

        currSpan.SetData("gen_ai.operation.name", "execute_tool");
        currSpan.SetData("gen_ai.tool.name", Name);
        currSpan.SetData("gen_ai.tool.description", Description);

        currSpan.SetData("gen_ai.tool.input", arguments);

        try
        {
            var result = await base.InvokeCoreAsync(arguments, cancellationToken).ConfigureAwait(false);

            if (result?.ToString() is { } resultString)
            {
                currSpan.SetData("gen_ai.tool.output", resultString);
            }

            currSpan.Finish(SpanStatus.Ok);
            return result;
        }
        catch (Exception ex)
        {
            currSpan.Finish(ex);
            throw;
        }
    }
}
