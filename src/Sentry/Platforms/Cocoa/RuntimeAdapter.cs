using ObjCRuntime;

namespace Sentry.Cocoa;

internal interface IRuntime
{
    internal event MarshalManagedExceptionHandler MarshalManagedException;
    internal event MarshalObjectiveCExceptionHandler MarshalObjectiveCException;
    bool IsMono { get; }
    void IgnoreNextSignal(int signal);
}

internal sealed class RuntimeAdapter : IRuntime
{
    public static RuntimeAdapter Instance { get; } = new();

    private RuntimeAdapter()
    {
        Runtime.MarshalManagedException += OnMarshalManagedException;
        Runtime.MarshalObjectiveCException += OnMarshalObjectiveCException;
    }

    public event MarshalManagedExceptionHandler? MarshalManagedException;
    public event MarshalObjectiveCExceptionHandler? MarshalObjectiveCException;

    public bool IsMono { get; } = Type.GetType("Mono.Runtime") != null;

    public void IgnoreNextSignal(int signal) => SentryCocoaHybridSdk.IgnoreNextSignal(signal);

    [SecurityCritical]
    private void OnMarshalManagedException(object sender, MarshalManagedExceptionEventArgs e) => MarshalManagedException?.Invoke(this, e);

    [SecurityCritical]
    private void OnMarshalObjectiveCException(object sender, MarshalObjectiveCExceptionEventArgs e) => MarshalObjectiveCException?.Invoke(this, e);
}
