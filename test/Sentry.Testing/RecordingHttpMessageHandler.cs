namespace Sentry.Testing;

public class RecordingHttpMessageHandler : DelegatingHandler
{
    private readonly List<HttpRequestMessage> _requests = new();

    public RecordingHttpMessageHandler() { }

    public RecordingHttpMessageHandler(HttpMessageHandler innerHandler) => InnerHandler = innerHandler;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requests.Add(await request.CloneAsync(cancellationToken));
        return await base.SendAsync(request, cancellationToken);
    }

#if NET5_0_OR_GREATER
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requests.Add(request.Clone(cancellationToken));
        return base.Send(request, cancellationToken);
    }
#endif

    public IReadOnlyList<HttpRequestMessage> GetRequests() => _requests.ToArray();

    protected override void Dispose(bool disposing)
    {
        _requests.DisposeAll();
        base.Dispose(disposing);
    }
}
