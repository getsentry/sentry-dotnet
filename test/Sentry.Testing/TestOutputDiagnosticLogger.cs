using System.Collections.Concurrent;

namespace Sentry.Testing;

public class TestOutputDiagnosticLogger : IDiagnosticLogger
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly SentryLevel _minimumLevel;
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IEnumerable<LogEntry> Entries => _entries;

    public bool HasErrorOrFatal => _entries
        .Any(x => x.Level is SentryLevel.Error or SentryLevel.Fatal);

    public class LogEntry
    {
        public SentryLevel Level { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public string RawMessage { get; set; }
    }

    public TestOutputDiagnosticLogger(
        ITestOutputHelper testOutputHelper,
        SentryLevel minimumLevel = SentryLevel.Debug)
    {
        _testOutputHelper = testOutputHelper;
        _minimumLevel = minimumLevel;
    }

    public bool IsEnabled(SentryLevel level) => level >= _minimumLevel;

    // Note: Log must be declared virtual so we can use it with NSubstitute spies.
    public virtual void Log(SentryLevel logLevel, string message, Exception exception = null, params object[] args)
    {
        var formattedMessage = string.Format(message, args);
        var entry = new LogEntry
        {
            Level = logLevel,
            Message = formattedMessage,
            RawMessage = message,
            Exception = exception
        };
        _entries.Enqueue(entry);

        if (exception == null)
        {
            _testOutputHelper.WriteLine($@"
[{logLevel}]: {formattedMessage}
".Trim());
        }
        else
        {
            _testOutputHelper.WriteLine($@"
[{logLevel}]: {formattedMessage}
    Exception: {exception}
".Trim());
        }


    }
}
