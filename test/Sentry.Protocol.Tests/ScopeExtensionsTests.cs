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
        private Scope _sut = new Scope();

        [Fact]
        public void SetFingerprint_NullArgument_ReplacesCurrentWithNull()
        {
            var scope = new Scope { InternalFingerprint = Enumerable.Empty<string>() };

            scope.SetFingerprint(null);

            Assert.Null(scope.InternalFingerprint);
        }

        [Fact]
        public void SetFingerprint_NewFingerprint_ReplacesCurrent()
        {
            var scope = new Scope { InternalFingerprint = new[] { "to be dropped" } };
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
        public void UnsetTag_NullDictionary_DoesNotCreateDictionary()
        {
            var scope = new Scope();

            scope.UnsetTag("non existent");

            Assert.Null(scope.InternalTags);
        }

        [Fact]
        public void UnsetTag_MatchingKey_RemovesFromDictionary()
        {
            const string expected = "expected";
            var scope = new Scope();
            scope.SetTag(expected, expected);
            scope.UnsetTag(expected);

            Assert.Empty(scope.Contexts);
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

#if NETCOREAPP2_1
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
#endif

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
            var expectedData = new Dictionary<string, string>() { { "key", "val" } };
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
        public void Apply_Null_Target_DoesNotThrow()
        {
            _sut.Apply(null);
        }

        [Fact]
        public void Apply_Null_Source_DoesNotThrow()
        {
            Scope sut = null;
            sut.Apply(null);
        }

        [Fact]
        public void Apply_Fingerprint_DoesNotSetWhenNull()
        {
            _sut.InternalFingerprint = null;

            const string expected = "fingerprint";
            var target = new Scope();
            target.SetFingerprint(new[] { expected });

            _sut.Apply(target);

            Assert.Equal(expected, target.InternalFingerprint.Single());
        }

        [Fact]
        public void Apply_Fingerprint_NotOnTarget_SetFromSource()
        {
            const string expected = "fingerprint";
            _sut.SetFingerprint(new[] { expected });

            var target = new Scope();

            _sut.Apply(target);

            Assert.Same(_sut.InternalFingerprint, target.InternalFingerprint);
        }

        [Fact]
        public void Apply_Fingerprint_OnTarget_NotOverwritenBySource()
        {
            var target = new Scope();
            target.SetFingerprint(new[] { "fingerprint" });
            var expected = target.InternalFingerprint;

            _sut.SetFingerprint(new[] { "new fingerprint" });
            _sut.Apply(target);

            Assert.Same(expected, target.InternalFingerprint);
        }

        [Fact]
        public void Apply_Breadcrumbs_OnTarget_MergedWithSource()
        {
            _sut.AddBreadcrumb("test sut");
            var target = new Scope();
            target.AddBreadcrumb("test target");

            _sut.Apply(target);

            Assert.Equal(2, target.InternalBreadcrumbs.Count);
        }

        [Fact]
        public void Apply_Breadcrumbs_NullOnSource_TargetIsNull()
        {
            var target = new Scope();

            _sut.Apply(target);

            Assert.Null(target.InternalBreadcrumbs);
        }

        [Fact]
        public void Apply_Breadcrumbs_NotOnTarget_SetFromSource()
        {
            _sut.AddBreadcrumb("test sut");

            var target = new Scope();
            _sut.Apply(target);

            Assert.Single(target.InternalBreadcrumbs);
        }

        [Fact]
        public void Apply_Breadcrumbs_NotOnSource_TargetUnmodified()
        {
            var target = new Scope();
            target.AddBreadcrumb("test target");
            var expected = target.InternalBreadcrumbs;

            _sut.Apply(target);

            Assert.Same(expected, target.InternalBreadcrumbs);
        }

        [Fact]
        public void Apply_Extra_OnTarget_MergedWithSource()
        {
            _sut.SetExtra("sut", "sut");
            var target = new Scope();
            target.SetExtra("target", "target");

            _sut.Apply(target);

            Assert.Equal(2, target.InternalExtra.Count);
        }

        [Fact]
        public void Apply_Extra_NullOnSource_TargetIsNull()
        {
            var target = new Scope();

            _sut.Apply(target);

            Assert.Null(target.InternalExtra);
        }

        [Fact]
        public void Apply_Extra_ConflictKey_KeepsTarget()
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
        public void Apply_Extra_NotOnTarget_SetFromSource()
        {
            _sut.SetExtra("sut", "sut");

            var target = new Scope();
            _sut.Apply(target);

            Assert.Single(target.InternalExtra);
        }

        [Fact]
        public void Apply_Extra_NotOnSource_TargetUnmodified()
        {
            var target = new Scope();
            target.SetExtra("target", "target");
            var expected = target.InternalExtra;

            _sut.Apply(target);

            Assert.Same(expected, target.InternalExtra);
        }

        [Fact]
        public void Apply_Tags_OnTarget_MergedWithSource()
        {
            _sut.SetTag("sut", "sut");
            var target = new Scope();
            target.SetTag("target", "target");

            _sut.Apply(target);

            Assert.Equal(2, target.InternalTags.Count);
        }

        [Fact]
        public void Apply_Tag_NullOnSource_TargetIsNull()
        {
            var target = new Scope();

            _sut.Apply(target);

            Assert.Null(target.InternalTags);
        }

        [Fact]
        public void Apply_Tags_ConflictKey_KeepsTarget()
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
        public void Apply_Tags_NotOnTarget_SetFromSource()
        {
            _sut.SetTag("sut", "sut");

            var target = new Scope();
            _sut.Apply(target);

            Assert.Single(target.InternalTags);
        }

        [Fact]
        public void Apply_Tags_NotOnSource_TargetUnmodified()
        {
            var target = new Scope();
            target.SetTag("target", "target");
            var expected = target.InternalTags;

            _sut.Apply(target);

            Assert.Same(expected, target.InternalTags);
        }

        [Fact]
        public void Apply_Contexts_KnownType_App_InstanceCloned()
        {
            _sut.Contexts.App.Name = "name";
            var target = new Scope();

            _sut.Apply(target);

            Assert.NotSame(_sut.Contexts.App, target.Contexts.App);
            Assert.Equal("name", target.Contexts.App.Name);
        }

        [Fact]
        public void Apply_Contexts_KnownType_Browser_InstanceCloned()
        {
            _sut.Contexts.Browser.Name = "name";
            var target = new Scope();

            _sut.Apply(target);

            Assert.NotSame(_sut.Contexts.Browser, target.Contexts.Browser);
            Assert.Equal("name", target.Contexts.Browser.Name);
        }

        [Fact]
        public void Apply_Contexts_KnownType_Device_InstanceCloned()
        {
            _sut.Contexts.Device.Name = "name";
            var target = new Scope();

            _sut.Apply(target);

            Assert.NotSame(_sut.Contexts.Device, target.Contexts.Device);
            Assert.Equal("name", target.Contexts.Device.Name);
        }

        [Fact]
        public void Apply_Contexts_KnownType_OperatingSystem_InstanceCloned()
        {
            _sut.Contexts.OperatingSystem.Name = "name";
            var target = new Scope();

            _sut.Apply(target);

            Assert.NotSame(_sut.Contexts.OperatingSystem, target.Contexts.OperatingSystem);
            Assert.Equal("name", target.Contexts.OperatingSystem.Name);
        }

        [Fact]
        public void Apply_Contexts_KnownType_Runtime_InstanceCloned()
        {
            _sut.Contexts.Runtime.Name = "name";
            var target = new Scope();

            _sut.Apply(target);

            Assert.NotSame(_sut.Contexts.Runtime, target.Contexts.Runtime);
            Assert.Equal("name", target.Contexts.Runtime.Name);
        }

        [Fact]
        public void Apply_Contexts_OnTarget_MergedWithSource()
        {
            _sut.Contexts["sut"] = "sut";
            var target = new Scope();
            target.Contexts["target"] = "target";

            _sut.Apply(target);

            Assert.Equal(2, target.Contexts.Count);
        }

        [Fact]
        public void Apply_Contexts_NullOnSource_TargetIsNull()
        {
            var target = new Scope();

            _sut.Apply(target);

            Assert.Null(target.InternalContexts);
        }

        [Fact]
        public void Apply_Contexts_ConflictKey_KeepsTarget()
        {
            const string conflictingKey = "conflict";
            const string expectedValue = "expected";
            _sut.Contexts[conflictingKey] = "sut";
            var target = new Scope();
            target.Contexts[conflictingKey] = expectedValue;

            _sut.Apply(target);

            Assert.Single(target.Contexts);
            Assert.Equal(expectedValue, target.Contexts[conflictingKey]);
        }

        [Fact]
        public void Apply_Contexts_NotOnTarget_SetFromSource()
        {
            _sut.Contexts["target"] = "target";

            var target = new Scope();
            _sut.Apply(target);

            Assert.Single(target.Contexts);
        }

        [Fact]
        public void Apply_Contexts_NotOnSource_TargetUnmodified()
        {
            var target = new Scope();
            target.Contexts["target"] = "target";
            var expected = target.Contexts;

            _sut.Apply(target);

            Assert.Equal(expected, target.Contexts);
        }

        [Fact]
        public void Apply_Sdk_DoesNotCopyNameWithoutVersion()
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
        public void Apply_Sdk_DoesNotCopyVersionWithoutName()
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
        public void Apply_Sdk_CopiesNameAndVersion()
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
        public void Apply_Sdk_SourceSingle_TargetNone_CopiesIntegrations()
        {
            _sut = new Scope();

            _sut.Sdk.AddIntegration("integration 1");

            var target = new Scope();

            _sut.Apply(target);

            Assert.Equal(_sut.Sdk.InternalIntegrations, target.Sdk.InternalIntegrations);
        }

        [Fact]
        public void Apply_Sdk_SourceSingle_AddsIntegrations()
        {
            _sut = new Scope();
            _sut.Sdk.AddIntegration("integration 1");

            var target = new Scope();
            _sut.Sdk.AddIntegration("integration 2");

            _sut.Apply(target);

            Assert.Equal(2, target.Sdk.InternalIntegrations.Count);
        }

        [Fact]
        public void Apply_Sdk_SourceNone_TargetSingle_DoesNotModifyTarget()
        {
            var expected = new ConcurrentBag<string> { "integration" };

            var target = new Scope
            {
                Sdk = { InternalIntegrations = expected }
            };

            _sut.Apply(target);

            Assert.Equal(expected, target.Sdk.InternalIntegrations);
        }

        [Fact]
        public void Apply_Environment_Null()
        {
            var target = new Scope();
            _sut.Environment = null;

            _sut.Apply(target);

            Assert.Null(target.InternalContexts);
        }

        [Fact]
        public void Apply_Environment_NotOnTarget_SetFromSource()
        {
            const string expected = "env";
            var target = new Scope();

            _sut.Environment = expected;
            _sut.Apply(target);

            Assert.Equal(expected, target.Environment);
        }

        [Fact]
        public void Apply_Environment_OnTarget_NotOverwritten()
        {
            const string expected = "env";
            var target = new Scope
            {
                Environment = expected
            };

            _sut.Environment = "other";
            _sut.Apply(target);

            Assert.Equal(expected, target.Environment);
        }

        [Fact]
        public void Apply_User_NullOnSource_TargetIsNull()
        {
            var target = new Scope();

            _sut.Apply(target);

            Assert.Null(target.InternalUser);
        }

        [Fact]
        public void Apply_User_NotSameReference()
        {
            var target = new Scope();

            _sut.User = new User();
            _sut.Apply(target);

            Assert.NotSame(_sut.User, target.User);
        }

        [Fact]
        public void Apply_User_OnTarget_MergedWithSource()
        {
            var target = new Scope();
            target.User.Email = "target";

            _sut.User.Id = "sut";
            _sut.Apply(target);

            Assert.Equal("sut", target.User.Id);
            Assert.Equal("target", target.User.Email);
        }

        [Fact]
        public void Apply_User_NotOnTarget_SetFromSource()
        {
            _sut.User.Id = "Id";
            _sut.User.Email = "Email";
            _sut.User.IpAddress = "IpAddress";
            _sut.User.Username = "Username";

            var target = new Scope();
            _sut.Apply(target);

            Assert.Equal("Id", target.User.Id);
            Assert.Equal("Email", target.User.Email);
            Assert.Equal("IpAddress", target.User.IpAddress);
            Assert.Equal("Username", target.User.Username);
        }

        [Fact]
        public void Apply_User_BothOnSourceAndTarget_TargetUnmodified()
        {
            var target = new Scope
            {
                User =
                {
                    Id = "Id",
                    Email = "Email",
                    IpAddress = "IpAddress",
                    Username = "Username"
                }
            };

            _sut.User.Id = "sut Id";
            _sut.User.Email = "sut Email";
            _sut.User.IpAddress = "sut IpAddress";
            _sut.User.Username = "sut Username";

            _sut.Apply(target);

            Assert.Equal("Id", target.User.Id);
            Assert.Equal("Email", target.User.Email);
            Assert.Equal("IpAddress", target.User.IpAddress);
            Assert.Equal("Username", target.User.Username);
        }


        [Fact]
        public void Apply_Request_NullOnSource_TargetIsNull()
        {
            var target = new Scope();

            _sut.Apply(target);

            Assert.Null(target.InternalUser);
        }

        [Fact]
        public void Apply_Request_NotSameReference()
        {
            var target = new Scope();

            _sut.Request = new Request
            {
                Method = "method"
            };
            _sut.Apply(target);

            Assert.NotSame(_sut.User, target.User);
        }

        [Fact]
        public void Apply_Request_OnTarget_MergedWithSource()
        {
            var target = new Scope
            {
                Request =
                {
                    Env = { { "InternalEnv", "Env"} },
                    Headers =  { { "InternalHeaders", "Headers" } },
                    Other =  { { "InternalOther", "Other" } },
                }
            };

            _sut.Request = new Request
            {
                Env = { { "sut: InternalEnv", "Env" } },
                Headers = { { "sut: InternalHeaders", "Headers" } },
                Other = { { "sut: InternalOther", "Other" } },
            };

            _sut.Apply(target);

            Assert.Equal(2, target.Request.Env.Count);
            Assert.Equal(2, target.Request.Headers.Count);
            Assert.Equal(2, target.Request.Other.Count);
        }

        [Fact]
        public void Apply_Request_NotOnTarget_SetFromSource()
        {
            _sut.Request = new Request
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

            var target = new Scope();
            _sut.Apply(target);

            Assert.Equal(_sut.Request.Cookies, target.Request.Cookies);
            Assert.Equal(_sut.Request.Url, target.Request.Url);
            Assert.Equal(_sut.Request.Data, target.Request.Data);
            Assert.Equal(_sut.Request.QueryString, target.Request.QueryString);
            Assert.Equal(_sut.Request.Method, target.Request.Method);
            Assert.Equal(_sut.Request.InternalOther, target.Request.InternalOther);
            Assert.Equal(_sut.Request.InternalHeaders, target.Request.InternalHeaders);
            Assert.Equal(_sut.Request.InternalEnv, target.Request.InternalEnv);
        }

        [Fact]
        public void Apply_Request_BothOnSourceAndTarget_TargetUnmodified()
        {
            var targetData = new object();
            var target = new Scope
            {
                Request =
                {
                    Cookies = "cookies",
                    Data = targetData,
                    Method = "method",
                    QueryString = "query",
                    Url = "/something"
                }
            };

            _sut.Request = new Request
            {
                Cookies = "sut: cookies",
                Data = new object(),
                Method = "sut: method",
                QueryString = "sut: query",
                Url = "sut: /something"
            };

            _sut.Apply(target);

            Assert.Equal("cookies", target.Request.Cookies);
            Assert.Equal("/something", target.Request.Url);
            Assert.Equal(targetData, target.Request.Data);
            Assert.Equal("query", target.Request.QueryString);
            Assert.Equal("method", target.Request.Method);
        }
    }
}
