using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using Sentry.Extensibility;
using JavaInputStreamReader = Java.IO.InputStreamReader;
using JavaOutputStreamWriter = Java.IO.OutputStreamWriter;

namespace Sentry.Android
{
    internal static class JavaExtensions
    {
        public static SentryLevel ToSentryLevel(this Java.SentryLevel level)
        {
            // note: switch doesn't work here because JNI enums are not constants
            if (level == Java.SentryLevel.Debug)
                return SentryLevel.Debug;
            if (level == Java.SentryLevel.Info)
                return SentryLevel.Info;
            if (level == Java.SentryLevel.Warning)
                return SentryLevel.Warning;
            if (level == Java.SentryLevel.Error)
                return SentryLevel.Error;
            if (level == Java.SentryLevel.Fatal)
                return SentryLevel.Fatal;

            throw new ArgumentOutOfRangeException(nameof(level), level, message: default);
        }

        public static Java.SentryLevel ToJavaSentryLevel(this SentryLevel level) =>
            level switch
            {
                SentryLevel.Debug => Java.SentryLevel.Debug!,
                SentryLevel.Info => Java.SentryLevel.Info!,
                SentryLevel.Warning => Java.SentryLevel.Warning!,
                SentryLevel.Error => Java.SentryLevel.Error!,
                SentryLevel.Fatal => Java.SentryLevel.Fatal!,
                _ => throw new ArgumentOutOfRangeException(nameof(level), level, message: default)
            };

        public static SentryId ToSentryId(this Java.Protocol.SentryId sentryId) =>
            new(Guid.Parse(sentryId.ToString()));

        public static SpanId ToSpanId(this Java.SpanId spanId) =>
            new(spanId.ToString());

        public static SpanStatus ToSpanStatus(this Java.SpanStatus status)
        {
            // note: switch doesn't work here because JNI enums are not constants
            if (status == Java.SpanStatus.Ok)
                return SpanStatus.Ok;
            if (status == Java.SpanStatus.DeadlineExceeded)
                return SpanStatus.DeadlineExceeded;
            if (status == Java.SpanStatus.Unauthenticated)
                return SpanStatus.Unauthenticated;
            if (status == Java.SpanStatus.PermissionDenied)
                return SpanStatus.PermissionDenied;
            if (status == Java.SpanStatus.NotFound)
                return SpanStatus.NotFound;
            if (status == Java.SpanStatus.ResourceExhausted)
                return SpanStatus.ResourceExhausted;
            if (status == Java.SpanStatus.InvalidArgument)
                return SpanStatus.InvalidArgument;
            if (status == Java.SpanStatus.Unimplemented)
                return SpanStatus.Unimplemented;
            if (status == Java.SpanStatus.Unavailable)
                return SpanStatus.Unavailable;
            if (status == Java.SpanStatus.InternalError)
                return SpanStatus.InternalError;
            if (status == Java.SpanStatus.UnknownError)
                return SpanStatus.UnknownError;
            if (status == Java.SpanStatus.Cancelled)
                return SpanStatus.Cancelled;
            if (status == Java.SpanStatus.AlreadyExists)
                return SpanStatus.AlreadyExists;
            if (status == Java.SpanStatus.FailedPrecondition)
                return SpanStatus.FailedPrecondition;
            if (status == Java.SpanStatus.Aborted)
                return SpanStatus.Aborted;
            if (status == Java.SpanStatus.OutOfRange)
                return SpanStatus.OutOfRange;
            if (status == Java.SpanStatus.DataLoss)
                return SpanStatus.DataLoss;

            throw new ArgumentOutOfRangeException(nameof(status), status, message: default);
        }

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
                tc.Status?.ToSpanStatus(),
                tc.Sampled?.BooleanValue(),
                tc.ParentSampled?.BooleanValue());

            var customContext = context.CustomSamplingContext?
                .Data.ToDictionary(x => x.Key, x => (object?)x.Value)
                ?? new(capacity: 0);

            return new TransactionSamplingContext(transactionContext, customContext);
        }
    }
}
