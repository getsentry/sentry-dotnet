using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.EventPipe;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry.Profiling;

// A list of frame indexes.
using SentryProfileStackTrace = HashableGrowableArray<int>;

/// <summary>
/// Processes TraceLog to produce a SampleProfile.
/// </summary>
internal class TraceLogProcessor
{
    private readonly SentryOptions _options;
    private readonly TraceLog _traceLog;

    // Output profile being built.
    public readonly SampleProfile Profile = new();

    // A sparse array that maps from StackSourceFrameIndex to an index in the output Profile.frames.
    private readonly SparseScalarArray<int> _frameIndexes = new(-1, 1000);

    // A dictionary from a StackTrace sealed array to an index in the output Profile.stacks.
    private readonly Dictionary<SentryProfileStackTrace, int> _stackIndexes = new(100);

    // A sparse array mapping from a ThreadIndex to an index in Profile.Threads.
    private readonly SparseScalarArray<int> _threadIndexes = new(-1, 10);

    private readonly double _timeShiftMs;

    public TraceLogProcessor(SentryOptions options, TraceLog traceLog, double TimeShiftMs)
    {
        _options = options;
        _traceLog = traceLog;
        _timeShiftMs = TimeShiftMs;
    }

    internal void AddSample(TraceEvent data)
    {
        var thread = data.Thread();
        if (thread.ThreadIndex == ThreadIndex.Invalid)
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

        var timestampMs = data.TimeStampRelativeMSec + _timeShiftMs;

        var stackIndex = AddStackTrace(callStackIndex);
        if (stackIndex < 0)
        {
            return;
        }

        var threadIndex = AddThread(thread);
        if (threadIndex < 0)
        {
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
        SentryProfileStackTrace stackTrace = new(10);
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

        int result = -1;
        if (stackTrace.Count > 0)
        {
            stackTrace.Seal();
            if (!_stackIndexes.TryGetValue(stackTrace, out result))
            {
                stackTrace.Trim(10);
                Profile.Stacks.Add(stackTrace);
                result = Profile.Stacks.Count - 1;
                _stackIndexes[stackTrace] = result;
            }
        }

        return result;
    }

    /// <summary>
    /// Check if the frame is already stored in the output Profile, or adds it.
    /// </summary>
    /// <returns>The index to the output Profile frames array.</returns>
    private int AddStackFrame(CodeAddressIndex codeAddressIndex)
    {
        var key = (int)codeAddressIndex;

        if (!_frameIndexes.ContainsKey(key))
        {
            Profile.Frames.Add(CreateStackFrame(codeAddressIndex));
            _frameIndexes[key] = Profile.Frames.Count - 1;
        }

        return _frameIndexes[key];
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
        }

        return _threadIndexes[key];
    }

    private static string ActivityPath(TraceActivity activity)
    {
        var creator = activity.Creator;
        if (creator is null || creator.IsThreadActivity)
        {
            return activity.Index.ToString();
        }
        else
        {
            return $"{ActivityPath(creator)}/{activity.Index.ToString()}";
        }
    }

    private SentryStackFrame CreateStackFrame(CodeAddressIndex codeAddressIndex)
    {
        var frame = new SentryStackFrame();

        var methodIndex = _traceLog.CodeAddresses.MethodIndex(codeAddressIndex);
        if (_traceLog.CodeAddresses.Methods[methodIndex] is { } method)
        {
            frame.Function = method.FullMethodName;

            TraceModuleFile moduleFile = method.MethodModuleFile;
            if (moduleFile is not null)
            {
                frame.Module = moduleFile.Name;
            }

            frame.ConfigureAppFrame(_options);
        }
        else
        {
            // native frame
            frame.InApp = false;
        }

        // TODO enable this once we implement symbolication (we will need to send debug_meta too), see StackTraceFactory.
        // if (_traceLog.CodeAddresses.ILOffset(codeAddressIndex) is { } ilOffset && ilOffset >= 0)
        // {
        //     frame.InstructionOffset = ilOffset;
        // }
        // else if (_traceLog.CodeAddresses.Address(codeAddressIndex) is { } address)
        // {
        //     frame.InstructionAddress = $"0x{address:x}";
        // }

        return frame;
    }
}
