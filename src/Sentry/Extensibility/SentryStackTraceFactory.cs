using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using Sentry.Protocol;

namespace Sentry.Extensibility
{
    /// <summary>
    /// Default factory to <see cref="SentryStackTrace" /> from an <see cref="Exception" />.
    /// </summary>
    public class SentryStackTraceFactory : ISentryStackTraceFactory
    {
        private readonly SentryOptions _options;

        public SentryStackTraceFactory(SentryOptions options) => _options = options;

        /// <summary>
        /// Creates a <see cref="SentryStackTrace" /> from the optional <see cref="Exception" />.
        /// </summary>
        /// <param name="exception">The exception to create the stacktrace from.</param>
        /// <returns>A Sentry stack trace.</returns>
        public SentryStackTrace Create(Exception exception = null)
        {
            var isCurrentStackTrace = exception == null && _options.AttachStacktrace;

            if (exception == null && !isCurrentStackTrace)
            {
                _options.DiagnosticLogger?.LogDebug(
                    "No Exception and AttachStacktrace is off. No stack trace will be collected.");
                return null;
            }

            _options.DiagnosticLogger?.LogDebug("Creating SentryStackTrace. isCurrentStackTrace: {0}.",
                isCurrentStackTrace);

            return Create(CreateStackTrace(exception), isCurrentStackTrace);
        }

        protected virtual StackTrace CreateStackTrace(Exception exception) =>
            exception == null ? new StackTrace(true) : new StackTrace(exception, true);

        internal SentryStackTrace Create(StackTrace stackTrace, bool isCurrentStackTrace)
        {
            var frames = CreateFrames(stackTrace, isCurrentStackTrace)
                // Sentry expects the frames to be sent in reversed order
                .Reverse();

            var stacktrace = new SentryStackTrace();

            foreach (var frame in frames)
            {
                stacktrace.Frames.Add(frame);
            }

            return stacktrace.Frames.Count == 0
                ? null
                : stacktrace;
        }

        internal IEnumerable<SentryStackFrame> CreateFrames(StackTrace stackTrace, bool isCurrentStackTrace)
        {
            var frames = stackTrace?.GetFrames();
            if (frames == null)
            {
                _options.DiagnosticLogger?.LogDebug(
                    "No stack frames found. AttachStacktrace: '{0}', isCurrentStackTrace: '{1}'",
                    _options.AttachStacktrace, isCurrentStackTrace);

                yield break;
            }

            var firstFrames = true;
            foreach (var stackFrame in frames)
            {
                // Remove the frames until the call for capture with the SDK
                if (firstFrames
                    && isCurrentStackTrace
                    && stackFrame.GetMethod() is MethodBase method
                    && method.DeclaringType?.AssemblyQualifiedName?.StartsWith("Sentry") == true)
                {
                    continue;
                }

                firstFrames = false;

                var frame = CreateFrame(stackFrame, isCurrentStackTrace);
                if (frame != null)
                {
                    yield return frame;
                }
            }
        }

        internal SentryStackFrame CreateFrame(StackFrame stackFrame) => InternalCreateFrame(stackFrame, true);

        protected virtual SentryStackFrame CreateFrame(StackFrame stackFrame, bool isCurrentStackTrace) =>
            InternalCreateFrame(stackFrame, true);

        private MetadataReaderProvider GetMetadataReaderProvider(Assembly assembly)
        {
            if (assembly.IsDynamic || !(assembly.Location is string assemblyLocation))
            {
                return null;
            }

            MetadataReaderProvider GetProviderFromDebugSymbol()
            {
                var pdbLocation = assemblyLocation;
                if (assemblyLocation.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase)
                    || assemblyLocation.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase))
                {
                    pdbLocation = assemblyLocation.Substring(0, assemblyLocation.Length - 4);
                }

                pdbLocation += ".pdb";

                try
                {
                    var pdbStream = File.OpenRead(pdbLocation);
                    return MetadataReaderProvider.FromPortablePdbStream(pdbStream);
                }
                catch (Exception e)
                {
                    _options.DiagnosticLogger?.LogError("Failed loading debug symbol at location {0}.", e,
                        pdbLocation);
                    return null;
                }
            }

            MetadataReaderProvider GetProviderFromAssembly()
            {
                try
                {
                    var assemblyStream = File.OpenRead(assemblyLocation);
                    var reader = new PEReader(assemblyStream);
                    if (!reader.HasMetadata)
                    {
                        return null;
                    }

                    foreach (var debugDirectoryEntry in reader.ReadDebugDirectory())
                    {
                        if (debugDirectoryEntry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb)
                        {
                            return reader.ReadEmbeddedPortablePdbDebugDirectoryData(debugDirectoryEntry);
                        }
                    }
                }
                catch (Exception e)
                {
                    _options.DiagnosticLogger?.LogError("Failed loading assembly at location {0}.", e,
                        assemblyLocation);
                }

                return null;
            }

            return GetProviderFromDebugSymbol() ?? GetProviderFromAssembly();
        }

        public static readonly Guid EmbeddedSource = new Guid("0E8A571B-6926-466E-B4AD-8AB04611F5FE");
        public static readonly Guid SourceLinkId = new Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A");

        protected SentryStackFrame InternalCreateFrame(StackFrame stackFrame, bool demangle)
        {
            const string unknownRequiredField = "(unknown)";
            var frame = new SentryStackFrame();
            if (GetMethod(stackFrame) is MethodBase method)
            {
                // TODO: SentryStackFrame.TryParse and skip frame instead of these unknown values:

                frame.Function = method.Name;
                if (method.DeclaringType is Type declaringType)
                {
                    frame.Module = declaringType.FullName ?? unknownRequiredField;
                    frame.Package = declaringType.Assembly.FullName;
                    // TODO: Cache this
                    // TODO: Make async
                    try
                    {
                        if (GetMetadataReaderProvider(declaringType.Assembly) is MetadataReaderProvider provider)
                        {
                            using (provider)
                            {
                                var metadataReader = provider.GetMetadataReader();
                                if (metadataReader == null)
                                {
                                    return null;
                                }

                                var blobHandle = default(BlobHandle);
                                foreach (var cdih in metadataReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition)
                                )
                                {
                                    var cdi = metadataReader.GetCustomDebugInformation(cdih);
                                    if (metadataReader.GetGuid(cdi.Kind) == SourceLinkId)
                                        blobHandle = cdi.Value;
                                }

                                if (blobHandle.IsNil)
                                {
                                    return null;
                                }

                                var bytes = metadataReader.GetBlobBytes(blobHandle);
                                frame.Vars.Add("source-link", Encoding.UTF8.GetString(bytes));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _options.DiagnosticLogger?.LogError("Failed getting soucelink.", e);
                    }
                }
            }

            frame.InApp = !IsSystemModuleName(frame.Module);
            frame.FileName = stackFrame.GetFileName();

            // stackFrame.HasILOffset() throws NotImplemented on Mono 5.12
            var ilOffset = stackFrame.GetILOffset();
            if (ilOffset != 0)
            {
                frame.InstructionOffset = stackFrame.GetILOffset();
            }

            var lineNo = stackFrame.GetFileLineNumber();
            if (lineNo != 0)
            {
                frame.LineNumber = lineNo;
            }

            var colNo = stackFrame.GetFileColumnNumber();
            if (lineNo != 0)
            {
                frame.ColumnNumber = colNo;
            }

            if (demangle)
            {
                DemangleAsyncFunctionName(frame);
                DemangleAnonymousFunction(frame);
            }

            return frame;
        }

        protected virtual MethodBase GetMethod(StackFrame stackFrame) => stackFrame.GetMethod();

        private bool IsSystemModuleName(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
            {
                return false;
            }

            foreach (var include in _options.InAppInclude)
            {
                if (moduleName.StartsWith(include, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            foreach (var exclude in _options.InAppExclude)
            {
                if (moduleName.StartsWith(exclude, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
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

            var match = Regex.Match(frame.Module, @"^(.*)\+<(\w*)>d__\d*$");
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
            if (frame?.Function == null)
            {
                return;
            }

            // Search for the function name in angle brackets followed by b__<digits/letters>.
            //
            // Change:
            //   <BeginInvokeAsynchronousActionMethod>b__36
            // to:
            //   BeginInvokeAsynchronousActionMethod { <lambda> }

            var match = Regex.Match(frame.Function, @"^<(\w*)>b__\w+$");
            if (match.Success && match.Groups.Count == 2)
            {
                frame.Function = match.Groups[1].Value + " { <lambda> }";
            }
        }
    }
}
