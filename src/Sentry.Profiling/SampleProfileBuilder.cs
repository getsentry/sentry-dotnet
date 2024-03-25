using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Stacks;
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
    private readonly ActivityComputer _activityComputer;
    // private readonly StartStopActivityComputer _startStopActivityComputer;
    private readonly MutableTraceEventStackSource _stackSource;

    // Output profile being built.
    public readonly SampleProfile Profile = new();

    // A sparse array that maps from StackSourceFrameIndex to an index in the output Profile.frames.
    private readonly Dictionary<int, int> _frameIndexes = new();

    // A dictionary from a CallStackIndex to an index in the output Profile.stacks.
    private readonly Dictionary<int, int> _stackIndexes = new();

    // A dictionary from a StackSourceCallStackIndex to an index in the output Profile.stacks.
    private readonly Dictionary<int, int> _stackSourceStackIndexes = new();

    // A sparse array mapping from a ThreadIndex to an index in Profile.Threads.
    private readonly Dictionary<int, int> _threadIndexes = new();

    // A sparse array mapping from an ActivityIndex to an index in Profile.Threads.
    private readonly Dictionary<int, int> _activityIndexes = new();

    // TODO make downsampling conditional once this is available: https://github.com/dotnet/runtime/issues/82939
    private readonly Downsampler _downsampler = new();

    public SampleProfileBuilder(
        SentryOptions options,
        TraceLog traceLog,
        MutableTraceEventStackSource stackSource,
        ActivityComputer activityComputer)
    {
        _options = options;
        _traceLog = traceLog;
        _activityComputer = activityComputer;
        _stackSource = stackSource;
    }

    internal void AddSample(TraceEvent data, double timestampMs)
    {
        var thread = data.Thread();
        if (thread is null || thread.ThreadIndex == ThreadIndex.Invalid)
        {
            _options.DiagnosticLogger?.LogDebug("Encountered a Profiler Sample without a correct thread. Skipping.");
            return;
        }

        var activity = _activityComputer.GetCurrentActivity(thread);
        if (activity is null)
        {
            _options.DiagnosticLogger?.LogDebug("Failed to get activity for a profiler sample. Skipping.");
            return;
        }

        int threadIndex;
        if (activity.IsThreadActivity)
        {
            threadIndex = AddThread(thread);
        }
        else
        {
            threadIndex = AddActivity(activity);
        }

        if (threadIndex < 0)
        {
            _options.DiagnosticLogger?.LogDebug("Profiler Sample threadIndex is invalid. Skipping.");
            return;
        }

        // We need custom sampling because the TraceLog dispatches events from a queue with a delay of about 2 seconds.
        if (!_downsampler.ShouldSample(threadIndex, timestampMs))
        {
            return;
        }

        int stackIndex;
        if (activity.IsThreadActivity)
        {
            stackIndex = AddThreadStackTrace(data);
        }
        else
        {
            stackIndex = AddActivityStackTrace(thread, data);
        }

        if (stackIndex < 0)
        {
            _options.DiagnosticLogger?.LogDebug("Encountered a Profiler Sample without an associated call stack. Skipping.");
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
    private int AddThreadStackTrace(TraceEvent data)
    {
        var callStackIndex = data.CallStackIndex();
        if (callStackIndex == CallStackIndex.Invalid)
        {
            return -1;
        }

        var key = (int)callStackIndex;
        if (!_stackIndexes.TryGetValue(key, out var value))
        {
            Profile.Stacks.Add(CreateStackTrace(callStackIndex));
            value = Profile.Stacks.Count - 1;
            _stackIndexes[key] = value;
        }
        return value;
    }

    /// <summary>
    /// Adds stack trace and frames, if missing.
    /// </summary>
    /// <returns>The index into the Profile's stacks list</returns>
    private int AddActivityStackTrace(TraceThread thread, TraceEvent data)
    {
        var stackSourceCallStackIndex = StackSourceCallStackIndex.Invalid;
        lock (_stackSource)
        {
            stackSourceCallStackIndex = _activityComputer.GetCallStack(_stackSource, data,
                null // TODO topThread => _startStopActivityComputer.GetCurrentStartStopActivityStack(_stackSource, thread, topThread)
            );
        }
        if (stackSourceCallStackIndex == StackSourceCallStackIndex.Invalid)
        {
            return -1;
        }

        var key = (int)stackSourceCallStackIndex;
        if (!_stackSourceStackIndexes.TryGetValue(key, out var value))
        {
            Profile.Stacks.Add(CreateStackTrace(stackSourceCallStackIndex));
            value = Profile.Stacks.Count - 1;
            _stackIndexes[key] = value;
        }
        return value;
    }

    private Internal.GrowableArray<int> CreateStackTrace(CallStackIndex callstackIndex)
    {
        var stackTrace = new Internal.GrowableArray<int>(10);
        while (callstackIndex != CallStackIndex.Invalid)
        {
            var codeAddressIndex = _traceLog.CallStacks.CodeAddressIndex(callstackIndex);
            if (codeAddressIndex == CodeAddressIndex.Invalid)
            {
                // No need to traverse further up the stack when we're on the thread/process.
                break;
            }

            stackTrace.Add(AddStackFrame(codeAddressIndex));
            callstackIndex = _traceLog.CallStacks.Caller(callstackIndex);
        }

        stackTrace.Trim(10);
        return stackTrace;
    }

    private Internal.GrowableArray<int> CreateStackTrace(StackSourceCallStackIndex callstackIndex)
    {
        var stackTrace = new Internal.GrowableArray<int>(10);
        CodeAddressIndex codeAddressIndex;
        while (callstackIndex != StackSourceCallStackIndex.Invalid
            // GetFrameIndex() throws... seems to be happening on top thread frames for some reason...
            && (callstackIndex - _stackSource.Interner.CallStackStartIndex) < _stackSource.Interner.CallStackCount)
        {
            lock (_stackSource)
            {
                var frameIndex = _stackSource.GetFrameIndex(callstackIndex);
                codeAddressIndex = _stackSource.GetFrameCodeAddress(frameIndex);
                if (codeAddressIndex == CodeAddressIndex.Invalid)
                {
                    // No need to traverse further up the stack when we're on the thread/process.
                    break;
                }

                callstackIndex = _stackSource.GetCallerIndex(callstackIndex);
            }

            stackTrace.Add(AddStackFrame(codeAddressIndex));
        }

        stackTrace.Trim(10);
        return stackTrace;
    }

    /// <summary>
    /// Check if the frame is already stored in the output Profile, or adds it.
    /// </summary>
    /// <returns>The index to the output Profile frames array.</returns>
    private int AddStackFrame(CodeAddressIndex codeAddressIndex)
    {
        var key = (int)codeAddressIndex;

        if (!_frameIndexes.TryGetValue(key, out var value))
        {
            Profile.Frames.Add(CreateStackFrame(codeAddressIndex));
            value = Profile.Frames.Count - 1;
            _frameIndexes[key] = value;
        }

        return value;
    }

    /// <summary>
    /// Ensures the thread is stored in the output Profile.
    /// </summary>
    /// <returns>The index to the output Profile thread array.</returns>
    private int AddThread(TraceThread thread)
    {
        var key = (int)thread.ThreadIndex;

        if (!_threadIndexes.TryGetValue(key, out var value))
        {
            value = AddSampleProfileThread(thread.ThreadInfo ?? $"Thread {thread.ThreadID}");
            _threadIndexes[key] = value;
        }

        return value;
    }

    /// <summary>
    /// Ensures the activity is stored in the output Profile as a Thread.
    /// </summary>
    /// <returns>The index to the output Profile thread array.</returns>
    private int AddActivity(TraceActivity activity)
    {
        var key = (int)activity.Index;

        if (!_activityIndexes.TryGetValue(key, out var value))
        {
            // Note: there's also activity.Name but it's rather verbose:
            // '<Activity (continuation) Index="8" Thread="Thread (1652)" Create="216.744" Start="216.938" kind="TaskScheduled" RawID="0x2072a40000000004"/>'
            value = AddSampleProfileThread($"Activity {activity.Path}");
            _activityIndexes[key] = value;
        }

        return value;
    }

    private int AddSampleProfileThread(string name)
    {
        Profile.Threads.Add(new() { Name = name });
        var value = Profile.Threads.Count - 1;
        _downsampler.NewThreadAdded(value);
        return value;
    }

    private SentryStackFrame CreateStackFrame(CodeAddressIndex codeAddressIndex)
    {
        var frame = new SentryStackFrame();

        var methodIndex = _traceLog.CodeAddresses.MethodIndex(codeAddressIndex);
        if (_traceLog.CodeAddresses.Methods[methodIndex] is { } method)
        {
            frame.Function = method.FullMethodName;

            if (method.MethodModuleFile is { } moduleFile)
            {
                frame.Module = moduleFile.Name;
            }

            frame.ConfigureAppFrame(_options);
        }
        else
        {
            // Fall back if the method info is unknown, see more info on Symbol resolution in
            // https://github.com/getsentry/perfview/blob/031250ffb4f9fcadb9263525d6c9f274be19ca51/src/PerfView/SupportFiles/UsersGuide.htm#L7745-L7784
            frame.InstructionAddress = (long?)_traceLog.CodeAddresses.Address(codeAddressIndex);

            if (_traceLog.CodeAddresses.ModuleFile(codeAddressIndex) is { } moduleFile)
            {
                frame.Module = moduleFile.Name;
                frame.ConfigureAppFrame(_options);
            }
            else
            {
                frame.InApp = false;
            }
        }

        return frame;
    }
}
