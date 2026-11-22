namespace Sentry.Internal;

/// <summary>
/// Warns, at most once, that a logging integration is dropping events because Sentry was never initialised
/// even though a DSN can be found. There is no diagnostic logger in that state, so the warning goes to the
/// logging framework's own diagnostics channel and to standard error.
/// </summary>
internal sealed class UninitializedSdkWarning
{
    private readonly Action<string>? _writeToIntegrationLog;
    private int _warned;

    public UninitializedSdkWarning(Action<string>? writeToIntegrationLog = null)
        => _writeToIntegrationLog = writeToIntegrationLog;

    public Func<string?> DsnLocator { get; set; } = static () => new SentryOptions().SettingLocator.TryGetDsn();

    public Action<string> WriteToStandardError { get; set; } = Console.Error.WriteLine;

    public void WarnOnce(string message)
    {
        if (Interlocked.Exchange(ref _warned, 1) != 0)
        {
            return;
        }

        if (DsnLocator() is null)
        {
            return;
        }

        _writeToIntegrationLog?.Invoke(message);
        WriteToStandardError(message);
    }
}
