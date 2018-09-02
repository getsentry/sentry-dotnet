using System.Collections.Generic;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol
{
    /// <summary>
    /// A frame of a stacktrace
    /// </summary>
    /// <see href="https://docs.sentry.io/clientdev/interfaces/stacktrace/"/>
    [DataContract]
    public class SentryStackFrame
    {
        [DataMember(Name = "pre_context", EmitDefaultValue = false)]
        internal List<string> InternalPreContext { get; private set; }

        [DataMember(Name = "post_context", EmitDefaultValue = false)]
        internal List<string> InternalPostContext { get; private set; }

        [DataMember(Name = "vars", EmitDefaultValue = false)]
        internal Dictionary<string, string> InternalVars { get; private set; }

        [DataMember(Name = "frames_omitted ", EmitDefaultValue = false)]
        internal List<int> InternalFramesOmitted { get; private set; }

        /// <summary>
        /// The relative file path to the call
        /// </summary>
        [DataMember(Name = "filename", EmitDefaultValue = false)]
        public string FileName { get; set; }

        /// <summary>
        /// The name of the function being called
        /// </summary>
        [DataMember(Name = "function", EmitDefaultValue = false)]
        public string Function { get; set; }

        /// <summary>
        /// Platform-specific module path
        /// </summary>
        [DataMember(Name = "module", EmitDefaultValue = false)]
        public string Module { get; set; }

        // Optional fields

        /// <summary>
        /// The line number of the call
        /// </summary>
        [DataMember(Name = "lineno", EmitDefaultValue = false)]
        public int? LineNumber { get; set; }

        /// <summary>
        /// The column number of the call
        /// </summary>
        [DataMember(Name = "colno", EmitDefaultValue = false)]
        public int? ColumnNumber { get; set; }

        /// <summary>
        /// The absolute path to filename
        /// </summary>
        [DataMember(Name = "abs_path", EmitDefaultValue = false)]
        public string AbsolutePath { get; set; }

        /// <summary>
        /// Source code in filename at line number
        /// </summary>
        [DataMember(Name = "context_line", EmitDefaultValue = false)]
        public string ContextLine { get; set; }

        /// <summary>
        /// A list of source code lines before context_line (in order) – usually [lineno - 5:lineno]
        /// </summary>
        public IList<string> PreContext => InternalPreContext ?? (InternalPreContext = new List<string>());

        /// <summary>
        /// A list of source code lines after context_line (in order) – usually [lineno + 1:lineno + 5]
        /// </summary>
        public IList<string> PostContext => InternalPostContext ?? (InternalPostContext = new List<string>());

        /// <summary>
        /// Signifies whether this frame is related to the execution of the relevant code in this stacktrace.
        /// </summary>
        /// <example>
        /// For example, the frames that might power the framework’s web server of your app are probably not relevant,
        /// however calls to the framework’s library once you start handling code likely are.
        /// </example>
        [DataMember(Name = "in_app", EmitDefaultValue = false)]
        public bool? InApp { get; set; }

        /// <summary>
        /// A mapping of variables which were available within this frame (usually context-locals).
        /// </summary>
        public IDictionary<string, string> Vars => InternalVars ?? (InternalVars = new Dictionary<string, string>());

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
        public IList<int> FramesOmitted => InternalFramesOmitted ?? (InternalFramesOmitted = new List<int>());

        /// <summary>
        /// The assembly where the code resides
        /// </summary>
        [DataMember(Name = "package", EmitDefaultValue = false)]
        public string Package { get; set; }

        /// <summary>
        /// This can override the platform for a single frame. Otherwise the platform of the event is assumed.
        /// </summary>
        [DataMember(Name = "platform", EmitDefaultValue = false)]
        public string Platform { get; set; }

        /// <summary>
        /// Optionally an address of the debug image to reference.
        /// If this is set and a known image is defined by debug_meta then symbolication can take place.
        /// </summary>
        [DataMember(Name = "image_addr", EmitDefaultValue = false)]
        public long ImageAddress { get; set; }

        /// <summary>
        /// An optional address that points to a symbol.
        /// We actually use the instruction address for symbolication but this can be used to calculate an instruction offset automatically.
        /// </summary>
        [DataMember(Name = "symbol_addr", EmitDefaultValue = false)]
        public long? SymbolAddress { get; set; }

        /// <summary>
        /// The instruction offset
        /// </summary>
        /// <remarks>
        /// The official docs refer to it as 'The difference between instruction address and symbol address in bytes.'
        /// In .NET this means the IL Offset within the assembly
        /// </remarks>
        [DataMember(Name = "instruction_offset", EmitDefaultValue = false)]
        public long? InstructionOffset { get; set; }
    }
}
