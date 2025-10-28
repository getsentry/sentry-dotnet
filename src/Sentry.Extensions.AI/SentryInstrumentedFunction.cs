using Microsoft.Extensions.AI;
using Sentry.Extensibility;

namespace Sentry.Extensions.AI;

internal sealed class SentryInstrumentedFunction(AIFunction innerFunction, ChatOptions options)
    : DelegatingAIFunction(innerFunction)
{
    private static readonly HubAdapter Hub = HubAdapter.Instance;

    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        var currSpan = InitToolSpan(arguments);
        RemoveSentryArgs(ref arguments);
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

    private ISpan InitToolSpan(AIFunctionArguments arguments)
    {
        const string operation = "gen_ai.execute_tool";
        var spanName = $"execute_tool {Name}";
        ISpan currSpan;

        if (arguments.TryGetValue(SentryAIConstants.KeyMessageFunctionArgumentDictKey,
                out var keyMessage)
            && keyMessage is ChatMessage message
            && SentryChatClient.GetMessageToSpanDict(options).TryGetValue(message, out var agentSpan))
        {
            currSpan = agentSpan.StartChild(operation, spanName);
        }
        else
        {
            // If we couldn't find the agent span, just attach it to the hub's current scope
            currSpan = Hub.StartSpan(operation, spanName);
        }

        currSpan.SetData("gen_ai.request.model", options?.ModelId);
        currSpan.SetData("gen_ai.operation.name", "execute_tool");
        currSpan.SetData("gen_ai.tool.name", Name);
        currSpan.SetData("gen_ai.tool.description", Description);
        currSpan.SetData("gen_ai.tool.input", arguments);

        return currSpan;
    }

    private static void RemoveSentryArgs(ref AIFunctionArguments arguments)
    {
        arguments.Remove(SentryAIConstants.KeyMessageFunctionArgumentDictKey);
    }
}
