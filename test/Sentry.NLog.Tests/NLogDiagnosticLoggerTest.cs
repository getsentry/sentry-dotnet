using System;
using NLog;
using NLog.Common;
using Xunit;

namespace Sentry.NLog.Tests
{
    public class NLogDiagnosticLoggerTest
    {
        [Fact]
        public void NLogDiagnosticLogger_Levels()
        {
            var logger = new NLogDiagnosticLogger();
            try
            {
                InternalLogger.LogLevel = LogLevel.Debug;
                foreach (SentryLevel level in Enum.GetValues(typeof(SentryLevel)))
                {
                    var logWriter = new System.IO.StringWriter();
                    InternalLogger.LogWriter = logWriter;
                    if (logger.IsEnabled(level))
                    {
                        logger.Log(level, level.ToString());
                    }
                    Assert.Contains(level.ToString(), logWriter.ToString());
                }
            }
            finally
            {
                InternalLogger.LogWriter = null;
                InternalLogger.LogLevel = LogLevel.Off;
            }
        }
    }
}
