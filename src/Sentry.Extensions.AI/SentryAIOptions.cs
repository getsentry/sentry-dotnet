namespace Sentry.Extensions.AI;

/// <summary>
/// Sentry AI instrumentation options
/// </summary>
/// <inheritdoc />
public class SentryAIOptions : SentryOptions
{
    /// <summary>
    /// Whether to include LLM request messages in spans.
    /// </summary>
    public bool IncludeAIRequestMessages { get; set; } = true;

    /// <summary>
    /// Whether to include LLM response content in spans.
    /// </summary>
    public bool IncludeAIResponseContent { get; set; } = true;

    /// <summary>
    /// Whether to initialize the Sentry SDK through this integration.
    /// </summary>
    /// <remarks>
    /// If you have already set up Sentry in your application, there is no need to re-initialize the Sentry SDK
    /// </remarks>
    public bool InitializeSdk { get; set; } = false;
}
