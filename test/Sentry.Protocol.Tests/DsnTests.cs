using System;
using Xunit;

namespace Sentry.Protocol.Tests
{
    public class DsnTests
    {
        [Fact]
        public void ToString_SameAsInput()
        {
            var @case = new DsnTestCase();
            var dsn = new Dsn(@case);
            Assert.Equal(@case.ToString(), dsn.ToString());
        }

        [Fact]
        public void Ctor_SampleValidDsnWithoutSecret_CorrectlyConstructs()
        {
            var dsn = new Dsn(DsnSamples.ValidDsnWithoutSecret);
            Assert.Equal(DsnSamples.ValidDsnWithoutSecret, dsn.ToString());
        }

        [Fact]
        public void Ctor_SampleValidDsnWithSecret_CorrectlyConstructs()
        {
            var dsn = new Dsn(DsnSamples.ValidDsnWithSecret);
            Assert.Equal(DsnSamples.ValidDsnWithSecret, dsn.ToString());
        }

        [Fact]
        public void Ctor_NotUri_ThrowsUriFormatException()
        {
            var ex = Assert.Throws<UriFormatException>(() => new Dsn("Not a URI"));
            Assert.Equal("Invalid URI: The format of the URI could not be determined.", ex.Message);
        }

        [Fact]
        public void Ctor_DisableSdk_ThrowsUriFormatException()
        {
            var ex = Assert.Throws<UriFormatException>(() => new Dsn(Constants.DisableSdkDsnValue));
            Assert.Equal("Invalid URI: The URI is empty.", ex.Message);
        }

        [Fact]
        public void Ctor_ValidDsn_CorrectlyConstructs()
        {
            var @case = new DsnTestCase();
            var dsn = new Dsn(@case);

            AssertEqual(@case, dsn);
        }

        [Fact]
        public void Ctor_MissingScheme_ThrowsUriFormatException()
        {
            var @case = new DsnTestCase { Scheme = null };
            var ex = Assert.Throws<UriFormatException>(() => new Dsn(@case));
            Assert.Equal("Invalid URI: The format of the URI could not be determined.", ex.Message);
        }

        [Fact]
        public void Ctor_FutureScheme_ValidDsn()
        {
            var @case = new DsnTestCase { Scheme = "hypothetical" };
            var dsn = new Dsn(@case);
            AssertEqual(@case, dsn);
        }

        [Fact]
        public void Ctor_EmptyPath_ValidDsn()
        {
            var @case = new DsnTestCase { Path = string.Empty };
            var dsn = new Dsn(@case);
            AssertEqual(@case, dsn);
        }

        [Fact]
        public void Ctor_MissingSecretKey_GetterReturnsNull()
        {
            var @case = new DsnTestCase { SecretKey = null };
            var sut = new Dsn(@case);
            Assert.Null(sut.SecretKey);
        }

        [Fact]
        public void Ctor_MissingPublicKey_ThrowsArgumentException()
        {
            var @case = new DsnTestCase { PublicKey = null };
            var ex = Assert.Throws<ArgumentException>(() => new Dsn(@case));
            Assert.Equal("Invalid DSN: No public key provided.", ex.Message);
        }

        [Fact]
        public void Ctor_MissingPublicAndSecretKey_ThrowsArgumentException()
        {
            var @case = new DsnTestCase { PublicKey = null, SecretKey = null, UserInfoSeparator = null, CredentialSeparator = null };
            var ex = Assert.Throws<ArgumentException>(() => new Dsn(@case));
            Assert.Equal("Invalid DSN: No public key provided.", ex.Message);
        }

        [Fact]
        public void Ctor_MissingProjectId_ThrowsArgumentException()
        {
            var @case = new DsnTestCase { ProjectId = null };
            var ex = Assert.Throws<ArgumentException>(() => new Dsn(@case));
            Assert.Equal("Invalid DSN: A Project Id is required.", ex.Message);
        }

        [Fact]
        public void Ctor_InvalidPort_ThrowsUriFormatException()
        {
            var @case = new DsnTestCase { Port = -1 };
            var ex = Assert.Throws<UriFormatException>(() => new Dsn(@case));
            Assert.Equal("Invalid URI: Invalid port specified.", ex.Message);
        }

        [Fact]
        public void Ctor_InvalidHost_ThrowsUriFormatException()
        {
            var @case = new DsnTestCase { Host = null };
            var ex = Assert.Throws<UriFormatException>(() => new Dsn(@case));
            Assert.Equal("Invalid URI: The hostname could not be parsed.", ex.Message);
        }

        [Fact]
        public void Ctor_EmptyStringDsn_ThrowsUriFormatException()
        {
            var ex = Assert.Throws<UriFormatException>(() => new Dsn(string.Empty));
            Assert.Equal("Invalid URI: The URI is empty.", ex.Message);
        }

        [Fact]
        public void Ctor_NullDsn_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new Dsn(null));
        }

        [Fact]
        public void TryParse_SampleValidDsnWithoutSecret_Succeeds()
        {
            Assert.True(Dsn.TryParse(DsnSamples.ValidDsnWithoutSecret, out var dsn));
            Assert.NotNull(dsn);
        }

        [Fact]
        public void TryParse_SampleValidDsnWithSecret_Succeeds()
        {
            Assert.True(Dsn.TryParse(DsnSamples.ValidDsnWithSecret, out var dsn));
            Assert.NotNull(dsn);
        }

        [Fact]
        public void TryParse_SampleInvalidDsn_Fails()
        {
            Assert.False(Dsn.TryParse(DsnSamples.InvalidDsn, out var dsn));
            Assert.Null(dsn);
        }

        [Fact]
        public void TryParse_NotUri_Fails()
        {
            Assert.False(Dsn.TryParse("Not a URI", out var dsn));
            Assert.Null(dsn);
        }

        [Fact]
        public void TryParse_DisabledSdk_Fails()
        {
            Assert.False(Dsn.TryParse(Constants.DisableSdkDsnValue, out var dsn));
            Assert.Null(dsn);
        }

        [Fact]
        public void TryParse_ValidDsn_Succeeds()
        {
            var @case = new DsnTestCase();
            Assert.True(Dsn.TryParse(@case, out var dsn));

            AssertEqual(@case, dsn);
        }

        [Fact]
        public void TryParse_MissingScheme_Fails()
        {
            var @case = new DsnTestCase { Scheme = null };
            Assert.False(Dsn.TryParse(@case, out var dsn));
            Assert.Null(dsn);
        }

        [Fact]
        public void TryParse_FutureScheme_Succeeds()
        {
            var @case = new DsnTestCase { Scheme = "hypothetical" };
            Assert.True(Dsn.TryParse(@case, out var dsn));
            AssertEqual(@case, dsn);
        }

        [Fact]
        public void TryParse_EmptyPath_Succeeds()
        {
            var @case = new DsnTestCase { Path = string.Empty };
            Assert.True(Dsn.TryParse(@case, out var dsn));
            AssertEqual(@case, dsn);
        }

        [Fact]
        public void TryParse_MissingSecretKey_Succeeds()
        {
            var @case = new DsnTestCase { SecretKey = null };
            Assert.True(Dsn.TryParse(@case, out var dsn));
            AssertEqual(@case, dsn);
        }

        [Fact]
        public void TryParse_MissingPublicKey_Fails()
        {
            var @case = new DsnTestCase { PublicKey = null };
            Assert.False(Dsn.TryParse(@case, out var dsn));
            Assert.Null(dsn);
        }

        [Fact]
        public void TryParse_MissingPublicAndSecretKey_Fails()
        {
            var @case = new DsnTestCase { PublicKey = null, SecretKey = null, UserInfoSeparator = null, CredentialSeparator = null };
            Assert.False(Dsn.TryParse(@case, out var dsn));
            Assert.Null(dsn);
        }

        [Fact]
        public void TryParse_MissingProjectId_Fails()
        {
            var @case = new DsnTestCase { ProjectId = null };
            Assert.False(Dsn.TryParse(@case, out var dsn));
            Assert.Null(dsn);
        }

        [Fact]
        public void TryParse_InvalidPort_Fails()
        {
            var @case = new DsnTestCase { Port = -1 };
            Assert.False(Dsn.TryParse(@case, out var dsn));
            Assert.Null(dsn);
        }

        [Fact]
        public void TryParse_InvalidHost_Fails()
        {
            var @case = new DsnTestCase { Host = null };
            Assert.False(Dsn.TryParse(@case, out var dsn));
            Assert.Null(dsn);
        }

        [Fact]
        public void TryParse_EmptyStringDsn_ThrowsUriFormatException()
        {
            Assert.False(Dsn.TryParse(string.Empty, out var dsn));
            Assert.Null(dsn);
        }

        [Fact]
        public void TryParse_NullDsn_ThrowsArgumentNull()
        {
            Assert.False(Dsn.TryParse(null, out var dsn));
            Assert.Null(dsn);
        }

        [Fact]
        public void IsDisabled_ValidDsn_False() => Assert.False(Dsn.IsDisabled(DsnSamples.ValidDsnWithSecret));

        [Fact]
        public void IsDisabled_InvalidDsn_False() => Assert.False(Dsn.IsDisabled(DsnSamples.InvalidDsn));

        [Fact]
        public void IsDisabled_NullDsn_False() => Assert.False(Dsn.IsDisabled(null));

        [Fact]
        public void IsDisabled_DisabledDsn_True() => Assert.True(Dsn.IsDisabled(Constants.DisableSdkDsnValue));

        [Fact]
        public void IsDisabled_EmptyStringDsn_True() => Assert.True(Dsn.IsDisabled(string.Empty));

        private static readonly Random Rnd = new Random();

        private class DsnTestCase
        {
            public string Scheme { get; set; } = "https";
            public string PublicKey { get; set; } = Guid.NewGuid().ToString("N");
            public string SecretKey { get; set; } = Guid.NewGuid().ToString("N");
            public string Host { get; set; } = "sentry.io";
            public string Path { get; set; } = "/some-path";
            public int? Port { get; set; } = Rnd.Next(1, 65535);
            public string ProjectId { get; set; } = Rnd.Next().ToString();

            public string CredentialSeparator { private get; set; } = ":";
            public string UserInfoSeparator { private get; set; } = "@";
            // -> {PROTOCOL}://{PUBLIC_KEY}:{SECRET_KEY}@{HOST}/{PATH}{PROJECT_ID} <-
            public override string ToString()
                => $"{Scheme}://{PublicKey}{(SecretKey == null ? null : $"{CredentialSeparator}{SecretKey}")}{UserInfoSeparator}{Host}{(Port != null ? $":{Port}" : "")}{Path}/{ProjectId}";

            public static implicit operator string(DsnTestCase @case) => @case.ToString();
            public static implicit operator Uri(DsnTestCase @case) => new Uri($"{@case.Scheme}://{@case.Host}:{@case.Port}{@case.Path}/api/{@case.ProjectId}/store/");
        }

        private static void AssertEqual(DsnTestCase @case, Dsn dsn)
        {
            if (@case == null) throw new ArgumentNullException(nameof(@case));
            if (dsn == null) throw new ArgumentNullException(nameof(dsn));

            Assert.Equal(@case.Scheme, dsn.SentryUri.Scheme);
            Assert.Equal(@case.PublicKey, dsn.PublicKey);
            Assert.Equal(@case.SecretKey, dsn.SecretKey);
            Assert.Equal(@case.ProjectId, dsn.ProjectId);
            Assert.Equal(@case.Path, dsn.Path);
            Assert.Equal(@case.Host, dsn.SentryUri.Host);
            Assert.Equal(@case.Port, dsn.SentryUri.Port);

            Assert.Equal(@case, dsn.SentryUri);
        }
    }
}
