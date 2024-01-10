using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Protocol.Metrics;

namespace Sentry.Protocol.Envelopes;

/// <summary>
/// Envelope item.
/// </summary>
public sealed class EnvelopeItem : ISerializable, IDisposable
{
    private const string TypeKey = "type";

    private const string TypeValueEvent = "event";
    private const string TypeValueUserReport = "user_report";
    private const string TypeValueTransaction = "transaction";
    private const string TypeValueSession = "session";
    private const string TypeValueAttachment = "attachment";
    private const string TypeValueClientReport = "client_report";
    private const string TypeValueProfile = "profile";
    private const string TypeValueMetric = "statsd";
    private const string TypeValueCodeLocations = "metric_meta";

    private const string LengthKey = "length";
    private const string FileNameKey = "filename";

    /// <summary>
    /// Header associated with this envelope item.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Header { get; }

    /// <summary>
    /// Item payload.
    /// </summary>
    public ISerializable Payload { get; }

    internal DataCategory DataCategory => TryGetType() switch
    {
        // Yes, the "event" item type corresponds to the "error" data category
        TypeValueEvent => DataCategory.Error,

        // These ones are equivalent
        TypeValueTransaction => DataCategory.Transaction,
        TypeValueSession => DataCategory.Session,
        TypeValueAttachment => DataCategory.Attachment,
        TypeValueProfile => DataCategory.Profile,

        // Not all envelope item types equate to data categories
        // Specifically, user_report and client_report just use "default"
        _ => DataCategory.Default
    };

    /// <summary>
    /// Initializes an instance of <see cref="EnvelopeItem"/>.
    /// </summary>
    public EnvelopeItem(IReadOnlyDictionary<string, object?> header, ISerializable payload)
    {
        Header = header;
        Payload = payload;
    }

    /// <summary>
    /// Tries to get item type.
    /// </summary>
    public string? TryGetType() => Header.GetValueOrDefault(TypeKey) as string;

    /// <summary>
    /// Tries to get payload length.
    /// </summary>
    public long? TryGetLength() =>
        Header.GetValueOrDefault(LengthKey) switch
        {
            null => null,
            var value => Convert.ToInt64(value) // can be int, long, or another numeric type
        };

    /// <summary>
    /// Returns the file name or null if no name exists.
    /// </summary>
    /// <returns>The file name or null.</returns>
    public string? TryGetFileName() => Header.GetValueOrDefault(FileNameKey) as string;

    private async Task<MemoryStream> BufferPayloadAsync(IDiagnosticLogger? logger, CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();

        if (Payload is JsonSerializable jsonSerializable)
        {
            // There's no advantage to buffer fully-materialized in-memory objects asynchronously,
            // and there's some minor overhead in doing so.  Thus we will serialize synchronously.

            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
            jsonSerializable.Serialize(buffer, logger);
        }
        else
        {
            await Payload.SerializeAsync(buffer, logger, cancellationToken).ConfigureAwait(false);
        }

        buffer.Seek(0, SeekOrigin.Begin);
        return buffer;
    }

    private MemoryStream BufferPayload(IDiagnosticLogger? logger)
    {
        var buffer = new MemoryStream();
        Payload.Serialize(buffer, logger);
        buffer.Seek(0, SeekOrigin.Begin);

        return buffer;
    }

    private static async Task SerializeHeaderAsync(
        Stream stream,
        IReadOnlyDictionary<string, object?> header,
        IDiagnosticLogger? logger,
        CancellationToken cancellationToken)
    {
        var writer = new Utf8JsonWriter(stream);
        await using (writer.ConfigureAwait(false))
        {
            writer.WriteDictionaryValue(header, logger);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static void SerializeHeader(
        Stream stream,
        IReadOnlyDictionary<string, object?> header,
        IDiagnosticLogger? logger)
    {
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteDictionaryValue(header, logger);
        writer.Flush();
    }

    /// <inheritdoc />
    public async Task SerializeAsync(Stream stream, IDiagnosticLogger? logger,
        CancellationToken cancellationToken = default)
    {
        // Always calculate the length of the payload, as Sentry will reject envelopes that have incorrect lengths
        // in item headers. Don't trust any previously calculated value to be correct.
        // See https://github.com/getsentry/sentry-dotnet/issues/1956

        var payloadBuffer = await BufferPayloadAsync(logger, cancellationToken).ConfigureAwait(false);
#if NETFRAMEWORK || NETSTANDARD2_0
        using (payloadBuffer)
#else
        await using (payloadBuffer.ConfigureAwait(false))
#endif
        {
            // Write to the outbound stream asynchronously. It's likely either an HttpRequestStream or a FileStream.

            // Header
            var headerWithLength = Header.ToDict();
            headerWithLength[LengthKey] = payloadBuffer.Length;
            await SerializeHeaderAsync(stream, headerWithLength, logger, cancellationToken).ConfigureAwait(false);
            await stream.WriteNewlineAsync(cancellationToken).ConfigureAwait(false);

            // Payload
            await payloadBuffer.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Serialize(Stream stream, IDiagnosticLogger? logger)
    {
        // Always calculate the length of the payload, as Sentry will reject envelopes that have incorrect lengths
        // in item headers. Don't trust any previously calculated value to be correct.
        // See https://github.com/getsentry/sentry-dotnet/issues/1956

        using var payloadBuffer = BufferPayload(logger);

        // Header
        var headerWithLength = Header.ToDict();
        headerWithLength[LengthKey] = payloadBuffer.Length;
        SerializeHeader(stream, headerWithLength, logger);
        stream.WriteNewline();

        // Payload
        payloadBuffer.CopyTo(stream);
    }

    /// <inheritdoc />
    public void Dispose() => (Payload as IDisposable)?.Dispose();

    /// <summary>
    /// Creates an <see cref="EnvelopeItem"/> from <paramref name="event"/>.
    /// </summary>
    public static EnvelopeItem FromEvent(SentryEvent @event)
    {
        var header = new Dictionary<string, object?>(1, StringComparer.Ordinal)
        {
            [TypeKey] = TypeValueEvent
        };

        return new EnvelopeItem(header, new JsonSerializable(@event));
    }

    /// <summary>
    /// Creates an <see cref="EnvelopeItem"/> from <paramref name="sentryUserFeedback"/>.
    /// </summary>
    public static EnvelopeItem FromUserFeedback(UserFeedback sentryUserFeedback)
    {
        var header = new Dictionary<string, object?>(1, StringComparer.Ordinal)
        {
            [TypeKey] = TypeValueUserReport
        };

        return new EnvelopeItem(header, new JsonSerializable(sentryUserFeedback));
    }

    /// <summary>
    /// Creates an <see cref="EnvelopeItem"/> from <paramref name="transaction"/>.
    /// </summary>
    public static EnvelopeItem FromTransaction(Transaction transaction)
    {
        var header = new Dictionary<string, object?>(1, StringComparer.Ordinal)
        {
            [TypeKey] = TypeValueTransaction
        };

        return new EnvelopeItem(header, new JsonSerializable(transaction));
    }

    /// <summary>
    /// Creates an <see cref="EnvelopeItem"/> from one or more <paramref name="codeLocations"/>.
    /// </summary>
    internal static EnvelopeItem FromCodeLocations(CodeLocations codeLocations)
    {
        var header = new Dictionary<string, object?>(1, StringComparer.Ordinal)
        {
            [TypeKey] = TypeValueCodeLocations
        };

        // Note that metrics are serialized using statsd encoding (not JSON)
        return new EnvelopeItem(header, new JsonSerializable(codeLocations));
    }

    /// <summary>
    /// Creates an <see cref="EnvelopeItem"/> from <paramref name="metric"/>.
    /// </summary>
    internal static EnvelopeItem FromMetric(Metric metric)
    {
        var header = new Dictionary<string, object?>(1, StringComparer.Ordinal)
        {
            [TypeKey] = TypeValueMetric
        };

        // Note that metrics are serialized using statsd encoding (not JSON)
        return new EnvelopeItem(header, metric);
    }

    /// <summary>
    /// Creates an <see cref="EnvelopeItem"/> from <paramref name="source"/>.
    /// </summary>
    internal static EnvelopeItem FromProfileInfo(ISerializable source)
    {
        var header = new Dictionary<string, object?>(1, StringComparer.Ordinal)
        {
            [TypeKey] = TypeValueProfile
        };

        return new EnvelopeItem(header, source);
    }

    /// <summary>
    /// Creates an <see cref="EnvelopeItem"/> from <paramref name="sessionUpdate"/>.
    /// </summary>
    public static EnvelopeItem FromSession(SessionUpdate sessionUpdate)
    {
        var header = new Dictionary<string, object?>(1, StringComparer.Ordinal)
        {
            [TypeKey] = TypeValueSession
        };

        return new EnvelopeItem(header, new JsonSerializable(sessionUpdate));
    }

    /// <summary>
    /// Creates an <see cref="EnvelopeItem"/> from <paramref name="attachment"/>.
    /// </summary>
    public static EnvelopeItem FromAttachment(Attachment attachment)
    {
        var stream = attachment.Content.GetStream();
        return FromAttachment(attachment, stream);
    }

    internal static EnvelopeItem FromAttachment(Attachment attachment, Stream stream)
    {
        var attachmentType = attachment.Type switch
        {
            AttachmentType.Minidump => "event.minidump",
            AttachmentType.AppleCrashReport => "event.applecrashreport",
            AttachmentType.UnrealContext => "unreal.context",
            AttachmentType.UnrealLogs => "unreal.logs",
            AttachmentType.ViewHierarchy => "event.view_hierarchy",
            _ => "event.attachment"
        };

        var header = new Dictionary<string, object?>(5, StringComparer.Ordinal)
        {
            [TypeKey] = TypeValueAttachment,
            [LengthKey] = stream.TryGetLength(),
            [FileNameKey] = attachment.FileName,
            ["attachment_type"] = attachmentType,
            ["content_type"] = attachment.ContentType
        };

        return new EnvelopeItem(header, new StreamSerializable(stream));
    }

    /// <summary>
    /// Creates an <see cref="EnvelopeItem"/> from <paramref name="report"/>.
    /// </summary>
    internal static EnvelopeItem FromClientReport(ClientReport report)
    {
        var header = new Dictionary<string, object?>(1, StringComparer.Ordinal)
        {
            [TypeKey] = TypeValueClientReport
        };

        return new EnvelopeItem(header, new JsonSerializable(report));
    }

    private static async Task<Dictionary<string, object?>> DeserializeHeaderAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var buffer = await stream.ReadLineAsync(cancellationToken).ConfigureAwait(false);

        return
            Json.Parse(buffer, JsonExtensions.GetDictionaryOrNull)
            ?? throw new InvalidOperationException("Envelope item header is malformed.");
    }

    private static async Task<ISerializable> DeserializePayloadAsync(
        Stream stream,
        IReadOnlyDictionary<string, object?> header,
        CancellationToken cancellationToken = default)
    {
        var payloadLength = header.GetValueOrDefault(LengthKey) switch
        {
            null => (long?)null,
            var value => Convert.ToInt64(value)
        };

        var payloadType = header.GetValueOrDefault(TypeKey) as string;

        // Event
        if (string.Equals(payloadType, TypeValueEvent, StringComparison.OrdinalIgnoreCase))
        {
            var bufferLength = (int)(payloadLength ?? stream.Length);
            var buffer = await stream.ReadByteChunkAsync(bufferLength, cancellationToken).ConfigureAwait(false);
            var sentryEvent = Json.Parse(buffer, SentryEvent.FromJson);

            return new JsonSerializable(sentryEvent);
        }

        // User report
        if (string.Equals(payloadType, TypeValueUserReport, StringComparison.OrdinalIgnoreCase))
        {
            var bufferLength = (int)(payloadLength ?? stream.Length);
            var buffer = await stream.ReadByteChunkAsync(bufferLength, cancellationToken).ConfigureAwait(false);
            var userFeedback = Json.Parse(buffer, UserFeedback.FromJson);

            return new JsonSerializable(userFeedback);
        }

        // Transaction
        if (string.Equals(payloadType, TypeValueTransaction, StringComparison.OrdinalIgnoreCase))
        {
            var bufferLength = (int)(payloadLength ?? stream.Length);
            var buffer = await stream.ReadByteChunkAsync(bufferLength, cancellationToken).ConfigureAwait(false);
            var transaction = Json.Parse(buffer, Transaction.FromJson);

            return new JsonSerializable(transaction);
        }

        // Session
        if (string.Equals(payloadType, TypeValueSession, StringComparison.OrdinalIgnoreCase))
        {
            var bufferLength = (int)(payloadLength ?? stream.Length);
            var buffer = await stream.ReadByteChunkAsync(bufferLength, cancellationToken).ConfigureAwait(false);
            var sessionUpdate = Json.Parse(buffer, SessionUpdate.FromJson);

            return new JsonSerializable(sessionUpdate);
        }

        // Client Report
        if (string.Equals(payloadType, TypeValueClientReport, StringComparison.OrdinalIgnoreCase))
        {
            var bufferLength = (int)(payloadLength ?? stream.Length);
            var buffer = await stream.ReadByteChunkAsync(bufferLength, cancellationToken).ConfigureAwait(false);
            var clientReport = Json.Parse(buffer, ClientReport.FromJson);

            return new JsonSerializable(clientReport);
        }

        // Arbitrary payload
        var payloadStream = new PartialStream(stream, stream.Position, payloadLength);

        if (payloadLength is not null)
        {
            stream.Seek(payloadLength.Value, SeekOrigin.Current);
        }
        else
        {
            stream.Seek(0, SeekOrigin.End);
        }

        return new StreamSerializable(payloadStream);
    }

    /// <summary>
    /// Deserializes envelope item from stream.
    /// </summary>
    public static async Task<EnvelopeItem> DeserializeAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var header = await DeserializeHeaderAsync(stream, cancellationToken).ConfigureAwait(false);
        var payload = await DeserializePayloadAsync(stream, header, cancellationToken).ConfigureAwait(false);

        // Swallow trailing newlines (some envelopes may have them after payloads)
        await stream.SkipNewlinesAsync(cancellationToken).ConfigureAwait(false);

        // Always remove the length header on deserialization so it will get re-calculated if later serialized.
        // We cannot trust the length to be identical when round-tripped.
        // See https://github.com/getsentry/sentry-dotnet/issues/1956
        header.Remove(LengthKey);

        return new EnvelopeItem(header, payload);
    }
}
