using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sentry.Internal.Extensions;

namespace Sentry
{
    /// <summary>
    /// A frame of a stacktrace.
    /// </summary>
    /// <see href="https://develop.sentry.dev/sdk/event-payloads/stacktrace/"/>
    public sealed class SentryStackFrame : IJsonSerializable
    {
        internal List<string>? InternalPreContext { get; private set; }

        internal List<string>? InternalPostContext { get; private set; }

        internal Dictionary<string, string>? InternalVars { get; private set; }

        internal List<int>? InternalFramesOmitted { get; private set; }

        /// <summary>
        /// The relative file path to the call.
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// The name of the function being called.
        /// </summary>
        public string? Function { get; set; }

        /// <summary>
        /// Platform-specific module path.
        /// </summary>
        public string? Module { get; set; }

        // Optional fields

        /// <summary>
        /// The line number of the call.
        /// </summary>
        public int? LineNumber { get; set; }

        /// <summary>
        /// The column number of the call.
        /// </summary>
        public int? ColumnNumber { get; set; }

        /// <summary>
        /// The absolute path to filename.
        /// </summary>
        public string? AbsolutePath { get; set; }

        /// <summary>
        /// Source code in filename at line number.
        /// </summary>
        public string? ContextLine { get; set; }

        /// <summary>
        /// A list of source code lines before context_line (in order) – usually [lineno - 5:lineno].
        /// </summary>
        public IList<string> PreContext => InternalPreContext ??= new List<string>();

        /// <summary>
        /// A list of source code lines after context_line (in order) – usually [lineno + 1:lineno + 5].
        /// </summary>
        public IList<string> PostContext => InternalPostContext ??= new List<string>();

        /// <summary>
        /// Signifies whether this frame is related to the execution of the relevant code in this stacktrace.
        /// </summary>
        /// <example>
        /// For example, the frames that might power the framework’s web server of your app are probably not relevant,
        /// however calls to the framework’s library once you start handling code likely are.
        /// </example>
        public bool? InApp { get; set; }

        /// <summary>
        /// A mapping of variables which were available within this frame (usually context-locals).
        /// </summary>
        public IDictionary<string, string> Vars => InternalVars ??= new Dictionary<string, string>();

        /// <summary>
        /// Which frames were omitted, if any.
        /// </summary>
        /// <remarks>
        /// If the list of frames is large, you can explicitly tell the system that you’ve omitted a range of frames.
        /// The frames_omitted must be a single tuple two values: start and end.
        /// </remarks>
        /// <example>
        /// If you only removed the 8th frame, the value would be (8, 9), meaning it started at the 8th frame,
        /// and went until the 9th (the number of frames omitted is end-start).
        /// The values should be based on a one-index.
        /// </example>
        public IList<int> FramesOmitted => InternalFramesOmitted ??= new List<int>();

        /// <summary>
        /// The assembly where the code resides.
        /// </summary>
        public string? Package { get; set; }

        /// <summary>
        /// This can override the platform for a single frame. Otherwise the platform of the event is assumed.
        /// </summary>
        public string? Platform { get; set; }

        /// <summary>
        /// Optionally an address of the debug image to reference.
        /// If this is set and a known image is defined by debug_meta then symbolication can take place.
        /// </summary>
        public long ImageAddress { get; set; }

        /// <summary>
        /// An optional address that points to a symbol.
        /// We actually use the instruction address for symbolication but this can be used to calculate an instruction offset automatically.
        /// </summary>
        public long? SymbolAddress { get; set; }

        /// <summary>
        /// An optional instruction address for symbolication.<br/>
        /// This should be a string with a hexadecimal number that includes a <b>0x</b> prefix.<br/>
        /// If this is set and a known image is defined in the <see href="https://develop.sentry.dev/sdk/event-payloads/debugmeta/">Debug Meta Interface</see>, then symbolication can take place.<br/>
        /// </summary>
        public string? InstructionAddress { get; set; }

        /// <summary>
        /// The instruction offset.
        /// </summary>
        /// <remarks>
        /// The official docs refer to it as 'The difference between instruction address and symbol address in bytes.'
        /// In .NET this means the IL Offset within the assembly.
        /// </remarks>
        public long? InstructionOffset { get; set; }

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteStringArrayIfNotEmpty("pre_context", InternalPreContext);
            writer.WriteStringArrayIfNotEmpty("post_context", InternalPostContext);
            writer.WriteStringDictionaryIfNotEmpty("vars", InternalVars!);
            writer.WriteArrayIfNotEmpty("frames_omitted", InternalFramesOmitted?.Cast<object>());
            writer.WriteStringIfNotWhiteSpace("filename", FileName);
            writer.WriteStringIfNotWhiteSpace("function", Function);
            writer.WriteStringIfNotWhiteSpace("module", Module);
            writer.WriteNumberIfNotNull("lineno", LineNumber);
            writer.WriteNumberIfNotNull("colno", ColumnNumber);
            writer.WriteStringIfNotWhiteSpace("abs_path", AbsolutePath);
            writer.WriteStringIfNotWhiteSpace("context_line", ContextLine);
            writer.WriteBooleanIfNotNull("in_app", InApp);
            writer.WriteStringIfNotWhiteSpace("package", Package);
            writer.WriteStringIfNotWhiteSpace("platform", Platform);
            writer.WriteNumberIfNotNull("image_addr", ImageAddress.NullIfDefault());
            writer.WriteNumberIfNotNull("symbol_addr", SymbolAddress);
            writer.WriteStringIfNotWhiteSpace("instruction_addr", InstructionAddress);
            writer.WriteNumberIfNotNull("instruction_offset", InstructionOffset);

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses from JSON.
        /// </summary>
        public static SentryStackFrame FromJson(JsonElement json)
        {
            var preContext = json.GetPropertyOrNull("pre_context")?.EnumerateArray().Select(j => j.GetString()).ToList();
            var postContext = json.GetPropertyOrNull("post_context")?.EnumerateArray().Select(j => j.GetString()).ToList();
            var vars = json.GetPropertyOrNull("vars")?.GetStringDictionaryOrNull();
            var framesOmitted = json.GetPropertyOrNull("frames_omitted")?.EnumerateArray().Select(j => j.GetInt32()).ToList();
            var filename = json.GetPropertyOrNull("filename")?.GetString();
            var function = json.GetPropertyOrNull("function")?.GetString();
            var module = json.GetPropertyOrNull("module")?.GetString();
            var lineNumber = json.GetPropertyOrNull("lineno")?.GetInt32();
            var columnNumber = json.GetPropertyOrNull("colno")?.GetInt32();
            var absolutePath = json.GetPropertyOrNull("abs_path")?.GetString();
            var contextLine = json.GetPropertyOrNull("context_line")?.GetString();
            var inApp = json.GetPropertyOrNull("in_app")?.GetBoolean();
            var package = json.GetPropertyOrNull("package")?.GetString();
            var platform = json.GetPropertyOrNull("platform")?.GetString();
            var imageAddress = json.GetPropertyOrNull("image_addr")?.GetInt64() ?? 0;
            var symbolAddress = json.GetPropertyOrNull("symbol_addr")?.GetInt64();
            var instructionAddress = json.GetPropertyOrNull("instruction_addr")?.GetString();
            var instructionOffset = json.GetPropertyOrNull("instruction_offset")?.GetInt64();

            return new SentryStackFrame
            {
                InternalPreContext = preContext!,
                InternalPostContext = postContext!,
                InternalVars = vars?.WhereNotNullValue().ToDictionary(),
                InternalFramesOmitted = framesOmitted,
                FileName = filename,
                Function = function,
                Module = module,
                LineNumber = lineNumber,
                ColumnNumber = columnNumber,
                AbsolutePath = absolutePath,
                ContextLine = contextLine,
                InApp = inApp,
                Package = package,
                Platform = platform,
                ImageAddress = imageAddress,
                SymbolAddress = symbolAddress,
                InstructionAddress = instructionAddress,
                InstructionOffset = instructionOffset
            };
        }
    }
}
