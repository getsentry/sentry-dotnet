using System;
using System.Collections.Generic;
using System.Text;
using Serilog;
using Serilog.Configuration;

namespace Sentry.Serilog
{
    public static class SentrySinkExtensions
    {
        public static LoggerConfiguration Sentry(
            this LoggerSinkConfiguration loggerConfiguration,
            IFormatProvider formatProvider = null)
        {
            return loggerConfiguration.Sink(new SentrySink(formatProvider));
        }
    }
}
