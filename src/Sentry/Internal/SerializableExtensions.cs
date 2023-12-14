using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal;

internal static class SerializableExtensions
{
    public static async Task<string> SerializeToStringAsync(
        this ISerializable serializable,
        IDiagnosticLogger logger,
        ISystemClock? clock = null,
        CancellationToken cancellationToken = default)
    {
        var stream = new MemoryStream();
#if NETFRAMEWORK || NETSTANDARD2_0
        using (stream)
#else
        await using (stream.ConfigureAwait(false))
#endif
        {
            if (clock != null && serializable is Envelope envelope)
            {
                await envelope.SerializeAsync(stream, logger, clock, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await serializable.SerializeAsync(stream, logger, cancellationToken).ConfigureAwait(false);
            }

            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync().ConfigureAwait(false);
        }
    }

    public static string SerializeToString(
        this ISerializable serializable,
        IDiagnosticLogger logger,
        ISystemClock? clock = null)
    {
        using var stream = new MemoryStream();

        if (clock != null && serializable is Envelope envelope)
        {
            envelope.Serialize(stream, logger, clock);
        }
        else
        {
            serializable.Serialize(stream, logger);
        }

        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
