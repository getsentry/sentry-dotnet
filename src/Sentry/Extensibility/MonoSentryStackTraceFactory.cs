using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry.Extensibility
{
    /// <summary>
    /// Mono factory to <see cref="SentryStackTrace" /> from an <see cref="Exception" />.
    /// </summary>
    public class MonoSentryStackTraceFactory : SentryStackTraceFactory
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
            if (exception.StackTrace is string stacktrace)
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
                    InstructionOffset = f.Offset,
                    Platform = "mono",
                    Function = f.MethodSignature,
                    LineNumber = GetLine(f.Line),
                    ContextLine = $"MVID:{f.Mvid}-AOTID:{f.Aotid}-MethodIndex:0x{f.MethodIndex:x4}-IsILOffset:{f.IsILOffset}-Offset:{f.Offset}-Line:{f.Line}",
                    // The following properties are not supported by the server and get dropped.
                    // For that reason, misusing the ContextLine to transport the data to Sentry
                    ModuleVersionId = f.Mvid,
                    AotId = f.Aotid,
                    MethodIndex = f.MethodIndex.ToString("x4"),
                    IsILOffset = f.IsILOffset,
                }).Reverse().ToArray()
            };

            static int? GetLine(string line) =>
                line is string l && int.TryParse(l, out var parsedLine)
                    ? parsedLine
                    : (int?)null;
        }
    }
}
