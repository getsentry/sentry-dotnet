using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Profiling;

/// <summary>
/// Build a SampleProfile from TraceEvent data.
/// </summary>
internal class SampleProfileBuilder
{
    private readonly SentryOptions _options;
    private readonly TraceLog _traceLog;

    // Output profile being built.
    public readonly SampleProfile Profile = new();

    // A sparse array that maps from CodeAddressIndex to an index in the output Profile.frames.
    private readonly Dictionary<int, int> _frameIndexesByCodeAddressIndex = new();

    // A sparse array that maps from MethodIndex to an index in the output Profile.frames.
    // This deduplicates frames that map to the same method but have a different CodeAddressIndex.
    private readonly Dictionary<int, int> _frameIndexesByMethodIndex = new();

    // A dictionary from a CallStackIndex to an index in the output Profile.stacks.
    private readonly Dictionary<int, int> _stackIndexes = new();

    // A sparse array mapping from a ThreadIndex to an index in Profile.Threads.
    private readonly Dictionary<int, int> _threadIndexes = new();

    // TODO make downsampling conditional once this is available: https://github.com/dotnet/runtime/issues/82939
    private readonly Downsampler _downsampler = new();

    public SampleProfileBuilder(SentryOptions options, TraceLog traceLog)
    {
        _options = options;
        _traceLog = traceLog;
    }

    internal void AddSample(TraceEvent data, double timestampMs)
    {
        var thread = data.Thread();
        if (thread is null || thread.ThreadIndex == ThreadIndex.Invalid)
        {
            _options.DiagnosticLogger?.LogDebug("Encountered a Profiler Sample without a correct thread. Skipping.");
            return;
        }

        var callStackIndex = data.CallStackIndex();
        if (callStackIndex == CallStackIndex.Invalid)
        {
            _options.DiagnosticLogger?.LogDebug("Encountered a Profiler Sample without an associated call stack. Skipping.");
            return;
        }

        var threadIndex = AddThread(thread);
        if (threadIndex < 0)
        {
            _options.DiagnosticLogger?.LogDebug("Profiler Sample threadIndex is invalid. Skipping.");
            return;
        }

        if (!_downsampler.ShouldSample(threadIndex, timestampMs))
        {
            return;
        }

        var stackIndex = AddStackTrace(callStackIndex);
        if (stackIndex < 0)
        {
            _options.DiagnosticLogger?.LogDebug("Invalid stackIndex for Profiler Sample. Skipping.");
            return;
        }

        Profile.Samples.Add(new()
        {
            Timestamp = (ulong)(timestampMs * 1_000_000),
            StackId = stackIndex,
            ThreadId = threadIndex
        });
    }

    /// <summary>
    /// Adds stack trace and frames, if missing.
    /// </summary>
    /// <returns>The index into the Profile's stacks list</returns>
    private int AddStackTrace(CallStackIndex callstackIndex)
    {
        var key = (int)callstackIndex;

        if (!_stackIndexes.ContainsKey(key))
        {
            Profile.Stacks.Add(CreateStackTrace(callstackIndex));
            _stackIndexes[key] = Profile.Stacks.Count - 1;
        }

        return _stackIndexes[key];
    }

    private Internal.GrowableArray<int> CreateStackTrace(CallStackIndex callstackIndex)
    {
        var stackTrace = new Internal.GrowableArray<int>(10);
        while (callstackIndex != CallStackIndex.Invalid)
        {
            var codeAddressIndex = _traceLog.CallStacks.CodeAddressIndex(callstackIndex);
            if (codeAddressIndex != CodeAddressIndex.Invalid)
            {
                stackTrace.Add(AddStackFrame(codeAddressIndex));
                callstackIndex = _traceLog.CallStacks.Caller(callstackIndex);
            }
            else
            {
                // No need to traverse further up the stack when we're on the thread/process.
                break;
            }
        }

        stackTrace.Trim(10);
        return stackTrace;
    }

    private int PushNewFrame(SentryStackFrame frame)
    {
        Profile.Frames.Add(frame);
        return Profile.Frames.Count - 1;
    }

    /// <summary>
    /// Check if the frame is already stored in the output Profile, or adds it.
    /// </summary>
    /// <returns>The index to the output Profile frames array.</returns>
    private int AddStackFrame(CodeAddressIndex codeAddressIndex)
    {
        if (_frameIndexesByCodeAddressIndex.TryGetValue((int)codeAddressIndex, out var value))
        {
            return value;
        }

        var methodIndex = _traceLog.CodeAddresses.MethodIndex(codeAddressIndex);
        if (methodIndex != MethodIndex.Invalid)
        {
            value = AddStackFrame(methodIndex);
            _frameIndexesByCodeAddressIndex[(int)codeAddressIndex] = value;
            return value;
        }

        // Fall back if the method info is unknown, see more info on Symbol resolution in
        // https://github.com/getsentry/perfview/blob/031250ffb4f9fcadb9263525d6c9f274be19ca51/src/PerfView/SupportFiles/UsersGuide.htm#L7745-L7784
        if (_traceLog.CodeAddresses[codeAddressIndex] is { } codeAddressInfo)
        {
            var frame = new SentryStackFrame
            {
                InstructionAddress = (long?)codeAddressInfo.Address,
                Module = codeAddressInfo.ModuleFile?.Name,
            };
            frame.ConfigureAppFrame(_options);

            return _frameIndexesByCodeAddressIndex[(int)codeAddressIndex] = PushNewFrame(frame);
        }

        // If all else fails, it's a completely unknown frame.
        // TODO check this - maybe we would be able to resolve it later in the future?
        return PushNewFrame(new SentryStackFrame { InApp = false });
    }

    /// <summary>
    /// Check if the frame is already stored in the output Profile, or adds it.
    /// </summary>
    /// <returns>The index to the output Profile frames array.</returns>
    private int AddStackFrame(MethodIndex methodIndex)
    {
        if (_frameIndexesByMethodIndex.TryGetValue((int)methodIndex, out var value))
        {
            return value;
        }

        var method = _traceLog.CodeAddresses.Methods[methodIndex];

        var frame = new SentryStackFrame
        {
            Function = method.FullMethodName,
            Module = method.MethodModuleFile?.Name
        };
        frame.ConfigureAppFrame(_options);

        return _frameIndexesByMethodIndex[(int)methodIndex] = PushNewFrame(frame);
    }

    /// <summary>
    /// Check if the thread is already stored in the output Profile, or adds it.
    /// </summary>
    /// <returns>The index to the output Profile frames array.</returns>
    private int AddThread(TraceThread thread)
    {
        var key = (int)thread.ThreadIndex;

        if (!_threadIndexes.ContainsKey(key))
        {
            Profile.Threads.Add(new()
            {
                Name = thread.ThreadInfo ?? $"Thread {thread.ThreadID}",
            });
            _threadIndexes[key] = Profile.Threads.Count - 1;
            _downsampler.NewThreadAdded(_threadIndexes[key]);
        }

        return _threadIndexes[key];
    }
}
