using System;
using Sentry.Extensibility;
using Xunit.Abstractions;

namespace Sentry.Testing
{
    public class TestOutputDiagnosticLogger : IDiagnosticLogger
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly SentryLevel _minimumLevel;

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

            _testOutputHelper.WriteLine($@"
[{logLevel}]: {formattedMessage}
    Exception: {exception?.ToString() ?? "<none>"}
".Trim());
        }
    }
}
