using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using Sentry.Infrastructure;
using Sentry.Protocol;
using Sentry.Reflection;

// ReSharper disable once CheckNamespace
namespace Sentry
{
    /// <summary>
    /// An event to be sent to Sentry
    /// </summary>
    /// <seealso href="https://docs.sentry.io/clientdev/attributes/"/>
    [DataContract]
    [DebuggerDisplay("{GetType().Name,nq}: {" + nameof(EventId) + ",nq}")]

    public class SentryEvent : Scope
    {
        [DataMember(Name = "modules", EmitDefaultValue = false)]
        internal IImmutableDictionary<string, string> InternalModules { get; private set; }

        [DataMember(Name = "event_id", EmitDefaultValue = false)]
        private string SerializableEventId => EventId.ToString("N");

        /// <summary>
        /// The unique identifier of this event
        /// </summary>
        /// <remarks>
        /// Hexadecimal string representing a uuid4 value.
        /// The length is exactly 32 characters (no dashes!)
        /// </remarks>
        public Guid EventId { get; }

        /// <summary>
        /// Gets the message that describes this event
        /// </summary>
        [DataMember(Name = "message", EmitDefaultValue = false)]
        public string Message { get; set; }

        /// <summary>
        /// Indicates when the event was created
        /// </summary>
        /// <example>2018-04-03T17:41:36</example>
        [DataMember(Name = "timestamp", EmitDefaultValue = false)]
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Name of the logger (or source) of the event
        /// </summary>
        [DataMember(Name = "logger", EmitDefaultValue = false)]
        public string Logger { get; set; }

        /// <summary>
        /// The name of the platform
        /// </summary>
        [DataMember(Name = "platform", EmitDefaultValue = false)]
        public string Platform { get; set; }

        /// <summary>
        /// Sentry level
        /// </summary>
        [DataMember(Name = "level", EmitDefaultValue = false)]
        public SentryLevel? Level { get; set; }

        /// <summary>
        /// The culprit
        /// </summary>
        /// <remarks>
        /// This value is essentially obsolete in favor of Transaction.
        /// </remarks>
        // TODO: Delete?
        [DataMember(Name = "culprit", EmitDefaultValue = false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string Culprit { get; set; }

        /// <summary>
        /// Identifies the host SDK from which the event was recorded.
        /// </summary>
        [DataMember(Name = "server_name", EmitDefaultValue = false)]
        public string ServerName { get; set; }

        /// <summary>
        /// The release version of the application.
        /// </summary>
        [DataMember(Name = "release", EmitDefaultValue = false)]
        public string Release { get; set; }

        /// <summary>
        /// The Sentry Exception interface
        /// </summary>
        [DataMember(Name = "exception", EmitDefaultValue = false)]
        internal SentryValues<SentryException> SentryExceptions { get; set; }

        /// <summary>
        /// A list of relevant modules and their versions.
        /// </summary>
        public IImmutableDictionary<string, string> Modules
        {
            get => InternalModules ?? (InternalModules = ImmutableDictionary<string, string>.Empty);
            internal set => InternalModules = value;
        }

        public SentryEvent() : this(null)
        { }

        /// <summary>
        /// Creates a Sentry event with optional Exception details and default values like Id and Timestamp
        /// </summary>
        /// <param name="exception">The exception.</param>
        public SentryEvent(Exception exception)
            : this(exception, null)
        { }

        // TODO: event as POCO, take this log out
        internal SentryEvent(
            Exception exception = null,
            ISystemClock clock = null,
            Guid id = default,
            bool? isUnhandled = null,
            bool populate = true)
        {
            clock = clock ?? SystemClock.Clock;
            EventId = id == default ? Guid.NewGuid() : id;

            Timestamp = clock.GetUtcNow();

            if (populate)
            {
                Populate(exception, isUnhandled);
            }
        }

        private static readonly (string Name, string Version) NameAndVersion
            = typeof(ISentryClient).Assembly.GetNameAndVersion();

        private void Populate(Exception exception, bool? isUnhandled)
        {
            Platform = "csharp";
            Sdk.Name = "Sentry.NET";
            Sdk.Version = NameAndVersion.Version;

            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }

                var asmName = assembly.GetName();
                builder[asmName.Name] = asmName.Version.ToString();
            }

            InternalModules = builder.ToImmutable();

            if (exception != null)
            {
                var sentryExceptions = CreateSentryException(exception, isUnhandled)
                    // Otherwise realization happens on the worker thread before sending event.
                    .ToList();

                var values = new SentryValues<SentryException>(sentryExceptions);

                foreach (var sentryException in sentryExceptions)
                {
                    var builderStrObj = ImmutableDictionary.CreateBuilder<string, object>();
                    foreach (string key in exception.Data.Keys)
                    {
                        builderStrObj[$"{sentryException.Type}.Data[{key}]"] = exception.Data[key];
                    }

                    Extra = builderStrObj.ToImmutable();
                }

                SentryExceptions = values;
            }
        }

        private static IEnumerable<SentryException> CreateSentryException(Exception exception, bool? isUnhandled)
        {
            Debug.Assert(exception != null);

            if (exception is AggregateException ae)
            {
                foreach (var inner in ae.InnerExceptions.SelectMany(e => CreateSentryException(e, null)))
                {
                    yield return inner;
                }
            }
            else if (exception.InnerException != null)
            {
                foreach (var inner in CreateSentryException(exception.InnerException, null))
                {
                    yield return inner;
                }
            }

            var sentryEx = new SentryException
            {
                Type = exception.GetType()?.FullName,
                Module = exception.GetType()?.Assembly?.FullName,
                Value = exception.Message,
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                Mechanism = GetMechanism(exception, isUnhandled)
            };

            if (exception.Data.Count != 0)
            {
                var builder = ImmutableDictionary.CreateBuilder<string, object>();
                foreach (string key in exception.Data.Keys)
                {
                    builder.Add(key, exception.Data[key]);
                }

                sentryEx.Data = builder.ToImmutable();
            }

            var stackTrace = new StackTrace(exception, true);

            // Sentry expects the frames to be sent in reversed order
            var frames = stackTrace.GetFrames()
                ?.Reverse()
                .Select(CreateSentryStackFrame)
                .ToList();

            if (frames != null)
            {
                sentryEx.Stacktrace = new SentryStackTrace
                {
                    Frames = frames
                };
            }

            yield return sentryEx;
        }

        public static Mechanism GetMechanism(Exception exception, bool? isUnhandled = null)
        {
            Debug.Assert(exception != null);

            Mechanism mechanism = null;

            if (exception.HelpLink != null)
            {
                mechanism = new Mechanism
                {
                    HelpLink = exception.HelpLink
                };
            }

            if (isUnhandled != null)
            {
                mechanism = new Mechanism
                {
                    Handled = !isUnhandled
                };
            }

            return mechanism;
        }

        public static SentryStackFrame CreateSentryStackFrame(StackFrame stackFrame)
        {
            const string unknownRequiredField = "(unknown)";

            var frame = new SentryStackFrame();

            if (stackFrame.HasMethod())
            {
                var method = stackFrame.GetMethod();

                // TODO: SentryStackFrame.TryParse and skip frame instead of these unknown values:
                frame.Module = method.DeclaringType?.FullName ?? unknownRequiredField;
                frame.Package = method.DeclaringType?.Assembly.FullName;
                frame.Function = method.Name;
                frame.ContextLine = method.ToString();
            }

            frame.InApp = !IsSystemModuleName(frame.Module);
            frame.FileName = stackFrame.GetFileName();

            if (stackFrame.HasILOffset())
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

            // TODO: Consider Ben.Demystifier (not on netcoreapp2.1+ which has by default)
            DemangleAsyncFunctionName(frame);
            DemangleAnonymousFunction(frame);

            return frame;

            // TODO: make this extensible
            bool IsSystemModuleName(string moduleName)
                => !string.IsNullOrEmpty(moduleName) &&
                      (moduleName.StartsWith("System.", StringComparison.Ordinal) ||
                       moduleName.StartsWith("Microsoft.", StringComparison.Ordinal));
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
        private static void DemangleAnonymousFunction(SentryStackFrame frame)
        {
            if (frame == null)
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
