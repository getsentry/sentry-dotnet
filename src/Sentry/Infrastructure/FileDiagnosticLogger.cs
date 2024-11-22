namespace Sentry.Infrastructure;

/// <summary>
/// File logger used by the SDK to report its internal logging to a file.
/// </summary>
/// <remarks>
/// Primarily used to capture debug information to troubleshoot the SDK itself.
/// </remarks>
public class FileDiagnosticLogger : DiagnosticLogger
{
    private readonly bool _alsoWriteToConsole;
    private readonly StreamWriter _writer;

    /// <summary>
    /// Creates a new instance of <see cref="FileDiagnosticLogger"/>.
    /// </summary>
    /// <param name="path">The path to the file to write logs to.  Will overwrite any existing file.</param>
    /// <param name="alsoWriteToConsole">If <c>true</c>, will write to the console as well as the file.</param>
    public FileDiagnosticLogger(string path, bool alsoWriteToConsole = false)
        : this(path, SentryLevel.Debug, alsoWriteToConsole)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="FileDiagnosticLogger"/>.
    /// </summary>
    /// <param name="path">The path to the file to write logs to.  Will overwrite any existing file.</param>
    /// <param name="minimalLevel">The minimal level to log.</param>
    /// <param name="alsoWriteToConsole">If <c>true</c>, will write to the console as well as the file.</param>
    public FileDiagnosticLogger(string path, SentryLevel minimalLevel, bool alsoWriteToConsole = false)
        : base(minimalLevel)
    {
        // Allow direct file system usage
#pragma warning disable SN0001
        var stream = File.OpenWrite(path);
#pragma warning restore SN0001
        _writer = new StreamWriter(stream);
        _alsoWriteToConsole = alsoWriteToConsole;

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            _writer.Flush();
            _writer.Dispose();
        };
    }

    /// <inheritdoc />
    protected override void LogMessage(string message)
    {
        _writer.WriteLine(message);

        if (_alsoWriteToConsole)
        {
            Console.WriteLine(message);
        }
    }
}
