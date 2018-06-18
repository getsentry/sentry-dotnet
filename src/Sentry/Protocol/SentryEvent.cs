using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using Sentry.Infrastructure;
using Sentry.Protocol;

// ReSharper disable once CheckNamespace
namespace Sentry
{
    /// <summary>
    /// An event to be sent to Sentry
    /// </summary>
    /// <seealso href="https://docs.sentry.io/clientdev/attributes/"/>
    [DataContract]
    [DebuggerDisplay("{" + nameof(Message) + "}")]
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
            Guid id = default)
        {
            clock = clock ?? SystemClock.Clock;
            EventId = id == default ? Guid.NewGuid() : id;

            Timestamp = clock.GetUtcNow();

            // TODO: should this be dotnet instead?
            Platform = "csharp";
            Sdk.Name = "Sentry.NET";
            // TODO: Read it off of env var here? Integration's version could be set instead
            // Less flexible than using SentryOptions to define this value
            Sdk.Version = "0.0.0";

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

            Populate(this, exception);
        }

        /// <summary>
        /// Populate fields from Exception
        /// </summary>
        /// <param name="event">The event.</param>
        /// <param name="exception">The exception.</param>
        public static void Populate(SentryEvent @event, Exception exception)
        {
            if (exception != null)
            {
                // TODO: Aggregate exception
                var sentryEx = new SentryException
                {
                    Type = exception.GetType()?.FullName,
                    Module = exception.GetType()?.Assembly?.FullName,
                    Value = exception.Message,
                    ThreadId = Thread.CurrentThread.ManagedThreadId,
                };

                var stackTrace = new StackTrace(exception, true);

                // Sentry expects the frames to be sent in reversed order
                var frames = stackTrace.GetFrames()?
                    .Reverse()
                    .Select(CreateSentryStackFrame);

                if (frames != null)
                {
                    sentryEx.Stacktrace = new SentryStackTrace
                    {
                        Frames = frames
                    };
                }

                var values = new SentryValues<SentryException>(sentryEx);
                @event.SentryExceptions = values;
            }
        }

        public static SentryStackFrame CreateSentryStackFrame(StackFrame stackFrame)
        {
            const string unknownRequiredField = "(unknown)";

            var frame = new SentryStackFrame();
            var method = stackFrame.GetMethod();

            // TODO: TryParse and skip frame instead of these unknown values:
            frame.Module = method?.DeclaringType?.FullName ?? unknownRequiredField;
            frame.Function = method?.Name ?? unknownRequiredField;
            frame.ContextLine = method?.ToString();

            frame.InApp = IsInApp(frame.Module);
            frame.FileName = stackFrame.GetFileName();

            var lineNo = stackFrame.GetFileLineNumber();
            if (lineNo != 0)
            {
                frame.LineNumber = lineNo;
            }

            var ilOffset = stackFrame.GetILOffset();
            if (ilOffset != 0)
            {
                frame.InstructionOffset = ilOffset;
            }

            var colNo = stackFrame.GetFileColumnNumber();
            if (lineNo != 0)
            {
                frame.ColumnNumber = colNo;
            }

            return frame;

            // TODO: make this extensible
            bool IsInApp(string moduleName)
                => string.IsNullOrEmpty(moduleName)
                   || !moduleName.StartsWith("System.", StringComparison.Ordinal)
                   && !moduleName.StartsWith("Microsoft.", StringComparison.Ordinal);
        }
    }
}
