namespace Sentry.Testing;

/// <summary>
/// To be used when we don't want the real network status to affect the reliability of our tests
/// </summary>
public class FakeReliableNetworkStatusListener : INetworkStatusListener
{
    public static readonly FakeReliableNetworkStatusListener Instance = new();

    private FakeReliableNetworkStatusListener()
    {
    }

    public bool Online => true;

    public Task WaitForNetworkOnlineAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
