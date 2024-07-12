namespace Sentry.Internal;

internal interface IPing
{
    Task<bool> IsAvailableAsync();
}

internal class TcpPing(string hostToCheck, int portToCheck = 443) : IPing
{
    private readonly Ping _ping = new();

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(hostToCheck, portToCheck).ConfigureAwait(false);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
