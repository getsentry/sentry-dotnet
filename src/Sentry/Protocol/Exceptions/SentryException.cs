using System;
using System.Collections.Generic;
using System.Text.Json;
using Sentry.Internal.Extensions;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol
{
    /// <summary>
    /// Sentry Exception interface.
    /// </summary>
    /// <see href="https://develop.sentry.dev/sdk/event-payloads/exception"/>
    public sealed class SentryException : IJsonSerializable
    {
        /// <summary>
        /// Exception Type.
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// The exception value.
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// The optional module, or package which the exception type lives in.
        /// </summary>
        public string? Module { get; set; }

        /// <summary>
        /// An optional value which refers to a thread in the threads interface.
        /// </summary>
        /// <seealso href="https://develop.sentry.dev/sdk/event-payloads/threads/"/>
        /// <seealso cref="SentryThread"/>
        public int ThreadId { get; set; }

        /// <summary>
        /// Stack trace.
        /// </summary>
        /// <see href="https://develop.sentry.dev/sdk/event-payloads/stacktrace/"/>
        public SentryStackTrace? Stacktrace { get; set; }

        /// <summary>
        /// An optional mechanism that created this exception.
        /// </summary>
        /// <see href="https://develop.sentry.dev/sdk/event-payloads/exception/#exception-mechanism"/>
        public Mechanism? Mechanism { get; set; }

        /// <summary>
        /// Arbitrary extra data that related to this error
        /// </summary>
        /// <remarks>
        /// The protocol does not yet support data at this level.
        /// For this reason this property is not serialized.
        /// The data is moved to the event level on Extra until such support is added
        /// </remarks>
        public IDictionary<string, object?> Data { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            // Type
            if (!string.IsNullOrWhiteSpace(Type))
            {
                writer.WriteString("type", Type);
            }

            // Value
            if (!string.IsNullOrWhiteSpace(Value))
            {
                writer.WriteString("value", Value);
            }

            // Module
            if (!string.IsNullOrWhiteSpace(Module))
            {
                writer.WriteString("module", Module);
            }

            // Thread ID
            if (ThreadId != default)
            {
                writer.WriteNumber("thread_id", ThreadId);
            }

            // Stack trace
            if (Stacktrace is {} stacktrace)
            {
                writer.WriteSerializable("stacktrace", stacktrace);
            }

            // Mechanism
            if (Mechanism is {} mechanism)
            {
                writer.WriteSerializable("mechanism", mechanism);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses from JSON.
        /// </summary>
        public static SentryException FromJson(JsonElement json)
        {
            var type = json.GetPropertyOrNull("type")?.GetString();
            var value = json.GetPropertyOrNull("value")?.GetString();
            var module = json.GetPropertyOrNull("module")?.GetString();
            var threadId = json.GetPropertyOrNull("thread_id")?.GetInt32() ?? 0;
            var stacktrace = json.GetPropertyOrNull("stacktrace")?.Pipe(SentryStackTrace.FromJson);
            var mechanism = json.GetPropertyOrNull("mechanism")?.Pipe(Mechanism.FromJson);

            return new SentryException
            {
                Type = type,
                Value = value,
                Module = module,
                ThreadId = threadId,
                Stacktrace = stacktrace,
                Mechanism = mechanism
            };
        }
    }
}
