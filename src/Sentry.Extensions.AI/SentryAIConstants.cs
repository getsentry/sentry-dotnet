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
}
