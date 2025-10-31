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
        try
        {
            var result = await base.InvokeCoreAsync(arguments, cancellationToken).ConfigureAwait(false);

            if (result?.ToString() is { } resultString)
            {
                currSpan.SetData(SentryAIConstants.SpanAttributes.ToolOutput, resultString);
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
        var spanName = $"execute_tool {Name}";
        var agentSpan = SentryAIUtil.GetActivitySpan();

        var currSpan = agentSpan != null
            ? agentSpan.StartChild(SentryAIConstants.SpanAttributes.ToolCallOperation, spanName)
            // If we couldn't find the agent span, just attach it to the hub's current scope
            : Hub.StartSpan(SentryAIConstants.SpanAttributes.ToolCallOperation, spanName);

        currSpan.SetData(SentryAIConstants.SpanAttributes.RequestModel, options?.ModelId);
        currSpan.SetData(SentryAIConstants.SpanAttributes.OperationName, "execute_tool");
        currSpan.SetData(SentryAIConstants.SpanAttributes.ToolName, Name);
        currSpan.SetData(SentryAIConstants.SpanAttributes.ToolDescription, Description);
        currSpan.SetData(SentryAIConstants.SpanAttributes.ToolInput, arguments);

        return currSpan;
    }
}
