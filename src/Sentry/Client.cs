using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using Simbolo.StackFrameData;

namespace Simbolo
{
    public static class Client
    {
        public static StackTraceInformation GetStackTraceInformation(Exception exception)
        {
            var stackTrace = new StackTrace(exception, true);
            if (stackTrace.FrameCount == 0)
            {
                return new StackTraceInformation();
            }

            var enhancedStackTrace = EnhancedStackTrace.GetFrames(stackTrace);

            var frames = new List<StackFrameInformation>();
            var debugMetas = new Dictionary<Guid, DebugMeta>();
            foreach (var stackFrame in enhancedStackTrace)
            {
                var frame = GetFrameInformation(stackFrame);
                if (frame is not null)
                {
                    frames.Add(frame);
                    if (frame.LineNumber == null && GetDebugMeta(stackFrame) is { } debugMeta
                                                 && !debugMetas.ContainsKey(debugMeta.ModuleId))
                    {
                        debugMetas[debugMeta.ModuleId] = debugMeta;
                    }
                }
            }

            return new StackTraceInformation(frames, debugMetas);
        }

        private static StackFrameInformation? GetFrameInformation(EnhancedStackFrame stackFrame)
        {
            if (stackFrame.GetMethod() is not { } method || method.Module.Assembly.IsDynamic)
            {
                return null;
            }

            // stackFrame.HasILOffset() throws NotImplemented on Mono 5.12
            var offset = stackFrame.GetILOffset();
            var isIlOffset = true;
            if (offset == 0)
            {
                isIlOffset = false;
                offset = stackFrame.GetNativeOffset();
            }

            int? lineNumber = stackFrame.GetFileLineNumber();
            if (lineNumber == 0)
            {
                lineNumber = null;
            }

            int? columnNumber = stackFrame.GetFileColumnNumber();
            if (columnNumber == 0)
            {
                columnNumber = null;
            }

            return new StackFrameInformation(
                stackFrame.ToString(),
                method.MetadataToken,
                EnhancedStackTrace.TryGetFullPath(stackFrame.GetFileName()!),
                offset,
                method.Module.ModuleVersionId,
                isIlOffset,
                null,
                stackFrame.MethodInfo.DeclaringType?.Assembly.FullName,
                stackFrame.MethodInfo.DeclaringType?.FullName?.Replace("+", "."),
                lineNumber,
                columnNumber);
        }

        private static readonly ConcurrentDictionary<Assembly, Lazy<DebugMeta>> Cache = new();
        private static readonly DebugMeta Empty = new("", Guid.Empty, "", Guid.Empty, 0, null);

        private static DebugMeta? GetDebugMeta(StackFrame frame)
        {
            var asm = frame.GetMethod()?.DeclaringType?.Assembly;
            var location = asm?.Location;
            if (location is null)
            {
                // TODO: Logging
                return null;
            }

            var cachedDebugMeta = Cache.GetOrAdd(asm!, ValueFactory);
            return cachedDebugMeta.Value == Empty ? null : cachedDebugMeta.Value;
        }

        private static Lazy<DebugMeta> ValueFactory(Assembly asm) =>
            new(() =>
            {
                var location = asm.Location;
                if (!File.Exists(location))
                {
                    return Empty;
                }

                using var stream = File.OpenRead(location);
                var reader = new PEReader(stream);
                var debugMeta = GetDebugMeta(reader);
                return debugMeta ?? Empty;
            });

        private static DebugMeta? GetDebugMeta(PEReader peReader)
        {
            var codeView = peReader.ReadDebugDirectory()
                .FirstOrDefault(d => d.Type == DebugDirectoryEntryType.CodeView);
            if (codeView.Type == DebugDirectoryEntryType.Unknown)
            {
                return null;
            }

            // Framework's assemblies don't have pdb checksum. I.e: System.Private.CoreLib.dll
            IEnumerable<string>? checksums = null;
            var pdbChecksum = peReader.ReadDebugDirectory()
                .FirstOrDefault(d => d.Type == DebugDirectoryEntryType.PdbChecksum);
            if (pdbChecksum.Type != DebugDirectoryEntryType.Unknown)
            {

                var checksumData = peReader.ReadPdbChecksumDebugDirectoryData(pdbChecksum);
                var algorithm = checksumData.AlgorithmName;
                var builder = new StringBuilder();
                builder.Append(algorithm);
                builder.Append(':');
                foreach (var bytes in checksumData.Checksum)
                {
                    builder.Append(bytes.ToString("x2"));
                }
                checksums = new[] {builder.ToString()};
            }

            var data = peReader.ReadCodeViewDebugDirectoryData(codeView);
            var isPortable = codeView.IsPortableCodeView;

            var signature = data.Guid;
            var age = data.Age;
            var file = data.Path;

            var metadataReader = peReader.GetMetadataReader();
            return new DebugMeta(
                file,
                metadataReader.GetGuid(metadataReader.GetModuleDefinition().Mvid),
                isPortable ? "ppdb" : "pdb",
                signature,
                age,
                checksums);
        }
    }
}

namespace Simbolo.StackFrameData
{
    public class StackTraceInformation
    {
        public IList<StackFrameInformation> StackFrameInformation { get; }
        public IDictionary<Guid, DebugMeta> DebugMetas { get; }

        public StackTraceInformation(
            IList<StackFrameInformation> stackFrameInformation,
            IDictionary<Guid, DebugMeta> debugMetas)
        {
            StackFrameInformation = stackFrameInformation;
            DebugMetas = debugMetas;
        }

        internal StackTraceInformation()
        {
            StackFrameInformation = new List<StackFrameInformation>();
            DebugMetas = new Dictionary<Guid, DebugMeta>(0);
        }

        public override string ToString() => ToString("default");

        public string ToString(string format) =>
            format switch
            {
                // "json" => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }),
                _ => ToStringDotnet(),
            };

        private string ToStringDotnet()
        {
            var builder = new StringBuilder(256);
            foreach (var info in StackFrameInformation)
            {
                if (info.Method is null)
                {
                    continue;
                }

                builder.Append("   at ");

                builder.Append(info.Method);
                if (info.FileName is not null)
                {
                    builder.Append(" in ");
                    builder.Append(info.FileName);
                }
                else if (info.Mvid is not null)
                {
                    builder.Append(" in ");
                    // Mono format
                    builder.Append('<');
                    builder.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:n}", info.Mvid.Value);
                    if (info.Aotid is not null)
                    {
                        builder.Append('#');
                        builder.Append(info.Aotid);
                    }
                    builder.Append('>');
                }

                if (info.LineNumber is not null)
                {
                    builder.Append(":line ");
                    builder.Append(info.LineNumber);
                }

                if (info.ColumnNumber is not null)
                {
                    builder.Append(':');
                    builder.Append(info.ColumnNumber);
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }
    }
}

namespace Simbolo.StackFrameData
{
    public record StackFrameInformation
    {
        public int? MethodIndex { get; }
        public int? Offset { get; }
        public bool? IsILOffset { get; }
        public Guid? Mvid { get; }
        public string? Aotid { get; }
        public string? FileName { get; }
        public string? Method { get; }
        // "package"
        public string? AssemblyFullName { get; }
        // "module"
        public string? TypeFullName { get; }
        public int? LineNumber { get; }
        public int? ColumnNumber { get; }

        public StackFrameInformation(
            string? method,
            int? methodIndex,
            string? fileName,
            int? offset,
            Guid? mvid,
            bool? isIlOffset,
            string? aotid,
            string? assemblyFullName,
            string? typeFullName,
            int? lineNumber,
            int? columnNumber)
        {
            MethodIndex = methodIndex;
            Offset = offset;
            IsILOffset = isIlOffset;
            Mvid = mvid;
            Aotid = aotid;
            FileName = fileName;
            Method = method;
            AssemblyFullName = assemblyFullName;
            TypeFullName = typeFullName;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        public override string ToString() =>
            $"{nameof(MethodIndex)}: {MethodIndex}, " +
            $"{nameof(Offset)}: {Offset}, " +
            $"{nameof(IsILOffset)}: {IsILOffset}, " +
            $"{nameof(Mvid)}: {Mvid}, " +
            $"{nameof(Aotid)}: {Aotid}, " +
            $"{nameof(FileName)}: {FileName}, " +
            $"{nameof(Method)}: {Method}, " +
            $"{nameof(AssemblyFullName)}: {AssemblyFullName}, " +
            $"{nameof(TypeFullName)}: {TypeFullName}, " +
            $"{nameof(LineNumber)}: {LineNumber}, " +
            $"{nameof(ColumnNumber)}: {ColumnNumber}";
    }
}

namespace Simbolo
{
    public class DebugMeta
    {
        public DebugMeta(string file, Guid moduleId, string type, Guid guid, int age, IEnumerable<string>? checksums)
        {
            File = file;
            ModuleId = moduleId;
            Type = type;
            Guid = guid;
            Age = age;
            Checksums = checksums;
        }

        public string File { get; }
        public Guid ModuleId { get; }
        public bool IsPortable => string.Equals(Type, "ppdb", StringComparison.InvariantCultureIgnoreCase);
        public string Type { get; }
        public Guid Guid { get; }
        public int Age { get; }
        public IEnumerable<string>? Checksums { get; }

        public override string ToString() =>
            $"{nameof(File)}: {File}, " +
            $"{nameof(ModuleId)}: {ModuleId}, " +
            $"{nameof(IsPortable)}: {IsPortable}, " +
            $"{nameof(Type)}: {Type}, " +
            $"{nameof(Guid)}: {Guid}, " +
            $"{nameof(Age)}: {Age}, " +
            $"{nameof(Checksums)}: {Environment.NewLine}{string.Join(Environment.NewLine, Checksums ?? Enumerable.Empty<string>())}";
    }
}
