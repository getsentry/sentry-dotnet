using Sentry.Infrastructure;

namespace Sentry.Extensions.AI;

/// <summary>
/// Sentry AI instrumentation options
/// </summary>
public class SentryAIOptions
{
    /// <summary>
    /// Experimental Sentry AI features.
    /// </summary>
    /// <remarks>
    /// This and related experimental APIs may change in the future.
    /// </remarks>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public SentryAIExperimentalOptions Experimental { get; set; } = new();

    /// <summary>
    /// Experimental Sentry AI options.
    /// </summary>
    /// <remarks>
    /// This and related experimental APIs may change in the future.
    /// </remarks>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public sealed class SentryAIExperimentalOptions
    {
        internal SentryAIExperimentalOptions()
        {
        }

        /// <summary>
        /// Whether to include request messages in spans.
        /// <para>This API is experimental, and it may change in the future.</para>
        /// </summary>
        public bool RecordInputs { get; set; } = true;

        /// <summary>
        /// Whether to include response content in spans.
        /// <para>This API is experimental, and it may change in the future.</para>
        /// </summary>
        public bool RecordOutputs { get; set; } = true;

        /// <summary>
        /// Name of the AI Agent
        /// <para>This API is experimental, and it may change in the future.</para>
        /// </summary>
        public string AgentName { get; set; } = "Agent";
    }
}
