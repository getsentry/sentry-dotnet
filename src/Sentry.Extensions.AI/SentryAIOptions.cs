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
        private bool? _recordInputs;
        private bool? _recordOutputs;

        /// <summary>
        /// Whether to include AI inputs in spans: system instructions, request messages, tool definitions and tool
        /// call arguments.
        /// <para>
        /// Defaults to <c>true</c>. When not set and <see cref="SentryOptions.ConfigureDataCollection"/> has been
        /// called, <see cref="DataCollectionOptions.GenAIOptions.Inputs"/> decides instead.
        /// </para>
        /// <para>This API is experimental, and it may change in the future.</para>
        /// </summary>
        public bool RecordInputs
        {
            get => _recordInputs ?? true;
            set => _recordInputs = value;
        }

        /// <summary>
        /// Whether to include AI outputs in spans: response text, response tool calls and tool call results.
        /// <para>
        /// Defaults to <c>true</c>. When not set and <see cref="SentryOptions.ConfigureDataCollection"/> has been
        /// called, <see cref="DataCollectionOptions.GenAIOptions.Outputs"/> decides instead.
        /// </para>
        /// <para>This API is experimental, and it may change in the future.</para>
        /// </summary>
        public bool RecordOutputs
        {
            get => _recordOutputs ?? true;
            set => _recordOutputs = value;
        }

        /// <summary>
        /// Name of the AI Agent
        /// <para>This API is experimental, and it may change in the future.</para>
        /// </summary>
        public string AgentName { get; set; } = "Agent";

        internal bool ShouldRecordInputs(SentryOptions? options) =>
            _recordInputs ?? options?.DataCollectionResolver.GenAIInputs ?? true;

        internal bool ShouldRecordOutputs(SentryOptions? options) =>
            _recordOutputs ?? options?.DataCollectionResolver.GenAIOutputs ?? true;
    }
}
