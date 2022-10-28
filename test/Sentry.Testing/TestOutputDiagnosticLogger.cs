using System.Diagnostics;
using System.Reflection;
using Xunit.Sdk;

namespace Sentry.Testing;

public class TestOutputDiagnosticLogger : IDiagnosticLogger
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly SentryLevel _minimumLevel;
    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly string _testName;

    private static readonly FieldInfo TestFieldInfo = typeof(TestOutputHelper)
        .GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);

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

    public TestOutputDiagnosticLogger(ITestOutputHelper testOutputHelper)
        : this(testOutputHelper, SentryLevel.Debug)
    {
    }

    public TestOutputDiagnosticLogger(ITestOutputHelper testOutputHelper, SentryLevel minimumLevel)
    {
        _testOutputHelper = testOutputHelper;
        _minimumLevel = minimumLevel;

        var test = TestFieldInfo.GetValue(_testOutputHelper) as ITest;
        _testName = test?.DisplayName;
    }

    public bool IsEnabled(SentryLevel level) => level >= _minimumLevel;

    // Note: Log must be declared virtual so we can use it with NSubstitute spies.
    public virtual void Log(SentryLevel logLevel, string message, Exception exception = null, params object[] args)
    {
        // Important: Only format the string if there are args passed.
        // Otherwise, a pre-formatted string that contains braces can cause a FormatException.
        var formattedMessage = args.Length == 0 ? message : string.Format(message, args);

        var entry = new LogEntry
        {
            Level = logLevel,
            Message = formattedMessage,
            RawMessage = message,
            Exception = exception
        };
        _entries.Enqueue(entry);

        var msg = $@"[{logLevel} {_stopwatch.Elapsed:hh\:mm\:ss\.ff}]: {formattedMessage}".Trim();

        if (exception != null)
        {
            msg += $"{Environment.NewLine}    Exception: {exception}";
        }

        try
        {
            _testOutputHelper.WriteLine(msg);
        }
        catch (InvalidOperationException ex)
        {
            // Handle "System.InvalidOperationException: There is no currently active test."
            Console.Error.WriteLine(
                $"Error: {ex.Message}{Environment.NewLine}" +
                $"    Test: {_testName}{Environment.NewLine}" +
                $"    Message: {msg}");
        }
    }
}
