namespace Sentry.Testing;

public class EmptySerializable : ISerializable
{
    public Task SerializeAsync(Stream stream, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
