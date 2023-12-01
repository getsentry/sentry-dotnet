using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.iOS.Facades;

internal class SerializableNSObject : ISerializable
{
    private readonly NSObject _value;

    public SerializableNSObject(NSObject value)
    {
        _value = value;
    }

    public Task SerializeAsync(Stream stream, IDiagnosticLogger? logger, CancellationToken cancellationToken = default)
        => Task.Run(() => Serialize(stream, logger)); // TODO do we need a better implementation?

    public void Serialize(Stream stream, IDiagnosticLogger? logger) {
        // For types that implement Sentry Cocoa's SentrySerializable protocol (interface),
        // We should call that first, and then serialize the result to JSON later.
        var obj = _value is CocoaSdk.ISentrySerializable serializable
            ? serializable.Serialize()
            : _value;

        // Now we will use Apple's JSON Serialization functions.
        // See https://developer.apple.com/documentation/foundation/nsjsonserialization
        // TODO can we pipe NSOutputStream directly? It can be passed as a second argument
        // TODO how do we check if the error happened? Is it non-null? Then we can rethrow as NSErrorException?
        var data = NSJsonSerialization.Serialize(obj, 0, out NSError error);
        data.AsStream().CopyTo(stream);
    }
}
