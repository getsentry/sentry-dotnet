using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Sentry.Protocol;

namespace Sentry.Extensibility
{
    /// <summary>
    /// Default factory to <see cref="SentryStackTrace" /> from an <see cref="Exception" />.
    /// </summary>
    public class SentryStackTraceFactory : ISentryStackTraceFactory
    {
        private readonly SentryOptions _options;

        public SentryStackTraceFactory(SentryOptions options) => _options = options;

        /// <summary>
        /// Creates a <see cref="SentryStackTrace" /> from the optional <see cref="Exception" />.
        /// </summary>
        /// <param name="exception">The exception to create the stacktrace from.</param>
        /// <returns>A Sentry stack trace.</returns>
        public SentryStackTrace Create(Exception exception = null)
        {
            var isCurrentStackTrace = exception == null && _options.AttachStacktrace;

            if (exception == null && !isCurrentStackTrace)
            {
                _options.DiagnosticLogger?.LogDebug("No Exception and AttachStacktrace is off. No stack trace will be collected.");
                return null;
            }

            _options.DiagnosticLogger?.LogDebug("Creating SentryStackTrace. isCurrentStackTrace: {0}.", isCurrentStackTrace);

            return Create(CreateStackTrace(exception), isCurrentStackTrace);
        }

        protected virtual StackTrace CreateStackTrace(Exception exception) =>
            exception == null ? new StackTrace(true) : new StackTrace(exception, true);

        internal SentryStackTrace Create(StackTrace stackTrace, bool isCurrentStackTrace)
        {
            var frames = CreateFrames(stackTrace, isCurrentStackTrace)
                // Sentry expects the frames to be sent in reversed order
                .Reverse();

            var stacktrace = new SentryStackTrace();

            foreach (var frame in frames)
            {
                stacktrace.Frames.Add(frame);
            }

            return stacktrace.Frames.Count == 0
                ? null
                : stacktrace;
        }

        internal IEnumerable<SentryStackFrame> CreateFrames(StackTrace stackTrace, bool isCurrentStackTrace)
        {
            var frames = stackTrace?.GetFrames();
            if (frames == null)
            {
                _options.DiagnosticLogger?.LogDebug("No stack frames found. AttachStacktrace: '{0}', isCurrentStackTrace: '{1}'",
                    _options.AttachStacktrace, isCurrentStackTrace);

                yield break;
            }

            var firstFrames = true;
            foreach (var stackFrame in frames)
            {
                // Remove the frames until the call for capture with the SDK
                if (firstFrames
                    && isCurrentStackTrace
                    && stackFrame.GetMethod() is MethodBase method
                    && method.DeclaringType?.AssemblyQualifiedName?.StartsWith("Sentry") == true)
                {
                    continue;
                }

                firstFrames = false;

                var frame = CreateFrame(stackFrame, isCurrentStackTrace);
                if (frame != null)
                {
                    yield return frame;
                }
            }
        }

        internal SentryStackFrame CreateFrame(StackFrame stackFrame) => InternalCreateFrame(stackFrame, true);

        protected virtual SentryStackFrame CreateFrame(StackFrame stackFrame, bool isCurrentStackTrace) => InternalCreateFrame(stackFrame, true);

        protected SentryStackFrame InternalCreateFrame(StackFrame stackFrame, bool demangle)
        {
            const string unknownRequiredField = "(unknown)";
            var frame = new SentryStackFrame();
            if (GetMethod(stackFrame) is MethodBase method)
            {
                // TODO: SentryStackFrame.TryParse and skip frame instead of these unknown values:
                frame.Module = method.DeclaringType?.FullName ?? unknownRequiredField;
                frame.Package = method.DeclaringType?.Assembly.FullName;
                frame.Function = method.Name;
            }

            frame.InApp = !IsSystemModuleName(frame.Module);
            frame.FileName = stackFrame.GetFileName();

            // stackFrame.HasILOffset() throws NotImplemented on Mono 5.12
            var ilOffset = stackFrame.GetILOffset();
            if (ilOffset != 0)
            {
                frame.InstructionOffset = stackFrame.GetILOffset();
            }

            var lineNo = stackFrame.GetFileLineNumber();
            if (lineNo != 0)
            {
                frame.LineNumber = lineNo;
            }

            var colNo = stackFrame.GetFileColumnNumber();
            if (lineNo != 0)
            {
                frame.ColumnNumber = colNo;
            }

            if (demangle)
            {
                DemangleAsyncFunctionName(frame);
                DemangleAnonymousFunction(frame);
            }

            return frame;
        }

        protected virtual MethodBase GetMethod(StackFrame stackFrame) => stackFrame.GetMethod();

        private bool IsSystemModuleName(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
            {
                return false;
            }

            foreach (var include in _options.InAppInclude)
            {
                if (moduleName.StartsWith(include, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            foreach (var exclude in _options.InAppExclude)
            {
                if (moduleName.StartsWith(exclude, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Clean up function and module names produced from `async` state machine calls.
        /// </summary>
        /// <para>
        /// When the Microsoft cs.exe compiler compiles some modern C# features,
        /// such as async/await calls, it can create synthetic function names that
        /// do not match the function names in the original source code. Here we
        /// reverse some of these transformations, so that the function and module
        /// names that appears in the Sentry UI will match the function and module
        /// names in the original source-code.
        /// </para>
        private static void DemangleAsyncFunctionName(SentryStackFrame frame)
        {
            if (frame.Module == null || frame.Function != "MoveNext")
            {
                return;
            }

            //  Search for the function name in angle brackets followed by d__<digits>.
            //
            // Change:
            //   RemotePrinterService+<UpdateNotification>d__24 in MoveNext at line 457:13
            // to:
            //   RemotePrinterService in UpdateNotification at line 457:13

            var match = Regex.Match(frame.Module, @"^(.*)\+<(\w*)>d__\d*$");
            if (match.Success && match.Groups.Count == 3)
            {
                frame.Module = match.Groups[1].Value;
                frame.Function = match.Groups[2].Value;
            }
        }

        /// <summary>
        /// Clean up function names for anonymous lambda calls.
        /// </summary>
        internal static void DemangleAnonymousFunction(SentryStackFrame frame)
        {
            if (frame?.Function == null)
            {
                return;
            }

            // Search for the function name in angle brackets followed by b__<digits/letters>.
            //
            // Change:
            //   <BeginInvokeAsynchronousActionMethod>b__36
            // to:
            //   BeginInvokeAsynchronousActionMethod { <lambda> }

            var match = Regex.Match(frame.Function, @"^<(\w*)>b__\w+$");
            if (match.Success && match.Groups.Count == 2)
            {
                frame.Function = match.Groups[1].Value + " { <lambda> }";
            }
        }
    }
}
