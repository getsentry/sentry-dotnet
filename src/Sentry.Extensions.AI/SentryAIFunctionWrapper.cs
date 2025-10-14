using Microsoft.Extensions.AI;

namespace Sentry.Extensions.AI;

public static class SentryAIFunctionWrapper
{
    private static AITool WrapToolCall(AITool aiTool, IHub hub)
    {
        if (aiTool is not AIFunction tool)
        {
            return aiTool;
        }

        tool.UnderlyingMethod = () =>
        {
            const string operation = "gen_ai.execute_tool";
            var spanName = aiTool.Name;
            var toolSpan = hub.GetSpan()?.StartChild(operation, spanName) ?? hub.StartTransaction(spanName, operation);
            var returnVal = tool.UnderlyingMethod.Invoke();
            return returnVal;
        }
    }

}
