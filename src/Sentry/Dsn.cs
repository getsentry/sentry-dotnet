namespace Sentry;

/// <summary>
/// The Data Source Name of a given project in Sentry.
/// </summary>
/// <remarks>
/// <see href="https://develop.sentry.dev/sdk/overview/#parsing-the-dsn"/>
/// </remarks>
internal sealed class Dsn
{
    /// <summary>
    /// Source DSN string.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// The project ID which the authenticated user is bound to.
    /// </summary>
    public string ProjectId { get; }

    /// <summary>
    /// An optional path of which Sentry is hosted.
    /// </summary>
    public string? Path { get; }

    /// <summary>
    /// The optional secret key to authenticate the SDK.
    /// </summary>
    public string? SecretKey { get; }

    /// <summary>
    /// The required public key to authenticate the SDK.
    /// </summary>
    public string PublicKey { get; }

    /// <summary>
    /// Sentry API's base URI.
    /// </summary>
    private Uri ApiBaseUri { get; }

    private Dsn(
        string source,
        string projectId,
        string? path,
        string? secretKey,
        string publicKey,
        Uri apiBaseUri)
    {
        Source = source;
        ProjectId = projectId;
        Path = path;
        SecretKey = secretKey;
        PublicKey = publicKey;
        ApiBaseUri = apiBaseUri;
    }

    public Uri GetStoreEndpointUri() => new(ApiBaseUri, "store/");

    public Uri GetEnvelopeEndpointUri() => new(ApiBaseUri, "envelope/");

    public override string ToString() => Source;

    public static bool IsDisabled(string? dsn) =>
        SentryConstants.DisableSdkDsnValue.Equals(dsn, StringComparison.OrdinalIgnoreCase);

    public static Dsn Parse(string dsn)
    {
        var uri = new Uri(dsn);

        // uri.UserInfo returns empty string instead of null when no user info data is provided
        if (string.IsNullOrWhiteSpace(uri.UserInfo))
        {
            throw new ArgumentException("Invalid DSN: No public key provided.");
        }

        var keys = uri.UserInfo.Split(':');

        var publicKey = keys[0];
        if (string.IsNullOrWhiteSpace(publicKey))
        {
            throw new ArgumentException("Invalid DSN: No public key provided.");
        }

        var secretKey = keys.Length > 1
            ? keys[1]
            : null;

        var path = uri.AbsolutePath[..uri.AbsolutePath.LastIndexOf('/')];

        var projectId = uri.AbsoluteUri[(uri.AbsoluteUri.LastIndexOf('/') + 1)..];
        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new ArgumentException("Invalid DSN: A Project Id is required.");
        }

        var apiBaseUri = new UriBuilder
        {
            Scheme = uri.Scheme,
            Host = uri.DnsSafeHost,
            Port = uri.Port,
            Path = $"{path}/api/{projectId}/"
        }.Uri;

        return new Dsn(
            dsn,
            projectId,
            path,
            secretKey,
            publicKey,
            apiBaseUri);
    }

    public static Dsn? TryParse(string? dsn)
    {
        if (string.IsNullOrWhiteSpace(dsn))
        {
            return null;
        }

        try
        {
            return Parse(dsn);
        }
        catch
        {
            // Parse should not throw though!
            return null;
        }
    }
}
