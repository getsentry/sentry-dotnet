namespace Sentry.Internal;

internal interface IPingHost
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
}

internal class PingHost(string hostToCheck) : IPingHost
{
    private readonly Ping _ping = new();

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        var reply = await _ping.SendPingAsync(hostToCheck).ConfigureAwait(false);
        return reply.Status == IPStatus.Success;
    }
}
