using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Sentry.Internal.Extensions;

namespace Sentry.Extensibility;

/// <summary>
/// Default factory to <see cref="SentryStackTrace" /> from an <see cref="Exception" />.
/// </summary>
public class SentryStackTraceFactory : ISentryStackTraceFactory
{
    private readonly SentryOptions _options;

    /*
     *  NOTE: While we could improve these regexes, doing so might break exception grouping on the backend.
     *        Specifically, RegexAsyncFunctionName would be better as:  @"^(.*)\+<(\w*|<\w*>b__\d*)>d(?:__\d*)?$"
     *        But we cannot make this change without consequences of ignored events coming back to life in Sentry.
     */

    private static readonly Regex RegexAsyncFunctionName = new(@"^(.*)\+<(\w*)>d__\d*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RegexAnonymousFunction = new(@"^<(\w*)>b__\w+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RegexAsyncReturn = new(@"^(.+`[0-9]+)\[\[",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

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
            _options.LogDebug("No Exception and AttachStacktrace is off. No stack trace will be collected.");
            return null;
        }

        _options.LogDebug("Creating SentryStackTrace. isCurrentStackTrace: {0}.", isCurrentStackTrace);

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

        // Not to throw on code that ignores nullability warnings.
        if (frames.IsNull())
        {
            _options.LogDebug("No stack frames found. AttachStacktrace: '{0}', isCurrentStackTrace: '{1}'",
                _options.AttachStacktrace, isCurrentStackTrace);

            yield break;
        }

        Debug.Assert(frames != null);

        var firstFrame = true;
        foreach (var stackFrame in frames)
        {

#if !NET5_0_OR_GREATER
            if (stackFrame is null)
            {
                continue;
            }
#endif

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
        string? projectPath = null;
        var frame = new SentryStackFrame();
        if (GetMethod(stackFrame) is { } method)
        {
            frame.Module = method.DeclaringType?.FullName ?? unknownRequiredField;
            frame.Package = method.DeclaringType?.Assembly.FullName;

            if (_options.StackTraceMode == StackTraceMode.Enhanced &&
                stackFrame is EnhancedStackFrame enhancedStackFrame)
            {
                var stringBuilder = new StringBuilder();
                frame.Function = enhancedStackFrame.MethodInfo.Append(stringBuilder, false).ToString();

                if (enhancedStackFrame.MethodInfo.DeclaringType is { } declaringType)
                {
                    stringBuilder.Clear();
                    stringBuilder.AppendTypeDisplayName(declaringType);
                    frame.Module = stringBuilder.ToString();
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

            AttributeReader.TryGetProjectDirectory(method.Module.Assembly, out projectPath);
        }

        frame.ConfigureAppFrame(_options);

        var frameFileName = stackFrame.GetFileName();
        if (projectPath != null && frameFileName != null)
        {
            if (frameFileName.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
            {
                frameFileName = frameFileName.Substring(projectPath.Length);
            }
        }

        frame.FileName = frameFileName;

        // stackFrame.HasILOffset() throws NotImplemented on Mono 5.12
        var ilOffset = stackFrame.GetILOffset();
        if (ilOffset != StackFrame.OFFSET_UNKNOWN)
        {
            frame.InstructionOffset = ilOffset;
        }

        var lineNo = stackFrame.GetFileLineNumber();
        if (lineNo > 0)
        {
            frame.LineNumber = lineNo;
        }

        var colNo = stackFrame.GetFileColumnNumber();
        if (lineNo > 0)
        {
            frame.ColumnNumber = colNo;
        }

        if (demangle && _options.StackTraceMode != StackTraceMode.Enhanced)
        {
            DemangleAsyncFunctionName(frame);
            DemangleAnonymousFunction(frame);
            DemangleLambdaReturnType(frame);
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

        var match = RegexAsyncFunctionName.Match(frame.Module);
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

        var match = RegexAnonymousFunction.Match(frame.Function);
        if (match.Success && match.Groups.Count == 2)
        {
            frame.Function = match.Groups[1].Value + " { <lambda> }";
        }
    }

    /// <summary>
    /// Remove return type from module in a Task with a Lambda with a return value.
    /// This was seen in Unity, see https://github.com/getsentry/sentry-unity/issues/845
    /// </summary>
    internal static void DemangleLambdaReturnType(SentryStackFrame frame)
    {
        if (frame.Module == null)
        {
            return;
        }

        // Change:
        //   System.Threading.Tasks.Task`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]] in InnerInvoke
        //   or System.Collections.Generic.List`1[[UnityEngine.Events.PersistentCall, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]] in get_Item
        // to:
        //   System.Threading.Tasks.Task`1 in InnerInvoke`
        //   or System.Collections.Generic.List`1 in get_Item
        var match = RegexAsyncReturn.Match(frame.Module);
        if (match.Success && match.Groups.Count == 2)
        {
            frame.Module = match.Groups[1].Value;
        }
    }
}
