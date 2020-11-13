using System;
using System.Collections.Generic;
using System.Linq;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Internal
{
    /// <summary>
    /// Mono factory to <see cref="SentryStackTrace" /> from an <see cref="Exception" />.
    /// </summary>
    internal class MonoSentryStackTraceFactory : SentryStackTraceFactory
    {
        private readonly SentryOptions _options;

        /// <summary>
        /// Creates an instance of <see cref="MonoSentryStackTraceFactory"/>.
        /// </summary>
        public MonoSentryStackTraceFactory(SentryOptions options) : base(options) => _options = options;

        /// <summary>
        /// Creates a <see cref="SentryStackTrace" /> from the optional <see cref="Exception" />.
        /// </summary>
        /// <param name="exception">The exception to create the stacktrace from.</param>
        /// <returns>A Sentry stack trace.</returns>
        public override SentryStackTrace? Create(Exception? exception = null)
        {
            if (exception == null)
            {
                _options.DiagnosticLogger?.LogDebug("No Exception to collect Mono stack trace.");
                return base.Create(exception);
            }

            List<StackFrameData>? frames = null;
            if (exception.StackTrace is { } stacktrace)
            {
                foreach (var line in stacktrace.Split('\n'))
                {
                    if (StackFrameData.TryParse(line, out var frame))
                    {
                        frames ??= new List<StackFrameData>();
                        frames.Add(frame);
                    }
                }
            }

            if (frames == null)
            {
                _options.DiagnosticLogger?.LogWarning("Couldn't resolve a Mono stacktrace, calling fallback");
                return base.Create(exception);
            }

            return new SentryStackTrace
            {
                // https://develop.sentry.dev/sdk/event-payloads/stacktrace/
                Frames = frames.Select(f => new SentryStackFrame
                {
                    Module = f.TypeFullName,
                    InstructionOffset = f.Offset != 0 ? f.Offset : (long?)null,
                    Function = f.MethodSignature,
                    LineNumber = GetLineNumber(f.Line),
                }).Reverse().ToArray()
            };

            static int? GetLineNumber(string line) =>
                // Protocol is uint. Also, Mono AOT / IL2CPP / no pdb means no line number (0) which isn't useful.
                line is { } l && int.TryParse(l, out var parsedLine) && parsedLine >= 0
                    ? parsedLine
                    : (int?)null;
        }
    }
}
