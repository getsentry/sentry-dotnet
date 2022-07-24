using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;

namespace Sentry.Protocol.Envelopes
{
    /// <summary>
    /// Represents an object which is already serialized as a stream.
    /// </summary>
    internal sealed class StreamSerializable : ISerializable, IDisposable
    {
        /// <summary>
        /// Source stream.
        /// </summary>
        public Stream Source { get; }

        /// <summary>
        /// Initializes an instance of <see cref="StreamSerializable"/>.
        /// </summary>
        public StreamSerializable(Stream source) => Source = source;

        /// <inheritdoc />
        public Task SerializeAsync(Stream stream, IDiagnosticLogger? logger, CancellationToken cancellationToken = default) =>
            Source.CopyToAsync(stream, cancellationToken);

        /// <inheritdoc />
        public void Serialize(Stream stream, IDiagnosticLogger? logger) => Source.CopyTo(stream);

        /// <inheritdoc />
        public void Dispose() => Source.Dispose();
    }
}
