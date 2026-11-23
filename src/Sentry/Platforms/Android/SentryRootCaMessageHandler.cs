using Java.Security;
using Javax.Net.Ssl;
using Sentry.Extensibility;
using Xamarin.Android.Net;
using JavaX509Certificate = Java.Security.Cert.X509Certificate;

namespace Sentry.Android;

/// <summary>
/// An <see cref="AndroidMessageHandler"/> for the SDK's own uploads to sentry.io that trusts the system root CAs plus
/// <see cref="SentryRootCertificates"/>. Chain validation is still done by the platform's default trust manager.
/// </summary>
/// <remarks>
/// It overrides <see cref="ConfigureCustomSSLSocketFactory"/> and not <c>ConfigureTrustManagerFactory</c>: the latter
/// makes AndroidMessageHandler build a new SSLContext for every request, and since OkHttp pools connections per socket
/// factory, that would rule out connection reuse and TLS session resumption.
/// </remarks>
internal sealed class SentryRootCaMessageHandler : AndroidMessageHandler
{
    internal const string SystemCaAliasPrefix = "system-ca-";
    internal const string SentryRootCaAliasPrefix = "sentry-root-ca-";

    private static SSLSocketFactory? CachedSocketFactory;
    private static bool SocketFactoryInitialized;
    private static object? SocketFactoryLock;

    private readonly IDiagnosticLogger? _logger;

    public SentryRootCaMessageHandler(IDiagnosticLogger? logger)
    {
        _logger = logger;
    }

    protected override SSLSocketFactory? ConfigureCustomSSLSocketFactory(HttpsURLConnection connection)
        => GetSocketFactory(_logger);

    internal static SSLSocketFactory? GetSocketFactory(IDiagnosticLogger? logger)
        => LazyInitializer.EnsureInitialized(
            ref CachedSocketFactory,
            ref SocketFactoryInitialized,
            ref SocketFactoryLock,
            () => CreateSocketFactory(() => [SentryRootCertificates.LoadIsrgRootX1()], logger));

    internal static SSLSocketFactory? CreateSocketFactory(
        Func<IEnumerable<JavaX509Certificate>> extraRoots,
        IDiagnosticLogger? logger)
    {
        try
        {
            var trustStore = CreateTrustStore(GetSystemTrustedIssuers(), extraRoots());

            var trustManagerFactory = TrustManagerFactory.GetInstance(TrustManagerFactory.DefaultAlgorithm)
                                      ?? throw new InvalidOperationException("No default TrustManagerFactory available.");
            trustManagerFactory.Init(trustStore);

            var sslContext = SSLContext.GetInstance("TLS")
                             ?? throw new InvalidOperationException("No TLS SSLContext available.");
            sslContext.Init(null, trustManagerFactory.GetTrustManagers(), null);
            return sslContext.SocketFactory
                   ?? throw new InvalidOperationException("The SSLContext has no socket factory.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to trust the bundled Sentry root CAs. Using the system default TLS configuration.");
            return null;
        }
    }

    internal static JavaX509Certificate[] GetSystemTrustedIssuers()
    {
        var trustManagerFactory = TrustManagerFactory.GetInstance(TrustManagerFactory.DefaultAlgorithm)
                                  ?? throw new InvalidOperationException("No default TrustManagerFactory available.");
        trustManagerFactory.Init((KeyStore?)null);

        foreach (var trustManager in trustManagerFactory.GetTrustManagers() ?? [])
        {
            if (AsX509TrustManager(trustManager) is { } x509TrustManager)
            {
                return x509TrustManager.GetAcceptedIssuers() ?? [];
            }
        }

        throw new InvalidOperationException("No default X509TrustManager available.");
    }

    private static IX509TrustManager? AsX509TrustManager(ITrustManager trustManager)
    {
        try
        {
            return trustManager.JavaCast<IX509TrustManager>();
        }
        catch (InvalidCastException)
        {
            return null;
        }
    }

    internal static KeyStore CreateTrustStore(
        IEnumerable<JavaX509Certificate> systemTrustedIssuers,
        IEnumerable<JavaX509Certificate> extraRoots)
    {
        var keyStore = KeyStore.GetInstance(KeyStore.DefaultType)
                       ?? throw new InvalidOperationException("No default KeyStore available.");
        keyStore.Load(null, null);

        var index = 0;
        foreach (var certificate in systemTrustedIssuers)
        {
            keyStore.SetCertificateEntry(SystemCaAliasPrefix + index++, certificate);
        }

        index = 0;
        foreach (var certificate in extraRoots)
        {
            keyStore.SetCertificateEntry(SentryRootCaAliasPrefix + index++, certificate);
        }

        return keyStore;
    }
}
