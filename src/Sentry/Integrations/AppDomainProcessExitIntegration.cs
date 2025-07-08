using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Integrations;

internal class AppDomainProcessExitIntegration : ISdkIntegration, IDisposable
{
    private readonly IAppDomain _appDomain;
    private IHub? _hub;
    private SentryOptions? _options;

    public AppDomainProcessExitIntegration(IAppDomain? appDomain = null)
        => _appDomain = appDomain ?? AppDomainAdapter.Instance;

    public void Register(IHub hub, SentryOptions options)
    {
        _hub = hub;
        _options = options;
        _appDomain.ProcessExit += HandleProcessExit;
    }

    internal void HandleProcessExit(object? sender, EventArgs e)
    {
        _options?.LogInfo("AppDomain process exited: Disposing SDK.");
        (_hub as IDisposable)?.Dispose();
    }

    public void Dispose()
    {
        _appDomain.ProcessExit -= HandleProcessExit;
    }
}
