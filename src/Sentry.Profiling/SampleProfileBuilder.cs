using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Sentry.Extensibility;
using Sentry.Internal;
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

    // TODO reevaluate the use of SparseArray after setting up continous profiling. Dictionary might be better.
    // A sparse array that maps from StackSourceFrameIndex to an index in the output Profile.frames.
    private readonly SparseScalarArray<int> _frameIndexes = new(-1, 1000);

    // A dictionary from a CallStackIndex to an index in the output Profile.stacks.
    private readonly SparseScalarArray<int> _stackIndexes = new(100);

    // A sparse array mapping from a ThreadIndex to an index in Profile.Threads.
    private readonly SparseScalarArray<int> _threadIndexes = new(-1, 10);

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
            return;
        }

        if (!_downsampler.ShouldSample(threadIndex, timestampMs))
        {
            return;
        }

        var stackIndex = AddStackTrace(callStackIndex);
        if (stackIndex < 0)
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
            _downsampler.NewThreadAdded(_threadIndexes[key]);
        }

        return _threadIndexes[key];
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
        // if (_traceLog.CodeAddresses.Address(codeAddressIndex) is { } address)
        // {
        //     frame.InstructionAddress = $"0x{address:x}";
        // }

        return frame;
    }
}
