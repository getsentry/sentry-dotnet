using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry.Integrations;

internal class UnobservedTaskExceptionIntegration : ISdkIntegration
{
    internal const string MechanismKey = "UnobservedTaskException";

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

        // System.Net.Quic is leaking UnobservedTaskExceptions
        // See: https://github.com/dotnet/runtime/issues/80111 . That will be fix maybe in net9.0
#if NET7_0_OR_GREATER
        if (ex.InnerExceptions.All(static exception => exception is System.Net.Quic.QuicException))
        {
            return;
        }
#endif

        ex.SetSentryMechanism(
            MechanismKey,
            "This exception was thrown from a task that was unobserved, such as from an async void method, or " +
            "a Task.Run that was not awaited. This exception was unhandled, but likely did not crash the application.",
            handled: false);

        // Call the internal implementation, so that we still capture even if the hub has been disabled.
        _hub.CaptureExceptionInternal(ex);
    }
}
