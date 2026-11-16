using Microsoft.Extensions.AI;
using Sentry.Internal;

namespace Sentry.Extensions.AI;

internal sealed class SentryInstrumentedFunction(AIFunction innerFunction, IHub? hub = null)
    : DelegatingAIFunction(innerFunction)
{
    private static readonly SentryAIOptions DefaultAIOptions = new();

    private readonly IHub _hub = hub ?? HubAdapter.Instance;

    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        var ficcActivity = SentryAIUtil.GetFICCActivity();
        var agentSpan = ficcActivity?.GetFICCSpan();
        var aiOptions = ficcActivity?.GetAIOptions() ?? DefaultAIOptions;
        var sentryOptions = _hub.GetSentryOptions();

        var toolSpan = InitToolSpan(agentSpan, arguments, aiOptions.Experimental.ShouldRecordInputs(sentryOptions));
        try
        {
            var result = await base.InvokeCoreAsync(arguments, cancellationToken).ConfigureAwait(false);

            if (aiOptions.Experimental.ShouldRecordOutputs(sentryOptions) && result?.ToString() is { } resultString)
            {
                toolSpan.SetData(SentryAIConstants.SpanAttributes.ToolOutput, resultString);
            }

            toolSpan.Finish(SpanStatus.Ok);
            return result;
        }
        catch (Exception ex)
        {
            toolSpan.Finish(SpanStatus.InternalError);
            _hub.CaptureException(ex);
            if (agentSpan != null)
            {
                // We don't finish the agent span with the exception because there will be another call to LLM.
                // Python SDK currently binds the exception to the agent span, so we do so here.
                _hub.BindException(ex, agentSpan);
            }
            throw;
        }
    }

    private ISpan InitToolSpan(ISpan? agentSpan, AIFunctionArguments arguments, bool recordInputs)
    {
        var spanName = $"execute_tool {Name}";

        // If the user correctly follows the instructions, we should be able to get the agent span
        var currSpan = agentSpan != null
            ? agentSpan.StartChild(SentryAIConstants.SpanAttributes.ToolCallOperation, spanName)
            // If we couldn't find the agent span, just attach it to the hub's current scope
            : _hub.StartSpan(SentryAIConstants.SpanAttributes.ToolCallOperation, spanName);

        currSpan.SetData(SentryAIConstants.SpanAttributes.OperationName, "execute_tool");
        currSpan.SetData(SentryAIConstants.SpanAttributes.ToolName, Name);

        if (recordInputs)
        {
            currSpan.SetData(SentryAIConstants.SpanAttributes.ToolDescription, Description);
            currSpan.SetData(SentryAIConstants.SpanAttributes.ToolInput, arguments);
        }

        return currSpan;
    }
}
