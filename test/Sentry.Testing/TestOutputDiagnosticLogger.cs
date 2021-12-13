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
    }

    public TestOutputDiagnosticLogger(
        ITestOutputHelper testOutputHelper,
        SentryLevel minimumLevel = SentryLevel.Debug)
    {
        _testOutputHelper = testOutputHelper;
        _minimumLevel = minimumLevel;
    }

    public bool IsEnabled(SentryLevel level) => level >= _minimumLevel;

    public void Log(SentryLevel logLevel, string message, Exception exception = null, params object[] args)
    {
        var formattedMessage = string.Format(message, args);
        _entries.Enqueue(
            new LogEntry { Level = logLevel, Message = formattedMessage, Exception = exception });

        _testOutputHelper.WriteLine($@"
[{logLevel}]: {formattedMessage}
    Exception: {exception?.ToString() ?? "<none>"}
".Trim());
    }
}
