#nullable enable
using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.Maui.TestUtils.DeviceTests.Runners.VisualRunner;

internal class TestRunLogger
{
    private static readonly char[] NewLineCharacters = { '\r', '\n' };
    private readonly object _locker = new();
    private readonly ILogger _logger;
    private readonly StringBuilder _builder;
    private int _failed;
    private int _passed;
    private int _skipped;

    public TestRunLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _builder = new StringBuilder();
    }

    public void LogTestResult(TestResultViewModel result)
    {
        lock (_locker)
        {
            _builder.Clear();

            if (result.TestCase.Result == TestState.Passed)
            {
                _builder.Append("\t[PASS] ");
                _passed++;
            }
            else if (result.TestCase.Result == TestState.Skipped)
            {
                _builder.Append("\t[SKIPPED] ");
                _skipped++;
            }
            else if (result.TestCase.Result == TestState.Failed)
            {
                _builder.Append("\t[FAIL] ");
                _failed++;
            }
            else
            {
                _builder.Append("\t[INFO] ");
            }
            _builder.Append(result.TestCase.DisplayName);

            var message = result.ErrorMessage;
            if (!string.IsNullOrEmpty(message))
            {
                _builder.Append(" : ");
                _builder.Append(message.Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal));
            }

            _builder.AppendLine();

            var stacktrace = result.ErrorStackTrace;
            if (!string.IsNullOrEmpty(stacktrace))
            {
                var lines = stacktrace.Split(NewLineCharacters, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    _builder.Append("\t\t");
                    _builder.AppendLine(line);
                }
            }

            _logger.LogInformation(_builder.ToString());
        }
    }

    public void LogTestStart(string? message = null)
    {
        lock (_locker)
        {
            _failed = _passed = _skipped = 0;

            if (string.IsNullOrEmpty(message))
                _logger.LogInformation("[Runner executing]");
            else
                _logger.LogInformation("[Runner executing: {0}]", message);
        }
    }

    public void LogTestComplete()
    {
        lock (_locker)
        {
            var total = _passed + _failed; // ignored are *not* run

            _logger.LogInformation("Tests run: {0} Passed: {1} Failed: {2} Skipped: {3}", total, _passed, _failed, _skipped);
        }
    }
}
