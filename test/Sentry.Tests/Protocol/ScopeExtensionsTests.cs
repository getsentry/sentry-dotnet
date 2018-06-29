using System.Collections.Immutable;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Protocol
{
    public class ScopeExtensionsTests
    {
        private Scope _sut = new Scope();

        [Fact]
        public void CopyTo_Sdk_DoesNotCopyNameWithoutVersion()
        {
            const string expectedName = "original name";
            const string expectedVersion = "original version";
            _sut.Sdk.Name = expectedName;
            _sut.Sdk.Version = expectedVersion;

            var target = new Scope
            {
                Sdk =
                {
                    Name = null,
                    Version = "1.0"
                }
            };

            _sut.CopyTo(target);

            Assert.Equal(expectedName, target.Sdk.Name);
            Assert.Equal(expectedVersion, target.Sdk.Version);
        }

        [Fact]
        public void CopyTo_Sdk_DoesNotCopyVersionWithoutName()
        {
            const string expectedName = "original name";
            const string expectedVersion = "original version";
            _sut.Sdk.Name = expectedName;
            _sut.Sdk.Version = expectedVersion;

            var target = new Scope
            {
                Sdk =
                {
                    Name = "some scoped name",
                    Version = null
                }
            };

            _sut.CopyTo(target);

            Assert.Equal(expectedName, target.Sdk.Name);
            Assert.Equal(expectedVersion, target.Sdk.Version);
        }

        [Fact]
        public void CopyTo_Sdk_CopiesNameAndVersion()
        {
            const string expectedName = "original name";
            const string expectedVersion = "original version";
            _sut.Sdk.Name = null;
            _sut.Sdk.Version = null;

            var target = new Scope
            {
                Sdk =
                {
                    Name = expectedName,
                    Version = expectedVersion
                }
            };

            _sut.CopyTo(target);

            Assert.Equal(expectedName, target.Sdk.Name);
            Assert.Equal(expectedVersion, target.Sdk.Version);
        }

        [Fact]
        public void CopyTo_Sdk_SourceSingle_TargetNone_CopiesIntegrations()
        {
            _sut = new Scope
            {
                Sdk = { InternalIntegrations = ImmutableList.Create("integration 1") }
            };

            var target = new Scope();

            _sut.CopyTo(target);

            Assert.Same(_sut.Sdk.InternalIntegrations, target.Sdk.InternalIntegrations);
        }

        [Fact]
        public void CopyTo_Sdk_SourceSingle_AddsIntegrations()
        {
            _sut = new Scope
            {
                Sdk = { InternalIntegrations = ImmutableList.Create("integration 1") }
            };

            var target = new Scope
            {
                Sdk = { InternalIntegrations = ImmutableList.Create("integration 2") }
            };

            _sut.CopyTo(target);

            Assert.Equal(2, target.Sdk.InternalIntegrations.Count);
        }

        [Fact]
        public void CopyTo_Sdk_SourceNone_TargetSingle_DoesNotModifyTarget()
        {
            var expected = ImmutableList.Create("integration");

            var target = new Scope
            {
                Sdk = { InternalIntegrations = expected }
            };

            _sut.CopyTo(target);

            Assert.Equal(expected, target.Sdk.InternalIntegrations);
        }
    }
}
