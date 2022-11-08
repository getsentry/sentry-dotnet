using System.Diagnostics;
using Trace = System.Diagnostics.Trace;

namespace Sentry.Tests.Infrastructure;

public class TraceDiagnosticLoggerTests
{
    [Fact]
    public void CanTrace()
    {
        var listener = new Listener();
        Trace.Listeners.Add(listener);
        try
        {
            var logger = new TraceDiagnosticLogger(SentryLevel.Debug);
            logger.Log(SentryLevel.Debug, "the message {0}", new Exception("the exception"), "arg1");
            Trace.Flush();
            Assert.Equal($"  Debug: the message arg1{Environment.NewLine}System.Exception: the exception",
                listener.Messages.Single());
        }
        finally
        {
            Trace.Listeners.Remove(listener);
        }
    }

    private class Listener : TraceListener
    {
        public List<string> Messages = new();

        public override void Write(string message)
        {
            Messages.Add(message);
        }

        public override void WriteLine(string message)
        {
            Messages.Add(message);
        }
    }
}
