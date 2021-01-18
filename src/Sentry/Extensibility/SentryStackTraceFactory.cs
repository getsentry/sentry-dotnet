using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
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

        /// <summary>
        /// Creates an instance of <see cref="SentryStackTraceFactory"/>.
        /// </summary>
        public SentryStackTraceFactory(SentryOptions options) => _options = options;

        /// <summary>
        /// Creates a <see cref="SentryStackTrace" /> from the optional <see cref="Exception" />.
        /// </summary>
        /// <param name="exception">The exception to create the stacktrace from.</param>
        /// <returns>A Sentry stack trace.</returns>
        public virtual SentryStackTrace? Create(Exception? exception = null)
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

        /// <summary>
        /// Creates a s<see cref="StackTrace"/> from the <see cref="Exception"/>.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>A StackTrace.</returns>
        protected virtual StackTrace CreateStackTrace(Exception? exception) =>
            exception is null
                ? new StackTrace(true)
                : new StackTrace(exception, true);

        /// <summary>
        /// Creates a <see cref="SentryStackTrace"/> from the <see cref="StackTrace"/>.
        /// </summary>
        /// <param name="stackTrace">The stack trace.</param>
        /// <param name="isCurrentStackTrace">Whether this is the current stack trace.</param>
        /// <returns>SentryStackTrace</returns>
        internal SentryStackTrace? Create(StackTrace stackTrace, bool isCurrentStackTrace)
        {
            var frames = CreateFrames(stackTrace, isCurrentStackTrace)
                // Sentry expects the frames to be sent in reversed order
                .Reverse();

            var stacktrace = new SentryStackTrace();

            foreach (var frame in frames)
            {
                stacktrace.Frames.Add(frame);
            }

            return stacktrace.Frames.Count != 0
                ? stacktrace
                : null;
        }

        /// <summary>
        /// Creates an enumerator of <see cref="SentryStackFrame"/> from a <see cref="StackTrace"/>.
        /// </summary>
        internal IEnumerable<SentryStackFrame> CreateFrames(StackTrace stackTrace, bool isCurrentStackTrace)
        {
            var frames = _options.StackTraceMode switch
            {
                StackTraceMode.Enhanced => EnhancedStackTrace.GetFrames(stackTrace).Select(p => p as StackFrame),
                _ => stackTrace.GetFrames()
// error CS8619: Nullability of reference types in value of type 'StackFrame?[]' doesn't match target type 'IEnumerable<StackFrame>'.
#if NETCOREAPP3_0
                                            .Where(f => f is not null)
#endif
            };
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse - Backward compatibility
            if (frames is null)
            {
                _options.DiagnosticLogger?.LogDebug("No stack frames found. AttachStacktrace: '{0}', isCurrentStackTrace: '{1}'",
                    _options.AttachStacktrace, isCurrentStackTrace);

                yield break;
            }

            var firstFrame = true;
            foreach (var stackFrame in frames)
            {
                if (stackFrame is null)
                {
                    continue;
                }

                // Remove the frames until the call for capture with the SDK
                if (firstFrame
                    && isCurrentStackTrace
                    && stackFrame.GetMethod() is { } method
                    && method.DeclaringType?.AssemblyQualifiedName?.StartsWith("Sentry") == true)
                {
                    continue;
                }

                firstFrame = false;

                yield return CreateFrame(stackFrame, isCurrentStackTrace);
            }
        }

        internal SentryStackFrame CreateFrame(StackFrame stackFrame) => InternalCreateFrame(stackFrame, true);

        /// <summary>
        /// Create a <see cref="SentryStackFrame"/> from a <see cref="StackFrame"/>.
        /// </summary>
        protected virtual SentryStackFrame CreateFrame(StackFrame stackFrame, bool isCurrentStackTrace) => InternalCreateFrame(stackFrame, true);

        /// <summary>
        /// Default the implementation of CreateFrame.
        /// </summary>
        protected SentryStackFrame InternalCreateFrame(StackFrame stackFrame, bool demangle)
        {
            const string unknownRequiredField = "(unknown)";

            var frame = new SentryStackFrame();
            if (GetMethod(stackFrame) is { } method)
            {
                frame.Module = method.DeclaringType?.FullName ?? unknownRequiredField;
                frame.Package = method.DeclaringType?.Assembly.FullName;

                if (_options.StackTraceMode == StackTraceMode.Enhanced && stackFrame is EnhancedStackFrame enhancedStackFrame)
                {
                    var sb = new StringBuilder();
                    frame.Function = enhancedStackFrame.MethodInfo.Append(sb, false).ToString();

                    if (enhancedStackFrame.MethodInfo.DeclaringType is { } declaringType)
                    {
                        sb.Clear();
                        sb.AppendTypeDisplayName(declaringType);
                        frame.Module = sb.ToString();
                    }
                }
                else
                {
                    frame.Function = method.Name;
                }

                // Originally we didn't skip methods from dynamic assemblies, so not to break compatibility:
                if (_options.StackTraceMode != StackTraceMode.Original && method.Module.Assembly.IsDynamic)
                {
                    frame.InApp = false;
                }
            }

            frame.InApp ??= !IsSystemModuleName(frame.Module);

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

            if (demangle && _options.StackTraceMode != StackTraceMode.Enhanced)
            {
                DemangleAsyncFunctionName(frame);
                DemangleAnonymousFunction(frame);
            }

            if (_options.StackTraceMode == StackTraceMode.Enhanced)
            {
                // In Enhanced mode, Module (which in this case is the Namespace)
                // is already prepended to the function, after return type.
                // Removing here at the end because this is used to resolve InApp=true/false
                frame.Module = null;
            }

            return frame;
        }

        /// <summary>
        /// Get a <see cref="MethodBase"/> from <see cref="StackFrame"/>.
        /// </summary>
        /// <param name="stackFrame">The <see cref="StackFrame"/></param>.
        protected virtual MethodBase? GetMethod(StackFrame stackFrame)
            => stackFrame.GetMethod();

        private bool IsSystemModuleName(string? moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
            {
                return false;
            }

            return _options.InAppInclude?.Any(include => moduleName.StartsWith(include, StringComparison.Ordinal)) != true &&
                   _options.InAppExclude?.Any(exclude => moduleName.StartsWith(exclude, StringComparison.Ordinal)) == true;
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
            if (frame.Function == null)
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
