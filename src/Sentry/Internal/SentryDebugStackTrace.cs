using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Sentry.Internal.Extensions;
using Sentry.Extensibility;

namespace Sentry.Internal;

/// <summary>
/// Sentry Stacktrace with debug images.
/// </summary>
internal sealed class SentryDebugStackTrace : SentryStackTrace
{
    private readonly SentryOptions _options;

    // Debug images referenced by frames in this StackTrace
    private readonly Dictionary<Guid, int> _debugImageIndexByModule = new();
    private readonly List<DebugImage> _debugImages = new();

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

    private SentryDebugStackTrace(SentryOptions options) => _options = options;

    internal static SentryDebugStackTrace Create(SentryOptions options, StackTrace stackTrace, bool isCurrentStackTrace)
    {
        var result = new SentryDebugStackTrace(options);

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
        if (_debugImages.Count > 0)
        {
            // TODO we can deduplicate here if the same debug image is already in the event.
            // If it is found, update all frames referencing it instead of adding again.
            // At the moment, we just add all images
            @event.DebugImages ??= new();
            @event.DebugImages.AddRange(_debugImages);
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
                continue;
            }

            firstFrame = false;

            yield return CreateFrame(stackFrame);
        }
    }

    /// <summary>
    /// Create a <see cref="SentryStackFrame"/> from a <see cref="StackFrame"/>.
    /// </summary>
    private SentryStackFrame CreateFrame(StackFrame stackFrame) => InternalCreateFrame(stackFrame, true);

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

            var moduleIdx = AddDebugImage(method.Module);
            if (moduleIdx != null)
            {
                frame.AddressMode = string.Format("rel:{0}", moduleIdx);

                var token = method.MetadataToken;
                // The top byte is the token type, the lower three bytes are the record id.
                // See: https://docs.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/ms404456(v=vs.100)#metadata-token-structure
                var tokenType = token & 0xff000000;
                // See https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/metadata/cortokentype-enumeration
                if (tokenType == 0x06000000) // CorTokenType.mdtMethodDef
                {
                    var recordId = token & 0x00ffffff;
                    frame.FunctionId = string.Format("0x{0:x}", recordId);
                }
            }
        }

        frame.ConfigureAppFrame(_options);

        var frameFileName = stackFrame.GetFileName();
        if (projectPath != null && frameFileName?.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase) is true)
        {
            frameFileName = frameFileName.Substring(projectPath.Length);
        }

        frame.FileName = frameFileName;

        // stackFrame.HasILOffset() throws NotImplemented on Mono 5.12
        var ilOffset = stackFrame.GetILOffset();
        if (ilOffset != StackFrame.OFFSET_UNKNOWN)
        {
            frame.InstructionAddress = string.Format("0x{0:x}", ilOffset);
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
    private static void DemangleAnonymousFunction(SentryStackFrame frame)
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
        if (match.Success && match.Groups.Count == 2)
        {
            frame.Module = match.Groups[1].Value;
        }
    }

    private int? AddDebugImage(Module module)
    {
        var id = module.ModuleVersionId;

        if (_debugImageIndexByModule.TryGetValue(id, out var idx))
        {
            return idx;
        }
        idx = _debugImages.Count;

        var codeFile = module.FullyQualifiedName;
        if (!File.Exists(codeFile))
        {
            return null;
        }
        using var stream = File.OpenRead(codeFile);
        var peReader = new PEReader(stream);

        var headers = peReader.PEHeaders;
        var peHeader = headers.PEHeader;

        string? codeId = null;
        if (peHeader != null)
        {
            codeId = string.Format("{0:X8}{1:x}", headers.CoffHeader.TimeDateStamp, peHeader.SizeOfImage);
        }

        string? debugId = null;
        string? debugFile = null;
        string? debugChecksum = null;

        var debugDirs = peReader.ReadDebugDirectory();
        foreach (var entry in debugDirs)
        {
            if (entry.Type == DebugDirectoryEntryType.PdbChecksum)
            {
                var checksum = peReader.ReadPdbChecksumDebugDirectoryData(entry);
                var checksumHex = string.Concat(checksum.Checksum.Select(b => b.ToString("x2")));
                debugChecksum = string.Format("{0}:{1:x}", checksum.AlgorithmName, checksumHex);
            }
            if (!entry.IsPortableCodeView)
            {
                continue;
            }
            var codeView = peReader.ReadCodeViewDebugDirectoryData(entry);

            // Together 16B of the Guid concatenated with 4B of the TimeDateStamp field of the entry form a PDB ID that
            // should be used to match the PE/COFF image with the associated PDB (instead of Guid and Age).
            // Matching PDB ID is stored in the #Pdb stream of the .pdb file.
            // See https://github.com/dotnet/runtime/blob/main/docs/design/specs/PE-COFF.md#codeview-debug-directory-entry-type-2
            debugId = string.Format("{0}-{1:x}", codeView.Guid, entry.Stamp);
            debugFile = codeView.Path;
        }

        // well, we are out of luck :-(
        if (debugId == null)
        {
            return null;
        }

        _debugImages.Add(new DebugImage
        {
            Type = "pe_dotnet",
            CodeId = codeId,
            CodeFile = codeFile,
            DebugId = debugId,
            DebugChecksum = debugChecksum,
            DebugFile = debugFile,
        });
        _debugImageIndexByModule.Add(id, idx);

        return idx;
    }
}
