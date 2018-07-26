using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NSubstitute;
using Xunit;

namespace Sentry.Protocol.Tests
{
    public class ScopeExtensionsTests
    {
        private Scope _sut = new Scope();

        [Fact]
        public void SetFingerprint_NullArgument_ReplacesCurrentWithNull()
        {
            var scope = new Scope { InternalFingerprint = ImmutableList<string>.Empty };

            scope.SetFingerprint(null);

            Assert.Null(scope.InternalFingerprint);
        }

        [Fact]
        public void SetFingerprint_NewFingerprint_ReplacesCurrent()
        {
            var scope = new Scope { InternalFingerprint = ImmutableList.Create("to be dropped") };
            var expectedFingerprint = new[] { "fingerprint" };

            scope.SetFingerprint(expectedFingerprint);

            Assert.Equal(expectedFingerprint, scope.InternalFingerprint);
        }

        [Fact]
        public void SetExtra_FirstExtra_NewDictionary()
        {
            var scope = new Scope();
            var expectedExtra = new Dictionary<string, object>
            {
                {"expected Extra", new object()}
            };

            scope.SetExtra(expectedExtra.Keys.Single(), expectedExtra.Values.Single());

            Assert.Equal(expectedExtra, scope.InternalExtra);
        }

        [Fact]
        public void SetExtra_SecondExtra_AddedToDictionary()
        {
            var originalExtra = new ConcurrentDictionary<string, object>();
            originalExtra.TryAdd("original", new object());
            var scope = new Scope { InternalExtra = originalExtra };

            var expectedExtra = new Dictionary<string, object>
            {
                {"expected", "extra" }
            };

            scope.SetExtra(expectedExtra.Keys.Single(), expectedExtra.Values.Single());

            Assert.Equal(originalExtra.First().Value, scope.InternalExtra[originalExtra.Keys.First()]);
            Assert.Equal(expectedExtra.First().Value, scope.InternalExtra[expectedExtra.Keys.First()]);
        }

        [Fact]
        public void SetExtras_FirstExtra_NewDictionary()
        {
            var scope = new Scope();
            var expectedExtra = new Dictionary<string, object>
            {
                {"expected Extra", new object()}
            };

            scope.SetExtras(expectedExtra);

            Assert.Equal(expectedExtra, scope.InternalExtra);
        }

        [Fact]
        public void SetExtras_SecondExtra_AddedToDictionary()
        {
            var originalExtra = new ConcurrentDictionary<string, object>();
            originalExtra.TryAdd("original", new object());

            var scope = new Scope { InternalExtra = originalExtra };

            var expectedExtra = new Dictionary<string, object>
            {
                {"expected", "extra" }
            };

            scope.SetExtras(expectedExtra);

            Assert.Equal(originalExtra.First().Value, scope.InternalExtra[originalExtra.Keys.First()]);
            Assert.Equal(expectedExtra.First().Value, scope.InternalExtra[expectedExtra.Keys.First()]);
        }

        [Fact]
        public void SetExtras_DuplicateExtra_LastSet()
        {
            var scope = new Scope();

            var expectedExtra = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("expected", "extra"),
                // second item has dup key:
                new KeyValuePair<string, object>("expected", "extra 2"),
            };

            scope.SetExtras(expectedExtra);

            Assert.Equal(expectedExtra.Last(), scope.InternalExtra.Single());
        }

        [Fact]
        public void SetTag_FirstTag_NewDictionary()
        {
            var scope = new Scope();

            var expectedTag = new Dictionary<string, string>
            {
                {"expected", "tag"}
            };

            scope.SetTag(expectedTag.Keys.Single(), expectedTag.Values.Single());

            Assert.Equal(expectedTag, scope.InternalTags);
        }

        [Fact]
        public void SetTag_SecondTag_AddedToDictionary()
        {
            var originalTag = new ConcurrentDictionary<string, string>();
            originalTag.TryAdd("original", "value");

            var scope = new Scope { InternalTags = originalTag };

            var expectedTag = new Dictionary<string, string>
            {
                {"expected", "tag" }
            };

            scope.SetTag(expectedTag.Keys.Single(), expectedTag.Values.Single());

            Assert.Equal(originalTag.First().Value, scope.InternalTags[originalTag.Keys.First()]);
            Assert.Equal(expectedTag.First().Value, scope.InternalTags[expectedTag.Keys.First()]);
        }

        [Fact]
        public void SetTags_FirstTag_NewDictionary()
        {
            var scope = new Scope();
            var expectedTag = new Dictionary<string, string>
            {
                {"expected", "tag"}
            };

            scope.SetTags(expectedTag);

            Assert.Equal(expectedTag, scope.InternalTags);
        }

        [Fact]
        public void SetTags_SecondTag_AddedToDictionary()
        {
            var originalTag = new ConcurrentDictionary<string, string>();
            originalTag.TryAdd("original", "value");

            var scope = new Scope { InternalTags = originalTag };

            var expectedTags = new Dictionary<string, string>
            {
                {"expected", "tag" }
            };

            scope.SetTags(expectedTags);

            Assert.Equal(originalTag.First().Value, scope.InternalTags[originalTag.Keys.First()]);
            Assert.Equal(expectedTags.First().Value, scope.InternalTags[expectedTags.Keys.First()]);
        }

        [Fact]
        public void SetTags_DuplicateTag_LastSet()
        {
            var scope = new Scope();

            var expectedTag = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("expected", "tag"),
                // second item has dup key:
                new KeyValuePair<string, string>("expected", "tag 2"),
            };

            scope.SetTags(expectedTag);

            Assert.Equal(expectedTag.Last(), scope.InternalTags.Single());
        }

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
            const string expectedMessage = "original Message";
            const string expectedCategory = "original Category";
            const string expectedType = "original Type";
            var expectedData = ImmutableDictionary<string, string>.Empty.Add("key", "val");
            const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Critical;

            _sut.AddBreadcrumb(
                expectedTimestamp,
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
            const string expectedMessage = "original Message";

            _sut.AddBreadcrumb(
                expectedTimestamp,
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
        public void CopyTo_Fingerprint_DoesNotSetWhenNull()
        {
            _sut.InternalFingerprint = null;

            const string expected = "fingerprint";
            var target = new Scope();
            target.SetFingerprint(new[] { expected });

            _sut.Apply(target);

            Assert.Equal(expected, target.InternalFingerprint.Single());
        }

        [Fact]
        public void CopyTo_Fingerprint_NotOnTarget_SetFromSource()
        {
            const string expected = "fingerprint";
            _sut.SetFingerprint(new[] { expected });

            var target = new Scope();

            _sut.Apply(target);

            Assert.Same(_sut.InternalFingerprint, target.InternalFingerprint);
        }

        [Fact]
        public void CopyTo_Fingerprint_OnTarget_NotOverwritenBySource()
        {
            var target = new Scope();
            target.SetFingerprint(new[] { "fingerprint" });
            var expected = target.InternalFingerprint;

            _sut.SetFingerprint(new[] { "new fingerprint" });
            _sut.Apply(target);

            Assert.Same(expected, target.InternalFingerprint);
        }

        [Fact]
        public void CopyTo_Breadcrumbs_OnTarget_MergedWithSource()
        {
            _sut.AddBreadcrumb("test sut");
            var target = new Scope();
            target.AddBreadcrumb("test target");

            _sut.Apply(target);

            Assert.Equal(2, target.InternalBreadcrumbs.Count);
        }

        [Fact]
        public void CopyTo_Breadcrumbs_NotOnTarget_SetFromSource()
        {
            _sut.AddBreadcrumb("test sut");

            var target = new Scope();
            _sut.Apply(target);

            Assert.Single(target.InternalBreadcrumbs);
        }

        [Fact]
        public void CopyTo_Breadcrumbs_NotOnSource_TargetUnmodified()
        {
            var target = new Scope();
            target.AddBreadcrumb("test target");
            var expected = target.InternalBreadcrumbs;

            _sut.Apply(target);

            Assert.Same(expected, target.InternalBreadcrumbs);
        }

        [Fact]
        public void CopyTo_Extra_OnTarget_MergedWithSource()
        {
            _sut.SetExtra("sut", "sut");
            var target = new Scope();
            target.SetExtra("target", "target");

            _sut.Apply(target);

            Assert.Equal(2, target.InternalExtra.Count);
        }

        [Fact]
        public void CopyTo_Extra_ConflictKey_KeepsTarget()
        {
            const string conflictingKey = "conflict";
            const string expectedValue = "expected";
            _sut.SetExtra(conflictingKey, "sut");
            var target = new Scope();
            target.SetExtra(conflictingKey, expectedValue);

            _sut.Apply(target);

            Assert.Single(target.InternalExtra);
            Assert.Equal(expectedValue, target.InternalExtra[conflictingKey]);
        }

        [Fact]
        public void CopyTo_Extra_NotOnTarget_SetFromSource()
        {
            _sut.SetExtra("sut", "sut");

            var target = new Scope();
            _sut.Apply(target);

            Assert.Single(target.InternalExtra);
        }

        [Fact]
        public void CopyTo_Extra_NotOnSource_TargetUnmodified()
        {
            var target = new Scope();
            target.SetExtra("target", "target");
            var expected = target.InternalExtra;

            _sut.Apply(target);

            Assert.Same(expected, target.InternalExtra);
        }

        [Fact]
        public void CopyTo_Tags_OnTarget_MergedWithSource()
        {
            _sut.SetTag("sut", "sut");
            var target = new Scope();
            target.SetTag("target", "target");

            _sut.Apply(target);

            Assert.Equal(2, target.InternalTags.Count);
        }

        [Fact]
        public void CopyTo_Tags_ConflictKey_KeepsTarget()
        {
            const string conflictingKey = "conflict";
            const string expectedValue = "expected";
            _sut.SetTag(conflictingKey, "sut");
            var target = new Scope();
            target.SetTag(conflictingKey, expectedValue);

            _sut.Apply(target);

            Assert.Single(target.InternalTags);
            Assert.Equal(expectedValue, target.InternalTags[conflictingKey]);
        }

        [Fact]
        public void CopyTo_Tags_NotOnTarget_SetFromSource()
        {
            _sut.SetTag("sut", "sut");

            var target = new Scope();
            _sut.Apply(target);

            Assert.Single(target.InternalTags);
        }

        [Fact]
        public void CopyTo_Tags_NotOnSource_TargetUnmodified()
        {
            var target = new Scope();
            target.SetTag("target", "target");
            var expected = target.InternalTags;

            _sut.Apply(target);

            Assert.Same(expected, target.InternalTags);
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

            _sut.Apply(target);

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

            _sut.Apply(target);

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

            _sut.Apply(target);

            Assert.Equal(expectedName, target.Sdk.Name);
            Assert.Equal(expectedVersion, target.Sdk.Version);
        }

        [Fact]
        public void CopyTo_Sdk_SourceSingle_TargetNone_CopiesIntegrations()
        {
            _sut = new Scope();

            _sut.Sdk.AddIntegration("integration 1");

            var target = new Scope();

            _sut.Apply(target);

            Assert.Same(_sut.Sdk.InternalIntegrations, target.Sdk.InternalIntegrations);
        }

        [Fact]
        public void CopyTo_Sdk_SourceSingle_AddsIntegrations()
        {
            _sut = new Scope();
            _sut.Sdk.AddIntegration("integration 1");

            var target = new Scope();
            _sut.Sdk.AddIntegration("integration 2");

            _sut.Apply(target);

            Assert.Equal(2, target.Sdk.InternalIntegrations.Count);
        }

        [Fact]
        public void CopyTo_Sdk_SourceNone_TargetSingle_DoesNotModifyTarget()
        {
            var expected = new ConcurrentBag<string> { "integration" };

            var target = new Scope
            {
                Sdk = { InternalIntegrations = expected }
            };

            _sut.Apply(target);

            Assert.Equal(expected, target.Sdk.InternalIntegrations);
        }
    }
}
