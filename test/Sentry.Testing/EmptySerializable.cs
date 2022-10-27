namespace Sentry.Testing;

public class EmptySerializable : ISerializable
{
    public Task SerializeAsync(Stream stream, IDiagnosticLogger logger, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Serialize(Stream stream, IDiagnosticLogger logger) { }
}
