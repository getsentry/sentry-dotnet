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
        _sentryHub.ConfigureScope(scope => scope.SetTag(_options.ScopeTagName, context.JobDetail.Key.Name));
        await next(context, cancellationToken).ConfigureAwait(false);
    }
}

public class SentryScopeMiddlewareOptions
{
    public string ScopeTagName { get; set; } = "quartz.job";
}
