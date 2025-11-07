namespace Sentry.Extensions.AI;

/// <summary>
/// Sentry AI instrumentation options
/// </summary>
public class SentryAIOptions
{
    /// <summary>
    /// Whether to include request messages in spans.
    /// </summary>
    public bool RecordInputs { get; set; } = true;

    /// <summary>
    /// Whether to include response content in spans.
    /// </summary>
    public bool RecordOutputs { get; set; } = true;

    /// <summary>
    /// Name of the AI Agent
    /// </summary>
    public string AgentName { get; set; } = "Agent";
}
