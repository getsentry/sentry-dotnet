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
    /// The list of strings which FunctionInvokingChatClient(FICC) uses to start the tool call <see cref="Activity"/>.
    /// </summary>
    internal static readonly string[] FICCActivityNames = ["orchestrate_tools", "FunctionInvokingChatClient.GetResponseAsync", "FunctionInvokingChatClient"];

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
        // Operations
        internal const string OperationName = "gen_ai.operation.name";
        internal const string InvokeAgentOperation = "gen_ai.invoke_agent";
        internal const string InvokeAgentDescription = "invoke_agent";
        internal const string ChatOperation = "gen_ai.chat";
        internal const string ToolCallOperation = "gen_ai.execute_tool";

        // Agent
        internal const string AgentName = "gen_ai.agent.name";

        // Request attributes
        internal const string RequestModel = "gen_ai.request.model";
        internal const string RequestMessages = "gen_ai.request.messages";
        internal const string RequestAvailableTools = "gen_ai.request.available_tools";
        internal const string RequestTemperature = "gen_ai.request.temperature";
        internal const string RequestMaxTokens = "gen_ai.request.max_tokens";
        internal const string RequestTopP = "gen_ai.request.top_p";
        internal const string RequestFrequencyPenalty = "gen_ai.request.frequency_penalty";
        internal const string RequestPresencePenalty = "gen_ai.request.presence_penalty";

        // Response attributes
        internal const string ResponseText = "gen_ai.response.text";
        internal const string ResponseToolCalls = "gen_ai.response.tool_calls";
        internal const string ResponseModel = "gen_ai.response.model";

        // Usage attributes
        internal const string UsageInputTokens = "gen_ai.usage.input_tokens";
        internal const string UsageOutputTokens = "gen_ai.usage.output_tokens";
        internal const string UsageTotalTokens = "gen_ai.usage.total_tokens";

        // Tool attributes
        internal const string ToolName = "gen_ai.tool.name";
        internal const string ToolDescription = "gen_ai.tool.description";
        internal const string ToolInput = "gen_ai.tool.input";
        internal const string ToolOutput = "gen_ai.tool.output";
    }
}
