using Microsoft.Extensions.Options;
using Quartz;

namespace Sentry.Quartz;

internal sealed class SentryScopeMiddleware : IJobExecutionMiddleware
{
    private readonly IHub _sentryHub;
    private readonly SentryScopeMiddlewareOptions _options;

    public SentryScopeMiddleware(IHub sentryHub, IOptions<SentryScopeMiddlewareOptions> options)
    {
        _sentryHub = sentryHub;
        _options = options.Value;
    }

    public async ValueTask Invoke(IJobExecutionContext context, JobExecutionDelegate next, CancellationToken cancellationToken)
    {
        using var _ = _sentryHub.PushScope();
        _sentryHub.ConfigureScope(scope => scope.SetTag(_options.ScopeTagName, context.JobDetail.Key.ToString()));
        await next(context, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Options for configuring the behavior of the SentryScopeMiddleware.
/// </summary>
public class SentryScopeMiddlewareOptions
{
    /// <summary>
    /// Gets or sets the name of the tag that will be added to the Sentry scope.
    /// </summary>
    /// <remarks>
    /// The value of this property is used as the key for a tag in the Sentry scope
    /// to allow associating metadata about the current Quartz job execution context.
    /// By default, it is set to "quartz.job".
    /// </remarks>
    public string ScopeTagName { get; set; } = "quartz.job";
}
