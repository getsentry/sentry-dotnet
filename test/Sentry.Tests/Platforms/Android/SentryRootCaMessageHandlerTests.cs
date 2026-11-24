using System.Security.Cryptography;
using Sentry.Android;

namespace Sentry.Tests.Platforms.Android;

public class SentryRootCaMessageHandlerTests
{
    [Fact]
    public void LoadIsrgRootX1_Sha256Fingerprint_MatchesIsrgRootX1()
    {
        var certificate = SentryRootCertificates.LoadIsrgRootX1();

        var hash = SHA256.HashData(certificate.GetEncoded()!);

        Assert.Equal(
            "96:BC:EC:06:26:49:76:F3:74:60:77:9A:CF:28:C5:A7:CF:E8:A3:C0:AA:E1:1A:8F:FC:EE:05:C0:BD:DF:08:C6",
            string.Join(":", hash.Select(b => b.ToString("X2"))));
    }

    [Fact]
    public void CreateTrustManagers_NoSystemIssuersAndIsrgRootX1_AcceptsExactlyIsrgRootX1()
    {
        var isrgRootX1 = SentryRootCertificates.LoadIsrgRootX1();

        var trustManagers = SentryRootCaMessageHandler.CreateTrustManagers([], [isrgRootX1]);

        var x509TrustManager = trustManagers.Select(SentryRootCaMessageHandler.AsX509TrustManager).Single(tm => tm is not null)!;
        var acceptedIssuer = Assert.Single(x509TrustManager.GetAcceptedIssuers()!);
        Assert.Equal(isrgRootX1, acceptedIssuer);
    }

    [Fact]
    public void GetSocketFactory_CalledTwice_ReturnsSameInstance()
    {
        var first = SentryRootCaMessageHandler.GetSocketFactory(null);
        var second = SentryRootCaMessageHandler.GetSocketFactory(null);

        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    [Fact]
    public void CreateSocketFactory_RootSourceThrows_LogsErrorAndReturnsNull()
    {
        var logger = new InMemoryDiagnosticLogger();
        var exception = new InvalidOperationException("Test");

        var socketFactory = SentryRootCaMessageHandler.CreateSocketFactory(() => throw exception, logger);

        Assert.Null(socketFactory);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(SentryLevel.Error, entry.Level);
        Assert.Same(exception, entry.Exception);
    }
}
