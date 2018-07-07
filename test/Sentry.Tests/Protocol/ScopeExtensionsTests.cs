using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NSubstitute;
using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Protocol
{
    public class ScopeExtensionsTests
    {
        private Scope _sut = new Scope();

        [Fact]
        public void AddBreadcrumb_WithoutOptions_NoMoreThanDefaultMaxBreadcrumbs()
        {
            var scope = new Scope();

            for (var i = 0; i < Constants.DefaultMaxBreadcrumbs + 1; i++)
            {
                scope.AddBreadcrumb(i.ToString());
            }

            Assert.Equal(Constants.DefaultMaxBreadcrumbs, scope.InternalBreadcrumbs.Count);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void AddBreadcrumb_WithOptions_BoundOptionsLimit(int limit)
        {
            var options = Substitute.For<IScopeOptions>();
            options.MaxBreadcrumbs.Returns(limit);

            var scope = new Scope(options);

            for (var i = 0; i < limit + 1; i++)
            {
                scope.AddBreadcrumb(i.ToString());
            }

            Assert.Equal(limit, scope.InternalBreadcrumbs.Count);
        }

        [Fact]
        public void AddBreadcrumb_DropOldest()
        {
            const int limit = 5;
            var options = Substitute.For<IScopeOptions>();
            options.MaxBreadcrumbs.Returns(limit);

            var scope = new Scope(options);

            for (var i = 0; i < limit + 1; i++)
            {
                scope.AddBreadcrumb(i.ToString());
            }

            // Breadcrumb 0 is dropped
            Assert.Equal("1", scope.Breadcrumbs.First().Message);
            Assert.Equal("5", scope.Breadcrumbs.Last().Message);
        }

        [Fact]
        public void AddBreadcrumb_ValueTuple_AllArgumentsMatch()
        {
            const string expectedMessage = "original Message";
            const string expectedCategory = "original Category";
            const string expectedType = "original Type";
            var expectedData = (key: "key", value: "value");
            const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Critical;

            _sut.AddBreadcrumb(
                expectedMessage,
                expectedCategory,
                expectedType,
                expectedData,
                expectedLevel);

            var actual = Assert.Single(_sut.InternalBreadcrumbs);
            Assert.Equal(expectedMessage, actual.Message);
            Assert.Equal(expectedCategory, actual.Category);
            Assert.Equal(expectedType, actual.Type);
            Assert.Equal(expectedData.key, actual.Data.Single().Key);
            Assert.Equal(expectedData.value, actual.Data.Single().Value);
            Assert.Equal(expectedLevel, actual.Level);
        }

        [Fact]
        public void AddBreadcrumb_Dictionary_AllArgumentsMatch()
        {
            const string expectedMessage = "original Message";
            const string expectedCategory = "original Category";
            const string expectedType = "original Type";
            var expectedData = new Dictionary<string, string>() { { "key", "val" } };
            const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Critical;

            _sut.AddBreadcrumb(
                expectedMessage,
                expectedCategory,
                expectedType,
                expectedData,
                expectedLevel);

            var actual = Assert.Single(_sut.InternalBreadcrumbs);
            Assert.Equal(expectedMessage, actual.Message);
            Assert.Equal(expectedCategory, actual.Category);
            Assert.Equal(expectedType, actual.Type);
            Assert.Equal(expectedData.Single().Key, actual.Data.Single().Key);
            Assert.Equal(expectedData.Single().Value, actual.Data.Single().Value);
            Assert.Equal(expectedLevel, actual.Level);
        }

        [Fact]
        public void AddBreadcrumb_ImmutableDictionary_AllArgumentsMatch()
        {
            var expectedTimestamp = DateTimeOffset.MaxValue;
            var clock = Substitute.For<ISystemClock>();
            clock.GetUtcNow().Returns(expectedTimestamp);
            const string expectedMessage = "original Message";
            const string expectedCategory = "original Category";
            const string expectedType = "original Type";
            var expectedData = ImmutableDictionary<string, string>.Empty.Add("key", "val");
            const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Critical;

            _sut.AddBreadcrumb(
                clock,
                expectedMessage,
                expectedCategory,
                expectedType,
                expectedData,
                expectedLevel);

            var actual = Assert.Single(_sut.InternalBreadcrumbs);
            Assert.Equal(expectedTimestamp, actual.Timestamp);
            Assert.Equal(expectedMessage, actual.Message);
            Assert.Equal(expectedCategory, actual.Category);
            Assert.Equal(expectedType, actual.Type);
            Assert.Equal(expectedData.Single().Key, actual.Data.Single().Key);
            Assert.Equal(expectedData.Single().Value, actual.Data.Single().Value);
            Assert.Equal(expectedLevel, actual.Level);
        }

        [Fact]
        public void AddBreadcrumb_OnlyRequiredArguments_ExpectedDefaults()
        {
            var expectedTimestamp = DateTimeOffset.MaxValue;
            var clock = Substitute.For<ISystemClock>();
            clock.GetUtcNow().Returns(expectedTimestamp);
            const string expectedMessage = "original Message";

            _sut.AddBreadcrumb(
                clock,
                expectedMessage);

            var actual = Assert.Single(_sut.InternalBreadcrumbs);
            Assert.Equal(expectedTimestamp, actual.Timestamp);
            Assert.Equal(expectedMessage, actual.Message);
            Assert.Null(actual.Category);
            Assert.Null(actual.Type);
            Assert.Null(actual.Data);
            Assert.Equal(BreadcrumbLevel.Info, actual.Level);
        }

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
