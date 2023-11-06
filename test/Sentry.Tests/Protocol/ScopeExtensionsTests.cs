namespace Sentry.Tests.Protocol;

public class ScopeExtensionsTests
{
    private class Fixture
    {
        public SentryOptions ScopeOptions { get; set; } = new();

        public Scope GetSut() => new(ScopeOptions);
    }

    private readonly Fixture _fixture = new();

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
        sut.User = null!;
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
    public void HasUser_UserWithEmail_ReturnsTrue()
    {
        var sut = _fixture.GetSut();
        sut.User.Email = "test";
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
    public void HasUser_UserWithSegment_ReturnsTrue()
    {
        var sut = _fixture.GetSut();
        sut.User.Segment = "test";
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
    public void SetFingerprint_NewFingerprint_ReplacesCurrent()
    {
        var sut = _fixture.GetSut();
        sut.Fingerprint = new[] { "to be dropped" };

        var expectedFingerprint = new[] { "fingerprint" };

        sut.SetFingerprint(expectedFingerprint);

        Assert.Equal(expectedFingerprint, sut.Fingerprint);
    }

    [Fact]
    public void SetExtra_FirstExtra_NewDictionary()
    {
        var sut = _fixture.GetSut();
        var expectedExtra = new Dictionary<string, object>
        {
            {"expected Extra", new object()}
        };

        sut.Extra[expectedExtra.Keys.Single()] = expectedExtra.Values.Single();

        Assert.Equal(expectedExtra, sut.Extra);
    }

    [Fact]
    public void SetExtra_SecondExtra_AddedToDictionary()
    {
        var sut = _fixture.GetSut();
        sut.Extra["original"] = "foo";
        sut.Extra["another"] = "bar";

        sut.Extra.Should().BeEquivalentTo(new Dictionary<string, object>
        {
            ["original"] = "foo",
            ["another"] = "bar"
        });
    }

    [Fact]
    public void SetExtra_FirstExtraWithNullValue_NewDictionary()
    {
        var sut = _fixture.GetSut();
        var expectedExtra = new Dictionary<string, object>
        {
            {"expected Extra", null}
        };

        sut.Extra[expectedExtra.Keys.Single()] = expectedExtra.Values.Single();

        Assert.Equal(expectedExtra, sut.Extra);
    }

    [Fact]
    public void SetExtra_SecondExtraWithNullValue_AddedToDictionary()
    {
        var sut = _fixture.GetSut();
        sut.Extra["original"] = "foo";
        sut.Extra["another"] = null;

        sut.Extra.Should().BeEquivalentTo(new Dictionary<string, object>
        {
            ["original"] = "foo",
            ["another"] = null
        });
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

        Assert.Equal(expectedExtra, sut.Extra);
    }

    [Fact]
    public void SetExtras_SecondExtra_AddedToDictionary()
    {
        var sut = _fixture.GetSut();
        sut.Extra["original"] = "foo";

        var expectedExtra = new Dictionary<string, object>
        {
            {"another", "bar"}
        };

        sut.SetExtras(expectedExtra);

        sut.Extra.Should().BeEquivalentTo(new Dictionary<string, object>
        {
            ["original"] = "foo",
            ["another"] = "bar"
        });
    }

    [Fact]
    public void SetExtras_DuplicateExtra_LastSet()
    {
        var sut = _fixture.GetSut();

        var expectedExtra = new List<KeyValuePair<string, object>>
        {
            new("expected", "extra"),
            // second item has dup key:
            new("expected", "extra 2"),
        };

        sut.SetExtras(expectedExtra);

        Assert.Equal(expectedExtra.Last(), sut.Extra.Single());
    }

    [Fact]
    public void SetTag_FirstTag_NewDictionary()
    {
        var sut = _fixture.GetSut();

        var expectedTag = new Dictionary<string, string>
        {
            {"expected", "tag"}
        };

        sut.Tags[expectedTag.Keys.Single()] = expectedTag.Values.Single();

        Assert.Equal(expectedTag, sut.Tags);
    }

    [Fact]
    public void UnsetTag_MatchingKey_RemovesFromDictionary()
    {
        const string expected = "expected";
        var sut = _fixture.GetSut();
        sut.Tags[expected] = expected;
        sut.Tags.Remove(expected);

        Assert.Empty(sut.Tags);
    }

    [Fact]
    public void SetTag_SecondTag_AddedToDictionary()
    {
        var sut = _fixture.GetSut();
        sut.Tags["original"] = "value";

        var expectedTag = new Dictionary<string, string>
        {
            {"additional", "bar"}
        };

        sut.Tags[expectedTag.Keys.Single()] = expectedTag.Values.Single();

        sut.Tags.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["original"] = "value",
            ["additional"] = "bar"
        });
    }

    [Fact]
    public void SetTags_FirstTag_NewDictionary()
    {
        var expectedTags = new Dictionary<string, string>
        {
            ["expected"] = "tag"
        };

        var sut = _fixture.GetSut();

        sut.SetTags(expectedTags);

        Assert.Equal(expectedTags, sut.Tags);
    }

    [Fact]
    public void SetTags_SecondTag_AddedToDictionary()
    {
        var sut = _fixture.GetSut();
        sut.Tags["original"] = "value";

        var expectedTags = new Dictionary<string, string>
        {
            {"expected", "tag"}
        };

        sut.SetTags(expectedTags);

        sut.Tags.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["original"] = "value",
            ["expected"] = "tag"
        });
    }

    [Fact]
    public void SetTags_DuplicateTag_LastSet()
    {
        var sut = _fixture.GetSut();

        var expectedTag = new List<KeyValuePair<string, string>>
        {
            new("expected", "tag"),
            // second item has dup key:
            new("expected", "tag 2"),
        };

        sut.SetTags(expectedTag);

        Assert.Equal(expectedTag.Last(), sut.Tags.Single());
    }

    [Fact]
    public void AddBreadcrumb_BeforeBreadcrumbDropsCrumb_NoBreadcrumbInEvent()
    {
        _fixture.ScopeOptions.SetBeforeBreadcrumb((_, _) => null);
        var sut = _fixture.GetSut();

        sut.AddBreadcrumb("no expected");

        Assert.Empty(sut.Breadcrumbs);
    }

    [Fact]
    public void AddBreadcrumb_BeforeBreadcrumbNewCrumb_NewCrumbUsed()
    {
        var expected = new Breadcrumb();
        _fixture.ScopeOptions.SetBeforeBreadcrumb((_, _) => expected);
        var sut = _fixture.GetSut();

        sut.AddBreadcrumb("no expected");

        Assert.Same(expected, sut.Breadcrumbs.Single());
    }

    [Fact]
    public void AddBreadcrumb_BeforeBreadcrumbReturns_SameCrumb()
    {
        var expected = new Breadcrumb();
        _fixture.ScopeOptions.SetBeforeBreadcrumb((c, _) => c);
        var sut = _fixture.GetSut();

        sut.AddBreadcrumb(expected);

        Assert.Same(expected, sut.Breadcrumbs.Single());
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

        Assert.Equal(Constants.DefaultMaxBreadcrumbs, sut.Breadcrumbs.Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void AddBreadcrumb_WithOptions_BoundOptionsLimit(int limit)
    {
        _fixture.ScopeOptions.MaxBreadcrumbs = limit;
        var sut = _fixture.GetSut();

        for (var i = 0; i < limit + 1; i++)
        {
            sut.AddBreadcrumb(i.ToString());
        }

        Assert.Equal(limit, sut.Breadcrumbs.Count);
    }

    [Fact]
    public void AddBreadcrumb_DropOldest()
    {
        const int limit = 5;

        _fixture.ScopeOptions.MaxBreadcrumbs = limit;
        var sut = _fixture.GetSut();

        for (var i = 0; i < limit + 1; i++)
        {
            sut.AddBreadcrumb(i.ToString());
        }

        // Breadcrumb 0 is dropped
        Assert.Equal("1", sut.Breadcrumbs.First().Message);
        Assert.Equal("5", sut.Breadcrumbs.Last().Message);
    }

#if !NETFRAMEWORK
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

        var actual = Assert.Single(sut.Breadcrumbs);
        Assert.Equal(expectedMessage, actual.Message);
        Assert.Equal(expectedCategory, actual.Category);
        Assert.Equal(expectedType, actual.Type);
        Assert.Equal(expectedData.key, actual.Data?.Single().Key);
        Assert.Equal(expectedData.value, actual.Data?.Single().Value);
        Assert.Equal(expectedLevel, actual.Level);
    }
#endif

    [Fact]
    public void AddBreadcrumb_Dictionary_AllArgumentsMatch()
    {
        const string expectedMessage = "original Message";
        const string expectedCategory = "original Category";
        const string expectedType = "original Type";
        var expectedData = new Dictionary<string, string> { { "key", "val" } };
        const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Critical;

        var sut = _fixture.GetSut();
        sut.AddBreadcrumb(
            expectedMessage,
            expectedCategory,
            expectedType,
            expectedData,
            expectedLevel);

        var actual = Assert.Single(sut.Breadcrumbs);
        Assert.Equal(expectedMessage, actual.Message);
        Assert.Equal(expectedCategory, actual.Category);
        Assert.Equal(expectedType, actual.Type);
        Assert.Equal(expectedData.Single().Key, actual.Data?.Single().Key);
        Assert.Equal(expectedData.Single().Value, actual.Data?.Single().Value);
        Assert.Equal(expectedLevel, actual.Level);
    }

    [Fact]
    public void AddBreadcrumb_ImmutableDictionary_AllArgumentsMatch()
    {
        var expectedTimestamp = DateTimeOffset.MaxValue;
        const string expectedMessage = "original Message";
        const string expectedCategory = "original Category";
        const string expectedType = "original Type";
        var expectedData = new Dictionary<string, string> { { "key", "val" } };
        const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Critical;

        var sut = _fixture.GetSut();
        sut.AddBreadcrumb(
            expectedTimestamp,
            expectedMessage,
            expectedCategory,
            expectedType,
            expectedData,
            expectedLevel);

        var actual = Assert.Single(sut.Breadcrumbs);
        Assert.Equal(expectedTimestamp, actual.Timestamp);
        Assert.Equal(expectedMessage, actual.Message);
        Assert.Equal(expectedCategory, actual.Category);
        Assert.Equal(expectedType, actual.Type);
        Assert.Equal(expectedData.Single().Key, actual.Data?.Single().Key);
        Assert.Equal(expectedData.Single().Value, actual.Data?.Single().Value);
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

        var actual = Assert.Single(sut.Breadcrumbs);
        Assert.Equal(expectedTimestamp, actual.Timestamp);
        Assert.Equal(expectedMessage, actual.Message);
        Assert.Null(actual.Category);
        Assert.Null(actual.Type);
        Assert.Null(actual.Data);
        Assert.Equal(BreadcrumbLevel.Info, actual.Level);
    }

    [Fact]
    public void AddAttachment_AllArgumentsMatch()
    {
        // Arrange
        var expectedStream = Stream.Null;
        var expectedFileName = "file.txt";
        var expectedType = AttachmentType.Minidump;
        var expectedContentType = "application/octet-stream";

        var scope = _fixture.GetSut();

        // Act
        scope.AddAttachment(
            expectedStream,
            expectedFileName,
            expectedType,
            expectedContentType);

        // Assert
        var attachment = Assert.Single(scope.Attachments);

        Assert.Equal(expectedStream, attachment.Content.GetStream());
        Assert.Equal(expectedFileName, attachment.FileName);
        Assert.Equal(expectedType, attachment.Type);
        Assert.Equal(expectedContentType, attachment.ContentType);
    }

    [Fact]
    public void AddAttachment_FromStream_UnknownLength_IsDropped()
    {
        // Arrange
        var logger = new InMemoryDiagnosticLogger();
        _fixture.ScopeOptions.DiagnosticLogger = logger;
        _fixture.ScopeOptions.Debug = true;

        // Stream without length, similar to HTTP streams.
        var stream = new LengthlessStream();

        var scope = _fixture.GetSut();

        // Act
        scope.AddAttachment(stream, "example.html");

        // Assert
        Assert.Empty(scope.Attachments);
        Assert.Contains(logger.Entries, e =>
            e.Message == "Cannot evaluate the size of attachment '{0}' because the stream is not seekable." &&
            e.Args[0].ToString() == "example.html");
    }

    [Fact]
    public void AddAttachment_FromFile_ArgumentsResolvedCorrectly()
    {
        // Arrange
        using var tempDir = new TempDirectory();
        var filePath = Path.Combine(tempDir.Path, "MyFile.txt");
        File.WriteAllText(filePath, "Hello world!");

        var scope = _fixture.GetSut();

        // Act
        scope.AddAttachment(filePath);

        // Assert
        var attachment = Assert.Single(scope.Attachments);
        using var stream = attachment.Content.GetStream();

        Assert.Equal("MyFile.txt", attachment.FileName);
        Assert.Equal(12, stream.Length);
    }

    [Fact]
    public void Apply_Null_Target_DoesNotThrow()
    {
        var sut = _fixture.GetSut();
        sut.Apply(null!);
    }

    [Fact]
    public void Apply_Fingerprint_NotOnTarget_SetFromSource()
    {
        const string expected = "fingerprint";
        var sut = _fixture.GetSut();
        sut.SetFingerprint(expected);

        var target = _fixture.GetSut();

        sut.Apply(target);

        Assert.Same(sut.Fingerprint, target.Fingerprint);
    }

    [Fact]
    public void Apply_Fingerprint_OnTarget_NotOverwrittenBySource()
    {
        var sut = _fixture.GetSut();
        sut.SetFingerprint("fingerprint");
        var expected = sut.Fingerprint;

        var target = _fixture.GetSut();
        sut.SetFingerprint("new fingerprint");
        sut.Apply(target);

        Assert.Equal(expected.Count, target.Fingerprint.Count);
    }

    [Fact]
    public void Apply_Breadcrumbs_OnTarget_MergedWithSource()
    {
        var sut = _fixture.GetSut();
        sut.AddBreadcrumb("test sut");
        var target = _fixture.GetSut();
        target.AddBreadcrumb("test target");

        sut.Apply(target);

        Assert.Equal(2, target.Breadcrumbs.Count);
    }

    [Fact]
    public void Apply_Breadcrumbs_NotOnTarget_SetFromSource()
    {
        var sut = _fixture.GetSut();
        sut.AddBreadcrumb("test sut");

        var target = _fixture.GetSut();
        sut.Apply(target);

        _ = Assert.Single(target.Breadcrumbs);
    }

    [Fact]
    public void Apply_Breadcrumbs_NotOnSource_TargetUnmodified()
    {
        var sut = _fixture.GetSut();
        sut.AddBreadcrumb("test target");
        var expected = sut.Breadcrumbs;

        var target = _fixture.GetSut();
        sut.Apply(target);

        Assert.Equal(expected.Count, target.Breadcrumbs.Count);
    }

    [Fact]
    public void Apply_Extra_OnTarget_MergedWithSource()
    {
        var sut = _fixture.GetSut();
        sut.Extra["sut"] = "sut";

        var target = _fixture.GetSut();
        target.Extra["target"] = "target";
        sut.Apply(target);

        Assert.Equal(2, target.Extra.Count);
    }

    [Fact]
    public void Apply_Extra_ConflictKey_KeepsTarget()
    {
        const string conflictingKey = "conflict";
        const string expectedValue = "expected";
        var sut = _fixture.GetSut();
        sut.Extra[conflictingKey] = "sut";
        var target = _fixture.GetSut();
        target.Extra[conflictingKey] = expectedValue;

        sut.Apply(target);

        _ = Assert.Single(target.Extra);
        Assert.Equal(expectedValue, target.Extra[conflictingKey]);
    }

    [Fact]
    public void Apply_Extra_NotOnTarget_SetFromSource()
    {
        var sut = _fixture.GetSut();
        sut.Extra["sut"] = "sut";

        var target = _fixture.GetSut();
        sut.Apply(target);

        _ = Assert.Single(target.Extra);
    }

    [Fact]
    public void Apply_Extra_NotOnSource_TargetUnmodified()
    {
        var target = _fixture.GetSut();
        target.Extra["target"] = "target";
        var expected = target.Extra;

        var sut = _fixture.GetSut();
        sut.Apply(target);

        Assert.Same(expected, target.Extra);
    }

    [Fact]
    public void Apply_Tags_OnTarget_MergedWithSource()
    {
        var sut = _fixture.GetSut();
        sut.Tags["sut"] = "sut";
        var target = _fixture.GetSut();
        target.Tags["target"] = "target";

        sut.Apply(target);

        Assert.Equal(2, target.Tags.Count);
    }

    [Fact]
    public void Apply_Tags_ConflictKey_KeepsTarget()
    {
        const string conflictingKey = "conflict";
        const string expectedValue = "expected";
        var sut = _fixture.GetSut();
        sut.Tags[conflictingKey] = "sut";
        var target = _fixture.GetSut();
        target.Tags[conflictingKey] = expectedValue;

        sut.Apply(target);

        _ = Assert.Single(target.Tags);
        Assert.Equal(expectedValue, target.Tags[conflictingKey]);
    }

    [Fact]
    public void Apply_Tags_NotOnTarget_SetFromSource()
    {
        var sut = _fixture.GetSut();
        sut.Tags["sut"] = "sut";

        var target = _fixture.GetSut();
        sut.Apply(target);

        _ = Assert.Single(target.Tags);
    }

    [Fact]
    public void Apply_Tags_NotOnSource_TargetUnmodified()
    {
        var sut = _fixture.GetSut();
        var target = _fixture.GetSut();
        target.Tags["target"] = "target";
        var expected = target.Tags;

        sut.Apply(target);

        Assert.Same(expected, target.Tags);
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
    public void Apply_Contexts_ConflictKey_KeepsTarget()
    {
        const string conflictingKey = "conflict";
        const string expectedValue = "expected";
        var sut = _fixture.GetSut();
        sut.Contexts[conflictingKey] = "sut";
        var target = _fixture.GetSut();
        target.Contexts[conflictingKey] = expectedValue;

        sut.Apply(target);

        _ = Assert.Single(target.Contexts);
        Assert.Equal(expectedValue, target.Contexts[conflictingKey]);
    }

    [Fact]
    public void Apply_Contexts_NotOnTarget_SetFromSource()
    {
        var sut = _fixture.GetSut();
        sut.Contexts["target"] = "target";

        var target = _fixture.GetSut();
        sut.Apply(target);

        _ = Assert.Single(target.Contexts);
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

        var target = new SentryEvent(null);

        sut.Apply(target);

        Assert.Equal(sut.Sdk.InternalPackages, target.Sdk.InternalPackages);
    }

    [Fact]
    public void Apply_Sdk_SourceSingle_AddsIntegrations()
    {
        var sut = _fixture.GetSut();

        sut.Sdk.AddPackage("nuget:Sentry.Extensions.Logging", "2.0.0-preview10");

        var target = new SentryEvent(null);
        sut.Sdk.AddPackage("nuget:Sentry.AspNetCore", "1.0.0");

        sut.Apply(target);

        Assert.Equal(2, target.Sdk.InternalPackages.Count);
    }

    [Fact]
    public void Apply_Sdk_SourceNone_TargetSingle_DoesNotModifyTarget()
    {
        var sut = _fixture.GetSut();
        var target = new SentryEvent(null);
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
        sut.TransactionName = null;

        var target = _fixture.GetSut();
        sut.Apply(target);

        Assert.Null(target.TransactionName);
    }

    [Fact]
    public void Apply_Transaction_NotOnTarget_SetFromSource()
    {
        const string expected = "transaction";

        var sut = _fixture.GetSut();
        sut.TransactionName = expected;
        var target = _fixture.GetSut();
        sut.Apply(target);

        Assert.Equal(expected, target.TransactionName);
    }

    [Fact]
    public void Apply_Transaction_OnTarget_NotOverwritten()
    {
        const string expected = "transaction";
        var sut = _fixture.GetSut();
        var target = _fixture.GetSut();
        target.TransactionName = expected;

        sut.TransactionName = "other";
        sut.Apply(target);

        Assert.Equal(expected, target.TransactionName);
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

    [Fact]
    public void Apply_Attachments_OnTarget_MergedWithSource()
    {
        // Arrange
        var source = _fixture.GetSut();
        source.AddAttachment(Stream.Null, "file1");

        var target = _fixture.GetSut();
        target.AddAttachment(Stream.Null, "file2");

        // Act
        source.Apply(target);

        // Assert
        Assert.Equal(2, target.Attachments.Count);
    }

    [Fact]
    public void Apply_Attachments_NotOnTarget_SetFromSource()
    {
        // Arrange
        var source = _fixture.GetSut();
        source.AddAttachment(Stream.Null, "file1");

        var target = _fixture.GetSut();

        // Act
        source.Apply(target);

        // Assert
        Assert.Equal(1, target.Attachments.Count);
    }

    [Fact]
    public void Apply_Attachments_NotOnSource_TargetUnmodified()
    {
        // Arrange
        var source = _fixture.GetSut();

        var target = _fixture.GetSut();
        target.AddAttachment(Stream.Null, "file1");

        // Act
        source.Apply(target);

        // Assert
        Assert.Equal(1, target.Attachments.Count);
    }
}
