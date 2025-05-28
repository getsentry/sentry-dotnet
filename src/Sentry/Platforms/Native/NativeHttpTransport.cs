namespace Sentry.Native;

/// <summary>
/// A custom transport for `sentry-native`.
/// </summary>
/// <see href="https://github.com/getsentry/sentry-native"/>
internal class NativeHttpTransport
{
    private readonly SentryOptions _options;

    private readonly HttpClient _httpClient;

    public NativeHttpTransport(SentryOptions options, HttpClient httpClient)
    {
        _options = options;
        _httpClient = httpClient;
    }

    public void SendData(IntPtr data, uint size)
    {
        using var request = CreateRequest(data, size);
        using var response = _httpClient.Send(request);
        response.EnsureSuccessStatusCode();
    }

    // TODO: copied from HttpTransportBase.cs
    private HttpRequestMessage CreateRequest(IntPtr data, uint size)
    {
        if (string.IsNullOrWhiteSpace(_options.Dsn))
        {
            throw new InvalidOperationException("The DSN is expected to be set at this point.");
        }

        var dsn = Dsn.Parse(_options.Dsn);
        var authHeader =
            $"Sentry sentry_version={_options.SentryVersion}," +
            $"sentry_client={SdkVersion.Instance.Name}/{SdkVersion.Instance.Version}," +
            $"sentry_key={dsn.PublicKey}" +
            (dsn.SecretKey is { } secretKey ? $",sentry_secret={secretKey}" : null);

        return new HttpRequestMessage
        {
            RequestUri = dsn.GetEnvelopeEndpointUri(),
            Method = HttpMethod.Post,
            Headers = { { "X-Sentry-Auth", authHeader } },
            Content = new StringContent(Marshal.PtrToStringAnsi(data, (int)size))
        };
    }
}
