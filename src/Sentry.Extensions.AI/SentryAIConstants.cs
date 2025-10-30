using Microsoft.Extensions.AI;

namespace Sentry.Extensions.AI;

internal static class SentryAIConstants
{
    /// <summary>
    /// <para>
    /// Sentry will add a <see cref="ISpan"/> to AdditionalAttribute in <see cref="ChatOptions"/>.
    /// </para>
    /// <para>
    /// This constant represents the string key to get the span which represents the agent span.
    /// </para>
    /// </summary>
    internal const string OptionsAdditionalAttributeAgentSpanName = "SentryChatMessageAgentSpan";

    /// <summary>
    /// <para>
    /// When an LLM uses a tool, Sentry will add an argument to <see cref="AIFunctionArguments"/>.
    /// The additional argument will contain the request <see cref="ChatMessage"/> which initialized the tool call.
    /// </para>
    /// <para>
    /// This constant represents the string key to get the message.
    /// </para>
    /// </summary>
    internal const string KeyMessageFunctionArgumentDictKey = "SentrySpanToMessageDictKey";

    /// <summary>
    /// The string which FunctionInvokingChatClient(FICC) uses to start the tool call <see cref="Activity"/>.
    /// </summary>
    /// <remarks>
    /// This string is valid from version 9.10 (inclusive). Previous versions of <c>Microsoft.Extensions.AI</c> has
    /// a lot of different strings to run <c>StartActivity</c>.
    /// </remarks>
    internal const string FICCActivityName = "orchestrate_tools";

    /// <summary>
    /// The string we use to identify our <see cref="ActivitySource"/>.
    /// </summary>
    internal const string SentryActivitySourceName = "Sentry.AgentMonitoring";

    /// <summary>
    /// The string we use to retrieve the span from the <see cref="Activity"/> using a Fused property
    /// </summary>
    internal const string SentryActivitySpanAttributeName = "SentryCurrSpan";

    internal static class SpanAttributes
    {
        internal const string InvokeAgentOperation = "gen_ai.invoke_agent";
        internal const string InvokeAgentDescription = "invoke_agent";
        internal const string ChatOperation = "gen_ai.chat";
        internal const string ToolCallOperation = "gen_ai.execute_tool";
        internal const string RequestModel = "gen_ai.request.model";
        internal const string OperationName = "gen_ai.operation.name";
        internal const string ToolName = "gen_ai.tool.name";
        internal const string ToolDescription = "gen_ai.tool.description";
        internal const string ToolInput = "gen_ai.tool.input";
    }

}
