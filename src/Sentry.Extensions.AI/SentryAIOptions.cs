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
        /// Whether to include request messages in spans.
        /// <para>
        /// Defaults to <c>true</c>. Once <see cref="SentryOptions.ConfigureDataCollection"/> has been called, this
        /// also covers tool definitions and tool call arguments, and when not set,
        /// <see cref="DataCollectionOptions.GenAIOptions.Inputs"/> decides instead.
        /// </para>
        /// <para>This API is experimental, and it may change in the future.</para>
        /// </summary>
        public bool RecordInputs
        {
            get => _recordInputs ?? true;
            set => _recordInputs = value;
        }

        /// <summary>
        /// Whether to include response content in spans.
        /// <para>
        /// Defaults to <c>true</c>. Once <see cref="SentryOptions.ConfigureDataCollection"/> has been called, this
        /// also covers response tool calls and tool call results, and when not set,
        /// <see cref="DataCollectionOptions.GenAIOptions.Outputs"/> decides instead.
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

        internal bool ShouldRecordToolInputs(SentryOptions? options) =>
            options?.DataCollection is null || ShouldRecordInputs(options);

        internal bool ShouldRecordToolOutputs(SentryOptions? options) =>
            options?.DataCollection is null || ShouldRecordOutputs(options);
    }
}
