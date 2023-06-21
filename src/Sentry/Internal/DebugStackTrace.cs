using Sentry.Internal.Extensions;
using Sentry.Extensibility;
using Sentry.Internal.ILSpy;

namespace Sentry.Internal;

/// <summary>
/// Sentry Stacktrace with debug images.
/// </summary>
internal class DebugStackTrace : SentryStackTrace
{
    private readonly SentryOptions _options;

    // Debug images referenced by frames in this StackTrace
    private readonly Dictionary<Guid, int> _debugImageIndexByModule = new();
    private const int DebugImageMissing = -1;
    private bool _debugImagesMerged;

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

    internal DebugStackTrace(SentryOptions options)
    {
        _options = options;
    }

    protected List<DebugImage> DebugImages { get; } = new();

    internal static DebugStackTrace Create(SentryOptions options, StackTrace stackTrace, bool isCurrentStackTrace)
    {
        var result = new DebugStackTrace(options);

        var frames = result.CreateFrames(stackTrace, isCurrentStackTrace)
            .Reverse(); // Sentry expects the frames to be sent in reversed order

        foreach (var frame in frames)
        {
            result.Frames.Add(frame);
        }

        return result;
    }

    internal void MergeDebugImagesInto(SentryEvent @event)
    {
        // This operation may only be run once because it is destructive to the object state;
        // Frame indexes may be changed as well as _debugImageIndexByModule becoming invalid.
        if (_debugImagesMerged)
        {
            throw new InvalidOperationException("Cannot call MergeDebugImagesInto multiple times");
        }
        _debugImagesMerged = true;

        _options.LogDebug("Merging {0} debug images from stacktrace.", DebugImages.Count);
        if (DebugImages.Count == 0)
        {
            return;
        }

        @event.DebugImages ??= new();

        if (@event.DebugImages.Count == 0)
        {
            // Default case when there's just a single stacktrace (i.e. no inner exceptions).
            @event.DebugImages.AddRange(DebugImages);
            return;
        }

        // Otherwise, we must merge new images into an existing list, which means their relative indexes change.
        // Therefore, we must also update indices specified in frame.AddressMode for each affected frame.
        // We could just append _debugImages to @event.DebugImages and shift all indexes but merging is simple too.
        var originalCount = @event.DebugImages.Count;
        Dictionary<string, string> relocations = new();
        for (var i = 0; i < DebugImages.Count; i++)
        {
            // First check if the image is already present on the event. Simple lookup should be faster than
            // constructing a map first, assuming there are normally just a few debug images affected.
            var found = false;
            for (var j = 0; j < originalCount; j++)
            {
                if (DebugImages[i].ModuleVersionId == @event.DebugImages[j].ModuleVersionId)
                {
                    if (i != j)
                    {
                        relocations.Add(GetRelativeAddressMode(i), GetRelativeAddressMode(j));
                    }
                    found = true;
                }
            }

            if (!found)
            {
                relocations.Add(GetRelativeAddressMode(i), GetRelativeAddressMode(@event.DebugImages.Count));
                @event.DebugImages.Add(DebugImages[i]);
            }
        }

        foreach (var frame in Frames)
        {
            if (frame.AddressMode is not null && relocations.TryGetValue(frame.AddressMode, out var newAddressMode))
            {
                frame.AddressMode = newAddressMode;
            }
        }
    }

    /// <summary>
    /// Creates an enumerator of <see cref="SentryStackFrame"/> from a <see cref="StackTrace"/>.
    /// </summary>
    private IEnumerable<SentryStackFrame> CreateFrames(StackTrace stackTrace, bool isCurrentStackTrace)
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
                _options.LogDebug("Skipping initial stack frame '{0}'", method.Name);
                continue;
            }

            firstFrame = false;

            yield return CreateFrame(stackFrame);
        }
    }

    /// <summary>
    /// Create a <see cref="SentryStackFrame"/> from a <see cref="StackFrame"/>.
    /// </summary>
    internal SentryStackFrame CreateFrame(StackFrame stackFrame) => InternalCreateFrame(stackFrame, true);

    /// <summary>
    /// Default the implementation of CreateFrame.
    /// </summary>
    private SentryStackFrame InternalCreateFrame(StackFrame stackFrame, bool demangle)
    {
        const string unknownRequiredField = "(unknown)";
        string? projectPath = null;
        var frame = new SentryStackFrame();
        if (stackFrame.GetMethod() is { } method)
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

                    // Ben.Demystifier doesn't always include the namespace, even when fullName==true.
                    // It's important that the module name always be fully qualified, so that in-app frame
                    // detection works correctly.
                    var module = stringBuilder.ToString();
                    frame.Module = declaringType.Namespace is { } ns && !module.StartsWith(ns)
                        ? $"{ns}.{module}"
                        : module;
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

            if (AddDebugImage(method.Module) is { } moduleIdx && moduleIdx != DebugImageMissing)
            {
                frame.AddressMode = GetRelativeAddressMode(moduleIdx);

                try
                {
                    var token = method.MetadataToken;
                    // The top byte is the token type, the lower three bytes are the record id.
                    // See: https://docs.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/ms404456(v=vs.100)#metadata-token-structure
                    var tokenType = token & 0xff000000;
                    // See https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/metadata/cortokentype-enumeration
                    if (tokenType == 0x06000000) // CorTokenType.mdtMethodDef
                    {
                        var recordId = token & 0x00ffffff;
                        frame.FunctionId = $"0x{recordId:x}";
                    }
                }
                catch (InvalidOperationException)
                {
                    // method.MetadataToken may throw
                    // see https://learn.microsoft.com/en-us/dotnet/api/system.reflection.memberinfo.metadatatoken?view=net-6.0
                    _options.LogDebug("Could not get MetadataToken for stack frame {0} from {1}", frame.Function, method.Module.Name);
                }
            }
        }

        frame.ConfigureAppFrame(_options);

        if (stackFrame.GetFileName() is { } frameFileName)
        {
            if (projectPath != null && frameFileName.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
            {
                frame.AbsolutePath = frameFileName;
                frameFileName = frameFileName[projectPath.Length..];
            }
            frame.FileName = frameFileName;
        }

        // stackFrame.HasILOffset() throws NotImplemented on Mono 5.12
        var ilOffset = stackFrame.GetILOffset();
        if (ilOffset != StackFrame.OFFSET_UNKNOWN)
        {
            frame.InstructionAddress = $"0x{ilOffset:x}";
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
            // TODO what is this really about? we have already run ConfigureAppFrame() at this time...
            frame.Module = null;
        }

        return frame;
    }

    private static string GetRelativeAddressMode(int moduleIndex) => $"rel:{moduleIndex}";

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
        if (match is { Success: true, Groups.Count: 3 })
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
        if (match is { Success: true, Groups.Count: 2 })
        {
            frame.Function = match.Groups[1].Value + " { <lambda> }";
        }
    }

    /// <summary>
    /// Remove return type from module in a Task with a Lambda with a return value.
    /// This was seen in Unity, see https://github.com/getsentry/sentry-unity/issues/845
    /// </summary>
    private static void DemangleLambdaReturnType(SentryStackFrame frame)
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
        if (match is { Success: true, Groups.Count: 2 })
        {
            frame.Module = match.Groups[1].Value;
        }
    }

    private static PEReader? TryReadAssemblyFromDisk(Module module, SentryOptions options, out string? assemblyName)
    {
        assemblyName = module.FullyQualifiedName;
        if (options.AssemblyReader is { } reader)
        {
            return reader.Invoke(assemblyName);
        }

        try
        {
            var assembly = File.OpenRead(assemblyName);
            return new PEReader(assembly);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private int? AddDebugImage(Module module)
    {
        var id = module.ModuleVersionId;
        if (_debugImageIndexByModule.TryGetValue(id, out var idx))
        {
            return idx;
        }

        var debugImage = GetDebugImage(module, _options);
        if (debugImage == null)
        {
            // don't try to resolve again
            _debugImageIndexByModule.Add(id, DebugImageMissing);
            return null;
        }

        idx = DebugImages.Count;
        DebugImages.Add(debugImage);
        _debugImageIndexByModule.Add(id, idx);

        return idx;
    }

    internal static DebugImage? GetDebugImage(Module module, SentryOptions options)
    {
        // Try to get it from disk (most common use case)
        var moduleName = module.GetNameOrScopeName();
        using var peDiskReader = TryReadAssemblyFromDisk(module, options, out var assemblyName);
        if (peDiskReader is not null)
        {
            if (peDiskReader.TryGetPEDebugImageData().ToDebugImage(assemblyName, module.ModuleVersionId) is not { } debugImage)
            {
                options.LogInfo("Skipping debug image for module '{0}' because the Debug ID couldn't be determined", moduleName);
                return null;
            }

            options.LogDebug("Got debug image for '{0}' having Debug ID: {1}", moduleName, debugImage.DebugId);
            return debugImage;
        }

#if NET5_0_OR_GREATER && PLATFORM_NEUTRAL
        // Maybe we're dealing with a single file assembly
        // https://github.com/getsentry/sentry-dotnet/issues/2362
        if (SingleFileApp.MainModule.IsBundle())
        {
            if (SingleFileApp.MainModule?.GetDebugImage(module) is not { } embeddedDebugImage)
            {
                options.LogInfo("Skipping embedded debug image for module '{0}' because the Debug ID couldn't be determined", moduleName);
                return null;
            }

            options.LogDebug("Got embedded debug image for '{0}' having Debug ID: {1}", moduleName, embeddedDebugImage.DebugId);
            return embeddedDebugImage;
        }
#endif

        // Finally, admit defeat
        options.LogDebug("Skipping debug image for module '{0}' because assembly wasn't found: '{1}'",
            moduleName, assemblyName);
        return null;
    }
}
