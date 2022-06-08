using System.Text.Json;
using Sentry.Extensibility;

namespace Sentry.Android
{
    internal static class JavaExtensions
    {
        public static SentryId ToSentryId(this Java.Protocol.SentryId sentryId) =>
            new(Guid.Parse(sentryId.ToString()));

        public static SpanId ToSpanId(this Java.SpanId spanId) =>
            new(spanId.ToString());

        public static SentryEvent ToSentryEvent(this Java.SentryEvent sentryEvent, Java.SentryOptions javaOptions)
        {
            using var stream = new MemoryStream();
            using var streamWriter = new JavaOutputStreamWriter(stream);
            using var jsonWriter = new Java.JsonObjectWriter(streamWriter, javaOptions.MaxDepth);
            sentryEvent.Serialize(jsonWriter, javaOptions.Logger);
            jsonWriter.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            using var json = JsonDocument.Parse(stream);
            return SentryEvent.FromJson(json.RootElement, sentryEvent.Throwable);
        }

        public static Java.SentryEvent ToJavaSentryEvent(this SentryEvent sentryEvent, IDiagnosticLogger? logger, Java.SentryOptions javaOptions)
        {
            using var stream = new MemoryStream();
            using var jsonWriter = new Utf8JsonWriter(stream);
            sentryEvent.WriteTo(jsonWriter, logger);
            jsonWriter.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            using var streamReader = new JavaInputStreamReader(stream);
            using var jsonReader = new Java.JsonObjectReader(streamReader);
            using var deserializer = new Java.SentryEvent.Deserializer();
            return deserializer.Deserialize(jsonReader, javaOptions.Logger);
        }

        public static Breadcrumb ToBreadcrumb(this Java.Breadcrumb breadcrumb, Java.SentryOptions javaOptions)
        {
            using var stream = new MemoryStream();
            using var streamWriter = new JavaOutputStreamWriter(stream);
            using var jsonWriter = new Java.JsonObjectWriter(streamWriter, javaOptions.MaxDepth);
            breadcrumb.Serialize(jsonWriter, javaOptions.Logger);
            jsonWriter.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            using var json = JsonDocument.Parse(stream);
            return Breadcrumb.FromJson(json.RootElement);
        }

        public static Java.Breadcrumb ToJavaBreadcrumb(this Breadcrumb breadcrumb, IDiagnosticLogger? logger, Java.SentryOptions javaOptions)
        {
            using var stream = new MemoryStream();
            using var jsonWriter = new Utf8JsonWriter(stream);
            breadcrumb.WriteTo(jsonWriter, logger);
            jsonWriter.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            using var streamReader = new JavaInputStreamReader(stream);
            using var jsonReader = new Java.JsonObjectReader(streamReader);
            using var deserializer = new Java.Breadcrumb.Deserializer();
            return deserializer.Deserialize(jsonReader, javaOptions.Logger);
        }

        public static TransactionSamplingContext ToTransactionSamplingContext(this Java.SamplingContext context)
        {
            var tc = context.TransactionContext;
            var transactionContext = new TransactionContext(
                tc.SpanId.ToSpanId(),
                tc.ParentSpanId?.ToSpanId(),
                tc.TraceId.ToSentryId(),
                tc.Name,
                tc.Operation,
                tc.Description,
                (SpanStatus)tc.Status,
                tc.Sampled?.BooleanValue(),
                tc.ParentSampled?.BooleanValue());

            var customContext = context.CustomSamplingContext?
                .Data.ToDictionary(x => x.Key, x => (object?)x.Value)
                ?? new(capacity: 0);

            return new TransactionSamplingContext(transactionContext, customContext);
        }
    }
}
