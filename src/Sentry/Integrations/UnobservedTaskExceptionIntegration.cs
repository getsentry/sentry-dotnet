using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry.Integrations;

internal class UnobservedTaskExceptionIntegration : ISdkIntegration
{
    private readonly IAppDomain _appDomain;
    private IHub _hub = null!;

    internal UnobservedTaskExceptionIntegration(IAppDomain? appDomain = null)
        => _appDomain = appDomain ?? AppDomainAdapter.Instance;

    public void Register(IHub hub, SentryOptions _)
    {
        _hub = hub;
        _appDomain.UnobservedTaskException += Handle;
    }

#if !NET6_0_OR_GREATER
    [HandleProcessCorruptedStateExceptions]
#endif
    [SecurityCritical]
    internal void Handle(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // The exception will never be null in any runtime.
        // The annotation was corrected in .NET 5
        // See: https://github.com/dotnet/runtime/issues/32454

#if NET5_0_OR_GREATER
        var ex = e.Exception;
#else
            var ex = e.Exception!;
#endif
        ex.Data[Mechanism.HandledKey] = false;
        ex.Data[Mechanism.MechanismKey] = "UnobservedTaskException";
        _hub.CaptureException(ex);
    }
}