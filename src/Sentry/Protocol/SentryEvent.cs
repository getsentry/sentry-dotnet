using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
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
    public class SentryEvent : Scope
    {
        [DataMember(Name = "modules", EmitDefaultValue = false)]
        internal IDictionary<string, string> InternalModules { get; private set; }

        [DataMember(Name = "event_id", EmitDefaultValue = false)]
        private string SerializableEventId => EventId.ToString("N");

        /// <summary>
        /// The unique identifier of this event
        /// </summary>
        /// <remarks>
        /// Hexadecimal string representing a uuid4 value.
        /// The length is exactly 32 characters (no dashes!)
        /// </remarks>
        public Guid EventId { get; set; }

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
        /// The name of the transaction (or culprit) which caused this exception.
        /// </summary>
        [DataMember(Name = "culprit", EmitDefaultValue = false)]
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
        /// The environment name, such as 'production' or 'staging'.
        /// </summary>
        /// <remarks>Requires Sentry 8.0 or higher</remarks>
        [DataMember(Name = "environment", EmitDefaultValue = false)]
        public string Environment { get; set; }

        /// <summary>
        /// A list of relevant modules and their versions.
        /// </summary>
        public IDictionary<string, string> Modules
        {
            get => InternalModules ?? (InternalModules = new Dictionary<string, string>());
            set => InternalModules = value;
        }

        /// <summary>
        /// Creates a Sentry event with optional Exception details and default values like Id and Timestamp
        /// </summary>
        /// <param name="exception">The exception.</param>
        public SentryEvent(Exception exception = null)
            : this(exception, SystemClock.Clock)
        { }

        internal SentryEvent(
            Exception exception = null,
            ISystemClock clock = null,
            Guid id = default)
        {
            Reset(this, clock, id);
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
                if (@event.Message == null)
                {
                    @event.Message = exception.Message;
                }

                // e.g: Namespace.Class.Method
                if (@event.Culprit == null)
                {
                    @event.Culprit = $"{exception.TargetSite?.ReflectedType?.FullName ?? "<unavailable>"}.{exception.TargetSite?.Name ?? "<unavailable>"}";
                }
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }
                var asmName = assembly.GetName();
                @event.Modules[asmName.Name] = asmName.Version.ToString();
            }
        }

        /// <summary>
        /// Resets the instance to a new value
        /// </summary>
        /// <param name="event"></param>
        public static void Reset(SentryEvent @event) => Reset(@event, SystemClock.Clock);

        internal static void Reset(SentryEvent @event, ISystemClock clock, Guid id = default)
        {
            Debug.Assert(@event != null);
            Debug.Assert(clock != null);

            // Set initial value to mandatory properties:
            //@event.InternalContexts = // TODO: Load initial context data

            @event.EventId = id == default ? Guid.NewGuid() : id;
            @event.Timestamp = clock.GetUtcNow();

            // TODO: should this be dotnet instead?
            @event.Platform = "csharp";
            @event.Sdk.Name = "Sentry.NET";
            // TODO: Read it off of env var here? Integration's version could be set instead
            // Less flexible than using SentryOptions to define this value
            @event.Sdk.Version = "0.0.0";

        }
    }
}
