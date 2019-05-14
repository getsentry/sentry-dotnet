using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Xunit;

namespace Sentry.Protocol.Tests
{
    public class ScopeExtensionsTests
    {
        private class Fixture
        {
            public IScopeOptions ScopeOptions { get; set; } = Substitute.For<IScopeOptions>();

            public Fixture()
            {
                ScopeOptions.BeforeBreadcrumb.Returns(null as Func<Breadcrumb, Breadcrumb>);
            }
            public BaseScope GetSut() => new BaseScope(ScopeOptions);
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void HasUser_DefaultScope_ReturnsFalse()
        {
            var sut = _fixture.GetSut();
            Assert.False(sut.HasUser());
        }

        [Fact]
        public void HasUser_NullUser_ReturnsFalse()
        {
            var sut = _fixture.GetSut();
            sut.User = null;
            Assert.False(sut.HasUser());
        }

        [Fact]
        public void HasUser_EmptyUser_ReturnsFalse()
        {
            var sut = _fixture.GetSut();
            sut.User = new User();
            Assert.False(sut.HasUser());
        }

        [Fact]
        public void HasUser_UserWithId_ReturnsTrue()
        {
            var sut = _fixture.GetSut();
            sut.User.Id = "test";
            Assert.True(sut.HasUser());
        }

        [Fact]
        public void HasUser_UserWithUserName_ReturnsTrue()
        {
            var sut = _fixture.GetSut();
            sut.User.Username = "test";
            Assert.True(sut.HasUser());
        }

        [Fact]
        public void HasUser_UserWithIpAddress_ReturnsTrue()
        {
            var sut = _fixture.GetSut();
            sut.User.IpAddress = "test";
            Assert.True(sut.HasUser());
        }

        [Fact]
        public void HasUser_UserWithEmail_ReturnsTrue()
        {
            var sut = _fixture.GetSut();
            sut.User.Email = "test";
            Assert.True(sut.HasUser());
        }

        [Fact]
        public void HasUser_UserWithOther_ReturnsTrue()
        {
            var sut = _fixture.GetSut();
            sut.User.Other.Add("other", "val");
            Assert.True(sut.HasUser());
        }

        [Fact]
        public void HasUser_UserWithEmptyOther_ReturnsFalse()
        {
            var sut = _fixture.GetSut();
            sut.User.Other = new Dictionary<string, string>();
            Assert.False(sut.HasUser());
        }

        [Fact]
        public void SetFingerprint_NullArgument_ReplacesCurrentWithNull()
        {
            var sut = _fixture.GetSut();
            sut.InternalFingerprint = Enumerable.Empty<string>();

            sut.SetFingerprint(null);

            Assert.Null(sut.InternalFingerprint);
        }

        [Fact]
        public void SetFingerprint_NewFingerprint_ReplacesCurrent()
        {
            var sut = _fixture.GetSut();
            sut.InternalFingerprint = new[] { "to be dropped" };

            var expectedFingerprint = new[] { "fingerprint" };

            sut.SetFingerprint(expectedFingerprint);

            Assert.Equal(expectedFingerprint, sut.InternalFingerprint);
        }

        [Fact]
        public void SetExtra_FirstExtra_NewDictionary()
        {
            var sut = _fixture.GetSut();
            var expectedExtra = new Dictionary<string, object>
            {
                {"expected Extra", new object()}
            };

            sut.SetExtra(expectedExtra.Keys.Single(), expectedExtra.Values.Single());

            Assert.Equal(expectedExtra, sut.InternalExtra);
        }

        [Fact]
        public void SetExtra_SecondExtra_AddedToDictionary()
        {
            var originalExtra = new ConcurrentDictionary<string, object>();
            originalExtra.TryAdd("original", new object());
            var sut = _fixture.GetSut();
            sut.InternalExtra = originalExtra;

            var expectedExtra = new Dictionary<string, object>
            {
                {"expected", "extra" }
            };

            sut.SetExtra(expectedExtra.Keys.Single(), expectedExtra.Values.Single());

            Assert.Equal(originalExtra.First().Value, sut.InternalExtra[originalExtra.Keys.First()]);
            Assert.Equal(expectedExtra.First().Value, sut.InternalExtra[expectedExtra.Keys.First()]);
        }

        [Fact]
        public void SetExtras_FirstExtra_NewDictionary()
        {
            var sut = _fixture.GetSut();
            var expectedExtra = new Dictionary<string, object>
            {
                {"expected Extra", new object()}
            };

            sut.SetExtras(expectedExtra);

            Assert.Equal(expectedExtra, sut.InternalExtra);
        }

        [Fact]
        public void SetExtras_SecondExtra_AddedToDictionary()
        {
            var originalExtra = new ConcurrentDictionary<string, object>();
            originalExtra.TryAdd("original", new object());

            var sut = _fixture.GetSut();
            sut.InternalExtra = originalExtra;

            var expectedExtra = new Dictionary<string, object>
            {
                {"expected", "extra" }
            };

            sut.SetExtras(expectedExtra);

            Assert.Equal(originalExtra.First().Value, sut.InternalExtra[originalExtra.Keys.First()]);
            Assert.Equal(expectedExtra.First().Value, sut.InternalExtra[expectedExtra.Keys.First()]);
        }

        [Fact]
        public void SetExtras_DuplicateExtra_LastSet()
        {
            var sut = _fixture.GetSut();

            var expectedExtra = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("expected", "extra"),
                // second item has dup key:
                new KeyValuePair<string, object>("expected", "extra 2"),
            };

            sut.SetExtras(expectedExtra);

            Assert.Equal(expectedExtra.Last(), sut.InternalExtra.Single());
        }

        [Fact]
        public void SetTag_FirstTag_NewDictionary()
        {
            var sut = _fixture.GetSut();

            var expectedTag = new Dictionary<string, string>
            {
                {"expected", "tag"}
            };

            sut.SetTag(expectedTag.Keys.Single(), expectedTag.Values.Single());

            Assert.Equal(expectedTag, sut.InternalTags);
        }

        [Fact]
        public void UnsetTag_NullDictionary_DoesNotCreateDictionary()
        {
            var sut = _fixture.GetSut();

            sut.UnsetTag("non existent");

            Assert.Null(sut.InternalTags);
        }

        [Fact]
        public void UnsetTag_MatchingKey_RemovesFromDictionary()
        {
            const string expected = "expected";
            var sut = _fixture.GetSut();
            sut.SetTag(expected, expected);
            sut.UnsetTag(expected);

            Assert.Empty(sut.Tags);
        }

        [Fact]
        public void SetTag_SecondTag_AddedToDictionary()
        {
            var originalTag = new ConcurrentDictionary<string, string>();
            originalTag.TryAdd("original", "value");

            var sut = _fixture.GetSut();
            sut.InternalTags = originalTag;

            var expectedTag = new Dictionary<string, string>
            {
                {"expected", "tag" }
            };

            sut.SetTag(expectedTag.Keys.Single(), expectedTag.Values.Single());

            Assert.Equal(originalTag.First().Value, sut.InternalTags[originalTag.Keys.First()]);
            Assert.Equal(expectedTag.First().Value, sut.InternalTags[expectedTag.Keys.First()]);
        }

        [Fact]
        public void SetTags_FirstTag_NewDictionary()
        {
            var sut = _fixture.GetSut();
            var expectedTag = new ConcurrentDictionary<string, string>();
            expectedTag.TryAdd("expected", "tag");

            sut.InternalTags = expectedTag;

            sut.SetTags(expectedTag);

            Assert.Equal(expectedTag, sut.InternalTags);
        }

        [Fact]
        public void SetTags_SecondTag_AddedToDictionary()
        {
            var sut = _fixture.GetSut();
            var originalTag = new ConcurrentDictionary<string, string>();
            originalTag.TryAdd("original", "value");

            sut.InternalTags = originalTag;

            var expectedTags = new Dictionary<string, string>
            {
                {"expected", "tag" }
            };

            sut.SetTags(expectedTags);

            Assert.Equal(originalTag.First().Value, sut.InternalTags[originalTag.Keys.First()]);
            Assert.Equal(expectedTags.First().Value, sut.InternalTags[expectedTags.Keys.First()]);
        }

        [Fact]
        public void SetTags_DuplicateTag_LastSet()
        {
            var sut = _fixture.GetSut();

            var expectedTag = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("expected", "tag"),
                // second item has dup key:
                new KeyValuePair<string, string>("expected", "tag 2"),
            };

            sut.SetTags(expectedTag);

            Assert.Equal(expectedTag.Last(), sut.InternalTags.Single());
        }

        [Fact]
        public void AddBreadcrumb_BeforeBreadcrumbDropsCrumb_NoBreadcrumbInEvent()
        {
            _fixture.ScopeOptions.BeforeBreadcrumb.Returns((Breadcrumb c) => null);
            var sut = _fixture.GetSut();

            sut.AddBreadcrumb("no expected");

            Assert.Null(sut.InternalBreadcrumbs);
        }

        [Fact]
        public void AddBreadcrumb_BeforeBreadcrumbNewCrumb_NewCrumbUsed()
        {
            var expected = new Breadcrumb();
            _fixture.ScopeOptions.BeforeBreadcrumb.Returns(_ => expected);
            var sut = _fixture.GetSut();

            sut.AddBreadcrumb("no expected");

            Assert.Same(expected, sut.InternalBreadcrumbs.Single());
        }

        [Fact]
        public void AddBreadcrumb_BeforeBreadcrumbReturns_SameCrumb()
        {
            var expected = new Breadcrumb();
            _fixture.ScopeOptions.BeforeBreadcrumb.Returns(c => c);
            var sut = _fixture.GetSut();

            sut.AddBreadcrumb(expected);

            Assert.Same(expected, sut.InternalBreadcrumbs.Single());
        }

        [Fact]
        public void AddBreadcrumb_WithoutOptions_NoMoreThanDefaultMaxBreadcrumbs()
        {
            _fixture.ScopeOptions = null;
            var sut = _fixture.GetSut();

            for (var i = 0; i < Constants.DefaultMaxBreadcrumbs + 1; i++)
            {
                sut.AddBreadcrumb(i.ToString());
            }

            Assert.Equal(Constants.DefaultMaxBreadcrumbs, sut.InternalBreadcrumbs.Count);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void AddBreadcrumb_WithOptions_BoundOptionsLimit(int limit)
        {
            _fixture.ScopeOptions.MaxBreadcrumbs.Returns(limit);
            var sut = _fixture.GetSut();

            for (var i = 0; i < limit + 1; i++)
            {
                sut.AddBreadcrumb(i.ToString());
            }

            Assert.Equal(limit, sut.InternalBreadcrumbs.Count);
        }

        [Fact]
        public void AddBreadcrumb_DropOldest()
        {
            const int limit = 5;

            _fixture.ScopeOptions.MaxBreadcrumbs.Returns(limit);
            var sut = _fixture.GetSut();

            for (var i = 0; i < limit + 1; i++)
            {
                sut.AddBreadcrumb(i.ToString());
            }

            // Breadcrumb 0 is dropped
            Assert.Equal("1", sut.Breadcrumbs.First().Message);
            Assert.Equal("5", sut.Breadcrumbs.Last().Message);
        }

#if HAS_VALUE_TUPLE
        [Fact]
        public void AddBreadcrumb_ValueTuple_AllArgumentsMatch()
        {
            const string expectedMessage = "original Message";
            const string expectedCategory = "original Category";
            const string expectedType = "original Type";
            var expectedData = (key: "key", value: "value");
            const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Critical;

            var sut = _fixture.GetSut();
            sut.AddBreadcrumb(
                expectedMessage,
                expectedCategory,
                expectedType,
                expectedData,
                expectedLevel);

            var actual = Assert.Single(sut.InternalBreadcrumbs);
            Assert.Equal(expectedMessage, actual.Message);
            Assert.Equal(expectedCategory, actual.Category);
            Assert.Equal(expectedType, actual.Type);
            Assert.Equal(expectedData.key, actual.Data.Single().Key);
            Assert.Equal(expectedData.value, actual.Data.Single().Value);
            Assert.Equal(expectedLevel, actual.Level);
        }
#endif

        [Fact]
        public void AddBreadcrumb_Dictionary_AllArgumentsMatch()
        {
            const string expectedMessage = "original Message";
            const string expectedCategory = "original Category";
            const string expectedType = "original Type";
            var expectedData = new Dictionary<string, string>() { { "key", "val" } };
            const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Critical;

            var sut = _fixture.GetSut();
            sut.AddBreadcrumb(
                expectedMessage,
                expectedCategory,
                expectedType,
                expectedData,
                expectedLevel);

            var actual = Assert.Single(sut.InternalBreadcrumbs);
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
            var expectedData = new Dictionary<string, string>() { { "key", "val" } };
            const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Critical;

            var sut = _fixture.GetSut();
            sut.AddBreadcrumb(
                expectedTimestamp,
                expectedMessage,
                expectedCategory,
                expectedType,
                expectedData,
                expectedLevel);

            var actual = Assert.Single(sut.InternalBreadcrumbs);
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

            var sut = _fixture.GetSut();
            sut.AddBreadcrumb(
                expectedTimestamp,
                expectedMessage);

            var actual = Assert.Single(sut.InternalBreadcrumbs);
            Assert.Equal(expectedTimestamp, actual.Timestamp);
            Assert.Equal(expectedMessage, actual.Message);
            Assert.Null(actual.Category);
            Assert.Null(actual.Type);
            Assert.Null(actual.Data);
            Assert.Equal(BreadcrumbLevel.Info, actual.Level);
        }

        [Fact]
        public void Apply_Null_Target_DoesNotThrow()
        {
            var sut = _fixture.GetSut();
            sut.Apply(null);
        }

        [Fact]
        public void Apply_Null_Source_DoesNotThrow()
        {
            BaseScope sut = null;
            sut.Apply(null);
        }

        [Fact]
        public void Apply_Fingerprint_DoesNotSetWhenNull()
        {
            var sut = _fixture.GetSut();
            sut.InternalFingerprint = null;

            const string expected = "fingerprint";
            var target = _fixture.GetSut();
            target.SetFingerprint(new[] { expected });

            sut.Apply(target);

            Assert.Equal(expected, target.InternalFingerprint.Single());
        }

        [Fact]
        public void Apply_Fingerprint_NotOnTarget_SetFromSource()
        {
            const string expected = "fingerprint";
            var sut = _fixture.GetSut();
            sut.SetFingerprint(new[] { expected });

            var target = _fixture.GetSut();

            sut.Apply(target);

            Assert.Same(sut.InternalFingerprint, target.InternalFingerprint);
        }

        [Fact]
        public void Apply_Fingerprint_OnTarget_NotOverwrittenBySource()
        {
            var sut = _fixture.GetSut();
            sut.SetFingerprint(new[] { "fingerprint" });
            var expected = sut.InternalFingerprint;

            var target = _fixture.GetSut();
            sut.SetFingerprint(new[] { "new fingerprint" });
            sut.Apply(target);

            Assert.Equal(expected.Count(), target.InternalFingerprint.Count());
        }

        [Fact]
        public void Apply_Breadcrumbs_OnTarget_MergedWithSource()
        {
            var sut = _fixture.GetSut();
            sut.AddBreadcrumb("test sut");
            var target = _fixture.GetSut();
            target.AddBreadcrumb("test target");

            sut.Apply(target);

            Assert.Equal(2, target.InternalBreadcrumbs.Count);
        }

        [Fact]
        public void Apply_Breadcrumbs_NullOnSource_TargetIsNull()
        {
            var target = _fixture.GetSut();

            var sut = _fixture.GetSut();
            sut.Apply(target);

            Assert.Null(target.InternalBreadcrumbs);
        }

        [Fact]
        public void Apply_Breadcrumbs_NotOnTarget_SetFromSource()
        {
            var sut = _fixture.GetSut();
            sut.AddBreadcrumb("test sut");

            var target = _fixture.GetSut();
            sut.Apply(target);

            Assert.Single(target.InternalBreadcrumbs);
        }

        [Fact]
        public void Apply_Breadcrumbs_NotOnSource_TargetUnmodified()
        {
            var sut = _fixture.GetSut();
            sut.AddBreadcrumb("test target");
            var expected = sut.InternalBreadcrumbs;

            var target = _fixture.GetSut();
            sut.Apply(target);

            Assert.Equal(expected.Count, target.InternalBreadcrumbs.Count);
        }

        [Fact]
        public void Apply_Extra_OnTarget_MergedWithSource()
        {
            var sut = _fixture.GetSut();
            sut.SetExtra("sut", "sut");

            var target = _fixture.GetSut();
            target.SetExtra("target", "target");
            sut.Apply(target);

            Assert.Equal(2, target.InternalExtra.Count);
        }

        [Fact]
        public void Apply_Extra_NullOnSource_TargetIsNull()
        {
            var sut = _fixture.GetSut();

            var target = _fixture.GetSut();
            sut.Apply(target);

            Assert.Null(target.InternalExtra);
        }

        [Fact]
        public void Apply_Extra_ConflictKey_KeepsTarget()
        {
            const string conflictingKey = "conflict";
            const string expectedValue = "expected";
            var sut = _fixture.GetSut();
            sut.SetExtra(conflictingKey, "sut");
            var target = _fixture.GetSut();
            target.SetExtra(conflictingKey, expectedValue);

            sut.Apply(target);

            Assert.Single(target.InternalExtra);
            Assert.Equal(expectedValue, target.InternalExtra[conflictingKey]);
        }

        [Fact]
        public void Apply_Extra_NotOnTarget_SetFromSource()
        {
            var sut = _fixture.GetSut();
            sut.SetExtra("sut", "sut");

            var target = _fixture.GetSut();
            sut.Apply(target);

            Assert.Single(target.InternalExtra);
        }

        [Fact]
        public void Apply_Extra_NotOnSource_TargetUnmodified()
        {
            var target = _fixture.GetSut();
            target.SetExtra("target", "target");
            var expected = target.InternalExtra;

            var sut = _fixture.GetSut();
            sut.Apply(target);

            Assert.Same(expected, target.InternalExtra);
        }

        [Fact]
        public void Apply_Tags_OnTarget_MergedWithSource()
        {
            var sut = _fixture.GetSut();
            sut.SetTag("sut", "sut");
            var target = _fixture.GetSut();
            target.SetTag("target", "target");

            sut.Apply(target);

            Assert.Equal(2, target.InternalTags.Count);
        }

        [Fact]
        public void Apply_Tag_NullOnSource_TargetIsNull()
        {
            var sut = _fixture.GetSut();
            var target = _fixture.GetSut();

            sut.Apply(target);

            Assert.Null(target.InternalTags);
        }

        [Fact]
        public void Apply_Tags_ConflictKey_KeepsTarget()
        {
            const string conflictingKey = "conflict";
            const string expectedValue = "expected";
            var sut = _fixture.GetSut();
            sut.SetTag(conflictingKey, "sut");
            var target = _fixture.GetSut();
            target.SetTag(conflictingKey, expectedValue);

            sut.Apply(target);

            Assert.Single(target.InternalTags);
            Assert.Equal(expectedValue, target.InternalTags[conflictingKey]);
        }

        [Fact]
        public void Apply_Tags_NotOnTarget_SetFromSource()
        {
            var sut = _fixture.GetSut();
            sut.SetTag("sut", "sut");

            var target = _fixture.GetSut();
            sut.Apply(target);

            Assert.Single(target.InternalTags);
        }

        [Fact]
        public void Apply_Tags_NotOnSource_TargetUnmodified()
        {
            var sut = _fixture.GetSut();
            var target = _fixture.GetSut();
            target.SetTag("target", "target");
            var expected = target.InternalTags;

            sut.Apply(target);

            Assert.Same(expected, target.InternalTags);
        }

        [Fact]
        public void Apply_Contexts_KnownType_App_InstanceCloned()
        {
            var sut = _fixture.GetSut();
            sut.Contexts.App.Name = "name";
            var target = _fixture.GetSut();

            sut.Apply(target);

            Assert.NotSame(sut.Contexts.App, target.Contexts.App);
            Assert.Equal("name", target.Contexts.App.Name);
        }

        [Fact]
        public void Apply_Contexts_KnownType_Browser_InstanceCloned()
        {
            var sut = _fixture.GetSut();
            sut.Contexts.Browser.Name = "name";
            var target = _fixture.GetSut();

            sut.Apply(target);

            Assert.NotSame(sut.Contexts.Browser, target.Contexts.Browser);
            Assert.Equal("name", target.Contexts.Browser.Name);
        }

        [Fact]
        public void Apply_Contexts_KnownType_Device_InstanceCloned()
        {
            var sut = _fixture.GetSut();
            sut.Contexts.Device.Name = "name";
            var target = _fixture.GetSut();

            sut.Apply(target);

            Assert.NotSame(sut.Contexts.Device, target.Contexts.Device);
            Assert.Equal("name", target.Contexts.Device.Name);
        }

        [Fact]
        public void Apply_Contexts_KnownType_OperatingSystem_InstanceCloned()
        {
            var sut = _fixture.GetSut();
            sut.Contexts.OperatingSystem.Name = "name";
            var target = _fixture.GetSut();

            sut.Apply(target);

            Assert.NotSame(sut.Contexts.OperatingSystem, target.Contexts.OperatingSystem);
            Assert.Equal("name", target.Contexts.OperatingSystem.Name);
        }

        [Fact]
        public void Apply_Contexts_KnownType_Runtime_InstanceCloned()
        {
            var sut = _fixture.GetSut();
            sut.Contexts.Runtime.Name = "name";
            var target = _fixture.GetSut();

            sut.Apply(target);

            Assert.NotSame(sut.Contexts.Runtime, target.Contexts.Runtime);
            Assert.Equal("name", target.Contexts.Runtime.Name);
        }

        [Fact]
        public void Apply_Contexts_OnTarget_MergedWithSource()
        {
            var sut = _fixture.GetSut();
            sut.Contexts["sut"] = "sut";
            var target = _fixture.GetSut();
            target.Contexts["target"] = "target";

            sut.Apply(target);

            Assert.Equal(2, target.Contexts.Count);
        }

        [Fact]
        public void Apply_Contexts_NullOnSource_TargetIsNull()
        {
            var sut = _fixture.GetSut();
            var target = _fixture.GetSut();

            sut.Apply(target);

            Assert.Null(target.InternalContexts);
        }

        [Fact]
        public void Apply_Contexts_ConflictKey_KeepsTarget()
        {
            const string conflictingKey = "conflict";
            const string expectedValue = "expected";
            var sut = _fixture.GetSut();
            sut.Contexts[conflictingKey] = "sut";
            var target = _fixture.GetSut();
            target.Contexts[conflictingKey] = expectedValue;

            sut.Apply(target);

            Assert.Single(target.Contexts);
            Assert.Equal(expectedValue, target.Contexts[conflictingKey]);
        }

        [Fact]
        public void Apply_Contexts_NotOnTarget_SetFromSource()
        {
            var sut = _fixture.GetSut();
            sut.Contexts["target"] = "target";

            var target = _fixture.GetSut();
            sut.Apply(target);

            Assert.Single(target.Contexts);
        }

        [Fact]
        public void Apply_Contexts_NotOnSource_TargetUnmodified()
        {
            var target = _fixture.GetSut();
            target.Contexts["target"] = "target";
            var expected = target.Contexts;

            var sut = _fixture.GetSut();
            sut.Apply(target);

            Assert.Equal(expected, target.Contexts);
        }

        [Fact]
        public void Apply_Sdk_DoesNotCopyNameWithoutVersion()
        {
            const string expectedName = "original name";
            const string expectedVersion = "original version";
            var sut = _fixture.GetSut();
            sut.Sdk.Name = expectedName;
            sut.Sdk.Version = expectedVersion;

            var target = _fixture.GetSut();
            target.Sdk.Name = null;
            target.Sdk.Version = "1.0";

            sut.Apply(target);

            Assert.Equal(expectedName, target.Sdk.Name);
            Assert.Equal(expectedVersion, target.Sdk.Version);
        }

        [Fact]
        public void Apply_Sdk_DoesNotCopyVersionWithoutName()
        {
            const string expectedName = "original name";
            const string expectedVersion = "original version";
            var sut = _fixture.GetSut();
            sut.Sdk.Name = expectedName;
            sut.Sdk.Version = expectedVersion;

            var target = _fixture.GetSut();
            target.Sdk.Name = "some scoped name";
            target.Sdk.Version = null;

            sut.Apply(target);

            Assert.Equal(expectedName, target.Sdk.Name);
            Assert.Equal(expectedVersion, target.Sdk.Version);
        }

        [Fact]
        public void Apply_Sdk_CopiesNameAndVersion()
        {
            const string expectedName = "original name";
            const string expectedVersion = "original version";
            var sut = _fixture.GetSut();
            sut.Sdk.Name = null;
            sut.Sdk.Version = null;

            var target = _fixture.GetSut();
            target.Sdk.Name = expectedName;
            target.Sdk.Version = expectedVersion;

            sut.Apply(target);

            Assert.Equal(expectedName, target.Sdk.Name);
            Assert.Equal(expectedVersion, target.Sdk.Version);
        }

        [Fact]
        public void Apply_Sdk_SourceSingle_TargetNone_CopiesPackage()
        {
            var sut = _fixture.GetSut();

            sut.Sdk.AddPackage("nuget:Sentry.Extensions.Logging", "2.0.0-preview10");

            var target = new BaseScope(null);

            sut.Apply(target);

            Assert.Equal(sut.Sdk.InternalPackages, target.Sdk.InternalPackages);
        }

        [Fact]
        public void Apply_Sdk_SourceSingle_AddsIntegrations()
        {
            var sut = _fixture.GetSut();

            sut.Sdk.AddPackage("nuget:Sentry.Extensions.Logging", "2.0.0-preview10");

            var target = new BaseScope(null);
            sut.Sdk.AddPackage("nuget:Sentry.AspNetCore", "1.0.0");

            sut.Apply(target);

            Assert.Equal(2, target.Sdk.InternalPackages.Count);
        }

        [Fact]
        public void Apply_Sdk_SourceNone_TargetSingle_DoesNotModifyTarget()
        {
            var sut = _fixture.GetSut();
            var target = new BaseScope(null);
            target.Sdk.AddPackage("nuget:Sentry", "1.0");
            var expected = target.Sdk.Packages.Single();

            sut.Apply(target);

            Assert.Same(expected, target.Sdk.InternalPackages.Single());
        }

        [Fact]
        public void Apply_Environment_Null()
        {
            var sut = _fixture.GetSut();
            sut.Environment = null;

            var target = _fixture.GetSut();
            sut.Apply(target);

            Assert.Null(target.Environment);
        }

        [Fact]
        public void Apply_Environment_NotOnTarget_SetFromSource()
        {
            const string expected = "env";

            var sut = _fixture.GetSut();
            sut.Environment = expected;
            var target = _fixture.GetSut();
            sut.Apply(target);

            Assert.Equal(expected, target.Environment);
        }

        [Fact]
        public void Apply_Environment_OnTarget_NotOverwritten()
        {
            const string expected = "env";
            var sut = _fixture.GetSut();
            var target = _fixture.GetSut();
            target.Environment = expected;

            sut.Environment = "other";
            sut.Apply(target);

            Assert.Equal(expected, target.Environment);
        }

        [Fact]
        public void Apply_Transaction_Null()
        {
            var sut = _fixture.GetSut();
            sut.Transaction = null;

            var target = _fixture.GetSut();
            sut.Apply(target);

            Assert.Null(target.Transaction);
        }

        [Fact]
        public void Apply_Transaction_NotOnTarget_SetFromSource()
        {
            const string expected = "transaction";

            var sut = _fixture.GetSut();
            sut.Transaction = expected;
            var target = _fixture.GetSut();
            sut.Apply(target);

            Assert.Equal(expected, target.Transaction);
        }

        [Fact]
        public void Apply_Transaction_OnTarget_NotOverwritten()
        {
            const string expected = "transaction";
            var sut = _fixture.GetSut();
            var target = _fixture.GetSut();
            target.Transaction = expected;

            sut.Transaction = "other";
            sut.Apply(target);

            Assert.Equal(expected, target.Transaction);
        }

        [Fact]
        public void Apply_Level_Null()
        {
            var sut = _fixture.GetSut();
            sut.Level = null;

            var target = _fixture.GetSut();
            sut.Apply(target);

            Assert.Null(target.Level);
        }

        [Fact]
        public void Apply_Level_NotOnTarget_SetFromSource()
        {
            const SentryLevel expected = SentryLevel.Fatal;

            var sut = _fixture.GetSut();
            sut.Level = expected;
            var target = _fixture.GetSut();
            sut.Apply(target);

            Assert.Equal(expected, target.Level);
        }

        [Fact]
        public void Apply_Level_OnTarget_NotOverwritten()
        {
            const SentryLevel expected = SentryLevel.Fatal;
            var sut = _fixture.GetSut();
            var target = _fixture.GetSut();
            target.Level = expected;

            sut.Level = SentryLevel.Info;
            sut.Apply(target);

            Assert.Equal(expected, target.Level);
        }

        [Fact]
        public void Apply_User_NullOnSource_TargetIsNull()
        {
            var sut = _fixture.GetSut();
            var target = _fixture.GetSut();

            sut.Apply(target);

            Assert.Null(target.InternalUser);
        }

        [Fact]
        public void Apply_User_NotSameReference()
        {
            var sut = _fixture.GetSut();
            var target = _fixture.GetSut();

            sut.User = new User();
            sut.Apply(target);

            Assert.NotSame(sut.User, target.User);
        }

        [Fact]
        public void Apply_User_OnTarget_MergedWithSource()
        {
            var sut = _fixture.GetSut();
            var target = _fixture.GetSut();
            target.User.Email = "target";

            sut.User.Id = "sut";
            sut.Apply(target);

            Assert.Equal("sut", target.User.Id);
            Assert.Equal("target", target.User.Email);
        }

        [Fact]
        public void Apply_User_NotOnTarget_SetFromSource()
        {
            var sut = _fixture.GetSut();
            sut.User.Id = "Id";
            sut.User.Email = "Email";
            sut.User.IpAddress = "IpAddress";
            sut.User.Username = "Username";

            var target = _fixture.GetSut();
            sut.Apply(target);

            Assert.Equal("Id", target.User.Id);
            Assert.Equal("Email", target.User.Email);
            Assert.Equal("IpAddress", target.User.IpAddress);
            Assert.Equal("Username", target.User.Username);
        }

        [Fact]
        public void Apply_User_BothOnSourceAndTarget_TargetUnmodified()
        {
            var target = _fixture.GetSut();
            target.User.Id = "Id";
            target.User.Email = "Email";
            target.User.IpAddress = "IpAddress";
            target.User.Username = "Username";

            var sut = _fixture.GetSut();
            sut.User.Id = "sut Id";
            sut.User.Email = "sut Email";
            sut.User.IpAddress = "sut IpAddress";
            sut.User.Username = "sut Username";

            sut.Apply(target);

            Assert.Equal("Id", target.User.Id);
            Assert.Equal("Email", target.User.Email);
            Assert.Equal("IpAddress", target.User.IpAddress);
            Assert.Equal("Username", target.User.Username);
        }


        [Fact]
        public void Apply_Request_NullOnSource_TargetIsNull()
        {
            var target = _fixture.GetSut();

            var sut = _fixture.GetSut();
            sut.Apply(target);

            Assert.Null(target.InternalUser);
        }

        [Fact]
        public void Apply_Request_NotSameReference()
        {
            var target = _fixture.GetSut();

            var sut = _fixture.GetSut();
            sut.Request = new Request
            {
                Method = "method"
            };
            sut.Apply(target);

            Assert.NotSame(sut.User, target.User);
        }

        [Fact]
        public void Apply_Request_OnTarget_MergedWithSource()
        {
            var target = _fixture.GetSut();
            target.Request.Env.Add("InternalEnv", "Env");
            target.Request.Headers.Add("InternalHeaders", "Headers");
            target.Request.Other.Add("InternalOther", "Other");

            var sut = _fixture.GetSut();
            sut.Request = new Request
            {
                Env = { { "sut: InternalEnv", "Env" } },
                Headers = { { "sut: InternalHeaders", "Headers" } },
                Other = { { "sut: InternalOther", "Other" } },
            };

            sut.Apply(target);

            Assert.Equal(2, target.Request.Env.Count);
            Assert.Equal(2, target.Request.Headers.Count);
            Assert.Equal(2, target.Request.Other.Count);
        }

        [Fact]
        public void Apply_Request_NotOnTarget_SetFromSource()
        {
            var sut = _fixture.GetSut();
            sut.Request = new Request
            {
                Env = { { "sut: InternalEnv", "Env" } },
                Headers = { { "sut: InternalHeaders", "Headers" } },
                Other = { { "sut: InternalOther", "Other" } },
                Cookies = "sut: cookies",
                Data = new object(),
                Method = "sut: method",
                QueryString = "sut: query",
                Url = "sut: /something"
            };

            var target = _fixture.GetSut();
            sut.Apply(target);

            Assert.Equal(sut.Request.Cookies, target.Request.Cookies);
            Assert.Equal(sut.Request.Url, target.Request.Url);
            Assert.Equal(sut.Request.Data, target.Request.Data);
            Assert.Equal(sut.Request.QueryString, target.Request.QueryString);
            Assert.Equal(sut.Request.Method, target.Request.Method);
            Assert.Equal(sut.Request.InternalOther, target.Request.InternalOther);
            Assert.Equal(sut.Request.InternalHeaders, target.Request.InternalHeaders);
            Assert.Equal(sut.Request.InternalEnv, target.Request.InternalEnv);
        }

        [Fact]
        public void Apply_Request_BothOnSourceAndTarget_TargetUnmodified()
        {
            var targetData = new object();
            var target = _fixture.GetSut();
            var request = new Request
            {
                Cookies = "cookies",
                Data = targetData,
                Method = "method",
                QueryString = "query",
                Url = "/something"
            };
            target.Request = request;

            var sut = _fixture.GetSut();
            sut.Request = new Request
            {
                Cookies = "sut: cookies",
                Data = new object(),
                Method = "sut: method",
                QueryString = "sut: query",
                Url = "sut: /something"
            };

            sut.Apply(target);

            Assert.Equal("cookies", target.Request.Cookies);
            Assert.Equal("/something", target.Request.Url);
            Assert.Equal(targetData, target.Request.Data);
            Assert.Equal("query", target.Request.QueryString);
            Assert.Equal("method", target.Request.Method);
        }
    }
}
