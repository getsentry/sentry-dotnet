namespace Sentry.Internal;

internal interface IPing
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
}

internal class TcpPing(string hostToCheck, int portToCheck = 443) : IPing
{
    private readonly Ping _ping = new();

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var tcpClient = new TcpClient();
#if NET5_0_OR_GREATER
            await tcpClient.ConnectAsync(hostToCheck, portToCheck, cancellationToken).ConfigureAwait(false);
#else
            await tcpClient.ConnectAsync(hostToCheck, portToCheck).ConfigureAwait(false);
#endif
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
