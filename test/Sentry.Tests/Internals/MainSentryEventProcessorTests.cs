namespace Sentry.Tests.Internals;

public class MainSentryEventProcessorTests
{
    private class Fixture
    {
        public ISentryStackTraceFactory SentryStackTraceFactory { get; set; } = Substitute.For<ISentryStackTraceFactory>();
        public SentryOptions SentryOptions { get; set; } = new() { Release = "release-123", Distribution = "dist-123"};
        public MainSentryEventProcessor GetSut() => new(SentryOptions, () => SentryStackTraceFactory);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Process_DefaultOptions_NoUserNameSet()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();

        _ = sut.Process(evt);

        Assert.Null(evt.User.Username);
    }

    [Fact]
    public void Process_SendDefaultPiiTrueIdEnvironmentDefault_UserNameSet()
    {
        var evt = new SentryEvent();

        _fixture.SentryOptions.SendDefaultPii = true;
        var sut = _fixture.GetSut();

        _ = sut.Process(evt);

        Assert.Equal(Environment.UserName, evt.User.Username);
    }

    [Fact]
    public void EnsureThreadPoolInfoExists()
    {
        var evt = new SentryEvent();

        _fixture.SentryOptions.SendDefaultPii = true;
        var sut = _fixture.GetSut();

        _ = sut.Process(evt);
        var info = (ThreadPoolInfo)evt.Contexts[MainSentryEventProcessor.ThreadPoolInfoKey];
        Assert.NotEqual(0, info.MinWorkerThreads);
        Assert.NotEqual(0, info.MinCompletionPortThreads);
        Assert.NotEqual(0, info.MaxWorkerThreads);
        Assert.NotEqual(0, info.MaxCompletionPortThreads);
    }

#if NETCOREAPP3_1_OR_GREATER
    [Fact]
    public void EnsureMemoryInfoExists()
    {
        var evt = new SentryEvent();

        _fixture.SentryOptions.SendDefaultPii = true;
        var sut = _fixture.GetSut();

        _ = sut.Process(evt);
        var memory = (MemoryInfo)evt.Contexts[MainSentryEventProcessor.MemoryInfoKey];
        Assert.NotEqual(0, memory.TotalAvailableMemoryBytes);
        Assert.NotEqual(0, memory.HighMemoryLoadThresholdBytes);
        Assert.NotEqual(0, memory.TotalAvailableMemoryBytes);
    }
#endif

#if NET5_0_OR_GREATER
    [Fact]
    public void EnsureRuntimeIdentifierExists()
    {
        var evt = new SentryEvent();
        var sut = _fixture.GetSut();

        _ = sut.Process(evt);

        var runtime = evt.Contexts.Runtime;
        Assert.Equal(System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier, runtime.Identifier);
    }
#endif

    [Fact]
    public void Process_SendDefaultPiiTrueIdEnvironmentTrue_UserNameSet()
    {
        var evt = new SentryEvent();

        _fixture.SentryOptions.SendDefaultPii = true;
        _fixture.SentryOptions.IsEnvironmentUser = true;
        var sut = _fixture.GetSut();

        _ = sut.Process(evt);

        Assert.Equal(Environment.UserName, evt.User.Username);
    }

    [Fact]
    public void Process_SendDefaultPiiTrueAndUserIpNull_UserIpSetServerInferredIp()
    {
        //Arrange
        var evt = new SentryEvent();

        _fixture.SentryOptions.SendDefaultPii = true;
        var sut = _fixture.GetSut();

        //Act
        _ = sut.Process(evt);

        //Assert
        evt.User.IpAddress.Should().Be(Enricher.DefaultIpAddress);
    }

    [Fact]
    public void Process_SendDefaultPiiTrueAndUserIpSet_UserIpIgnoreServerInferredIp()
    {
        //Arrange
        var evt = new SentryEvent();
        var ip = "192.0.0.1";
        evt.User.IpAddress = ip;

        _fixture.SentryOptions.SendDefaultPii = true;

        var sut = _fixture.GetSut();

        //Act
        _ = sut.Process(evt);

        //Assert
        Assert.Equal(ip, evt.User.IpAddress);
    }

    [Fact]
    public void Process_SendDefaultPiiFalse_UserIpAuto()
    {
        //Arrange
        var evt = new SentryEvent();
        _fixture.SentryOptions.SendDefaultPii = false;
        var sut = _fixture.GetSut();

        //Act
        _ = sut.Process(evt);

        //Assert
        evt.User.IpAddress.Should().Be(Enricher.DefaultIpAddress);
    }

    [Fact]
    public void Process_SendDefaultPiiTrueIdEnvironmentFalse_UserNameNotSet()
    {
        var evt = new SentryEvent();
        _fixture.SentryOptions.SendDefaultPii = true;
        _fixture.SentryOptions.IsEnvironmentUser = false;

        var sut = _fixture.GetSut();
        _ = sut.Process(evt);

        Assert.Null(evt.User.Username);
    }

    [Fact]
    public void Process_DefaultOptions_NoServerNameSet()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();

        _ = sut.Process(evt);

        Assert.Null(evt.ServerName);
    }

    [Fact]
    public void Process_SendDefaultPiiTrue_ServerNameSet()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();

        _fixture.SentryOptions.SendDefaultPii = true;
        _ = sut.Process(evt);

        Assert.Equal(Environment.MachineName, evt.ServerName);
    }

    [Fact]
    public void Process_SendDefaultPiiTrueNameOnOptionAndOnEvent_ServerNameNotOverwritten()
    {
        var expectedServerName = "expected server name";
        _fixture.SentryOptions.ServerName = "Value on options doesn't take precedence over the event";
        var sut = _fixture.GetSut();
        var evt = new SentryEvent { ServerName = expectedServerName };
        _fixture.SentryOptions.SendDefaultPii = true;
        _ = sut.Process(evt);

        Assert.Equal(expectedServerName, evt.ServerName);
    }

    [Fact]
    public void Process_SendDefaultPiiTrueAndNameOnOption_ServerNameSetToOptionsValue()
    {
        var expectedServerName = "expected server name";
        _fixture.SentryOptions.ServerName = expectedServerName;
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();

        _fixture.SentryOptions.SendDefaultPii = true;
        _ = sut.Process(evt);

        Assert.Equal(expectedServerName, evt.ServerName);
    }

    [Fact]
    public void Process_SendDefaultPiiFalseAndNameOnOption_ServerNameSetToOptionsValue()
    {
        var expectedServerName = "expected server name";
        _fixture.SentryOptions.ServerName = expectedServerName;
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();

        _fixture.SentryOptions.SendDefaultPii = false;
        _ = sut.Process(evt);

        Assert.Equal(expectedServerName, evt.ServerName);
    }

    [Fact]
    public void Process_ReleaseOnOptions_SetToEvent()
    {
        const string expectedVersion = "1.0 - f4d6b23";
        _fixture.SentryOptions.Release = expectedVersion;
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();

        _ = sut.Process(evt);

        Assert.Equal(expectedVersion, evt.Release);
    }

    [Fact]
    public void Process_NoReleaseOnOptions_SameAsCachedVersion()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();

        _ = sut.Process(evt);

        Assert.Equal(sut.Release, evt.Release);
    }

    [Fact]
    public void Process_DistributionOnOptions_SetToEvent()
    {
        const string expectedVersion = "14G60";
        _fixture.SentryOptions.Distribution = expectedVersion;
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();

        _ = sut.Process(evt);

        Assert.Equal(expectedVersion, evt.Distribution);
    }

    [Fact]
    public void Process_NoDistributionOnOptions_SameAsCachedVersion()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();

        _ = sut.Process(evt);

        Assert.Equal(sut.Distribution, evt.Distribution);
    }

    [Theory]
    [InlineData(null, null)] // Missing: will get default value.
    [InlineData("", null)] // Missing: will get default value.
    [InlineData(" ", null)] // Missing: will get default value.
    [InlineData("a", "a")] // Provided: nothing will change.
    [InlineData("production", "production")] // Provided: nothing will change. (value has a lower case 'p', different to default value)
    [InlineData("aBcDe F !@#$ gHi", "aBcDe F !@#$ gHi")] // Provided: nothing will change. (Case check)
    public void Process_EnvironmentOnOptions_SetToEvent(string environment, string expectedEnvironment)
    {
        expectedEnvironment ??= Debugger.IsAttached
            ? Sentry.Internal.Constants.DebugEnvironmentSetting
            : Sentry.Internal.Constants.ProductionEnvironmentSetting;

        _fixture.SentryOptions.Environment = environment;
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();

        _ = sut.Process(evt);

        Assert.Equal(expectedEnvironment, evt.Environment);
    }

    [Theory]
    [InlineData(null, null)] // Missing: will get default value.
    [InlineData("", null)] // Missing: will get default value.
    [InlineData(" ", null)] // Missing: will get default value.
    [InlineData("a", "a")] // Provided: nothing will change.
    [InlineData("Production", "Production")] // Provided: nothing will change. (value has a upper case 'p', different to default value)
    [InlineData("aBcDe F !@#$ gHi", "aBcDe F !@#$ gHi")] // Provided: nothing will change. (Case check)
    public void Process_NoEnvironmentOnOptions_SameAsEnvironmentVariable(string environment, string expectedEnvironment)
    {
        expectedEnvironment ??= Debugger.IsAttached
            ? Sentry.Internal.Constants.DebugEnvironmentSetting
            : Sentry.Internal.Constants.ProductionEnvironmentSetting;

        _fixture.SentryOptions.Environment = null;
        _fixture.SentryOptions.FakeSettings()
            .EnvironmentVariables[Sentry.Internal.Constants.EnvironmentEnvironmentVariable] = environment;

        var sut = _fixture.GetSut();
        var evt = new SentryEvent();

        _ = sut.Process(evt);

        Assert.Equal(expectedEnvironment, evt.Environment);
    }

    [Fact]
    public void Process_NoLevelOnEvent_SetToError()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent
        {
            Level = null
        };

        _ = sut.Process(evt);

        Assert.Equal(SentryLevel.Error, evt.Level);
    }

    [Fact]
    public void Process_NoExceptionOnEvent_ExceptionProcessorsNotInvoked()
    {
        var invoked = false;

        _fixture.SentryOptions.AddExceptionProcessorProvider(() =>
        {
            invoked = true;
            return new[] { Substitute.For<ISentryEventExceptionProcessor>() };
        });
        var sut = _fixture.GetSut();

        var evt = new SentryEvent();
        _ = sut.Process(evt);

        Assert.False(invoked);
    }

    [Fact]
    public void Process_Platform_CSharp()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();
        _ = sut.Process(evt);

        Assert.Equal(Constants.Platform, evt.Platform);
    }

    [Fact]
    public void Process_Modules_NotEmpty()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();
        _ = sut.Process(evt);

        Assert.NotEmpty(evt.Modules);
    }

    [Fact]
    public void Process_Modules_IsEmpty_WhenSpecified()
    {
        _fixture.SentryOptions.ReportAssembliesMode = ReportAssembliesMode.None;

        var sut = _fixture.GetSut();
        var evt = new SentryEvent();
        _ = sut.Process(evt);

        Assert.Empty(evt.Modules);
    }

    [Fact]
    public void Process_Modules_ReportAssembliesMode_None()
    {
        _fixture.SentryOptions.ReportAssembliesMode = ReportAssembliesMode.None;
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();
        _ = sut.Process(evt);

        Assert.Empty(evt.Modules);
    }

    [Fact]
    public void Process_Modules_ReportAssembliesMode_Version()
    {
        _fixture.SentryOptions.ReportAssembliesMode = ReportAssembliesMode.Version;
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();
        _ = sut.Process(evt);

        // Don't allow any assembly with a + (sha commit is added to informational version)
        Assert.DoesNotContain(evt.Modules, x => x.Value.Contains('+'));
    }

    [Fact]
    public void Process_Modules_ReportAssembliesMode_InformationalVersion()
    {
        _fixture.SentryOptions.ReportAssembliesMode = ReportAssembliesMode.InformationalVersion;
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();
        _ = sut.Process(evt);

        // Ensure at least 1 assembly with a + (sha commit is added to informational version)
        Assert.Contains(evt.Modules, x => x.Value.Contains('+'));
    }

    [Fact]
    public void Process_SdkNameAndVersion_ToDefault()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();

        _ = sut.Process(evt);

        Assert.Equal(Sentry.Internal.Constants.SdkName, evt.Sdk.Name);
        Assert.Equal(typeof(ISentryClient).Assembly.GetNameAndVersion().Version, evt.Sdk.Version);
    }

    [Fact]
    public void Process_SdkNameAndVersion_NotModified()
    {
        const string expectedName = "TestSdk";
        const string expectedVersion = "1.0";
        var sut = _fixture.GetSut();

        var evt = new SentryEvent
        {
            Sdk =
            {
                Name = expectedName,
                Version = expectedVersion
            }
        };

        _ = sut.Process(evt);

        Assert.Equal(expectedName, evt.Sdk.Name);
        Assert.Equal(expectedVersion, evt.Sdk.Version);
    }

    [Fact]
    public void Process_AttachStacktraceTrueAndNoExceptionInEvent_CallsStacktraceFactory()
    {
        _fixture.SentryOptions.AttachStacktrace = true;
        var sut = _fixture.GetSut();

        var evt = new SentryEvent();
        _ = sut.Process(evt);

        _ = _fixture.SentryStackTraceFactory.Received(1).Create();
    }

    [Fact]
    public void Process_AttachStacktraceTrueAndExceptionInEventHasNoStackTrace_CallsStacktraceFactory()
    {
        _fixture.SentryOptions.AttachStacktrace = true;
        var sut = _fixture.GetSut();

        var evt = new SentryEvent(new Exception());
        _ = sut.Process(evt);

        _ = _fixture.SentryStackTraceFactory.Received(1).Create();
    }

    [Fact]
    public void Process_AttachStacktraceTrueAndExistentThreadInEvent_AddsNewThread()
    {
        var expected = new SentryStackTrace();
        _ = _fixture.SentryStackTraceFactory.Create(Arg.Any<Exception>()).Returns(expected);
        _fixture.SentryOptions.AttachStacktrace = true;
        var sut = _fixture.GetSut();

        Thread.CurrentThread.Name = "second";
        var evt = new SentryEvent { SentryThreads = new[] { new SentryThread { Name = "first" } } };
        _ = sut.Process(evt);

        Assert.Equal(2, evt.SentryThreads.Count());
        Assert.Equal("first", evt.SentryThreads.First().Name);
        Assert.Equal("second", evt.SentryThreads.Last().Name);
    }

    [Fact]
    public void Process_CultureInfoAndCultureInfoAreEqual_OnlyCultureInfoSet()
    {
        //Arrange
        var sut = _fixture.GetSut();
        var evt = new SentryEvent(new Exception());
        CultureInfo.CurrentCulture = new CultureInfo(1042);
        CultureInfo.CurrentUICulture = new CultureInfo(1042);

        //Act
        evt = sut.Process(evt);

        //Assert
        Assert.False(evt.Contexts.ContainsKey(MainSentryEventProcessor.CurrentUiCultureKey));
        Assert.True(evt.Contexts.ContainsKey(MainSentryEventProcessor.CultureInfoKey));
    }

    [Fact]
    public void Process_DifferentCultureInfoAndCultureUiInfo_CultureInfoAndCultureUiInfoSet()
    {
        //Arrange
        var sut = _fixture.GetSut();
        var evt = new SentryEvent(new Exception());
        CultureInfo.CurrentCulture = new CultureInfo(1041);
        CultureInfo.CurrentUICulture = new CultureInfo(1033);

        //Act
        evt = sut.Process(evt);

        //Assert
        Assert.True(evt.Contexts.ContainsKey(MainSentryEventProcessor.CurrentUiCultureKey));
        Assert.True(evt.Contexts.ContainsKey(MainSentryEventProcessor.CultureInfoKey));
    }

    [Fact]
    public void Process_DeviceTimezoneSet()
    {
        var sut = _fixture.GetSut();

        var evt = new SentryEvent();
        _ = sut.Process(evt);

        Assert.Equal(TimeZoneInfo.Local, evt.Contexts.Device.Timezone);
    }

    [Theory]
    [MemberData(nameof(CultureInfoTestCase))]
    public void Process_CultureInfo_ValuesOnContext(Action<CultureInfo> setter, Func<CultureInfo> getter, string key)
    {
        // Arrange
        var originalValue = getter();
        try
        {
            setter(CultureInfo.CreateSpecificCulture("pt-BR"));
            var sut = _fixture.GetSut();

            var evt = new SentryEvent();

            // Act
            _ = sut.Process(evt);

            // Assert
            dynamic ret = evt.Contexts[key];
#pragma warning disable IDE0058 // Expression value is never used, cannot use _ = because it'll affect the test result
            Assert.Equal(getter().Name, ret["name"]);
            Assert.Equal(getter().DisplayName, ret["display_name"]);
            Assert.Equal(getter().Calendar.GetType().Name, ret["calendar"]);
#pragma warning restore IDE0058
        }
        finally
        {
            setter(originalValue);
        }
    }

    public static TheoryData<IDictionary<string, string>, IDictionary<string, string>, IDictionary<string, string>> AppliesDefaultTagTheoryData =>
        new()
        {
            // No event tags, single default.
            {
                null, // No event tags.
                new Dictionary<string, string> { { "key-1", "value-1" } }, // 1x default tags.
                new Dictionary<string, string> { { "key-1", "value-1" } }  // No event tags, so expect just the default tags.
            },

            // No event tags, multiple defaults.
            {
                null, // No event tags.
                new Dictionary<string, string>
                {
                    { "key-1", "value-1" },
                    { "key-2", "value-2" },
                    { "key-3", "value-3" }
                },
                new Dictionary<string, string>
                {
                    { "key-1", "value-1" },
                    { "key-2", "value-2" },
                    { "key-3", "value-3" }
                }
            },

            // 1x event tags, 1x default tag (which is not the same as the event tag)
            {
                new Dictionary<string, string> { { "key-1", "value-1" } }, // 1x event tags.
                new Dictionary<string, string> { { "key-2", "value-2" } }, // 1x default tags.
                new Dictionary<string, string>
                {
                    { "key-1", "value-1" },
                    { "key-2", "value-2" }
                }
            },

            // 3x event tags, 3x default tag (which _all are the same_ as the event tags).
            // Expected: no duplicates, just the event tags.
            {
                new Dictionary<string, string>{
                    { "key-1", "value-1" },
                    { "key-2", "value-2" },
                    { "key-3", "value-3" }
                },
                new Dictionary<string, string>
                {
                    { "key-1", "value-1" }, // Note: these are the same as the event tags, above.
                    { "key-2", "value-2" },
                    { "key-3", "value-3" }
                },
                new Dictionary<string, string>
                {
                    { "key-1", "value-1" },
                    { "key-2", "value-2" },
                    { "key-3", "value-3" }
                }
            },

            // 3x event tags, 3x default tag but _we have a mix of both_
            // Expected: the unique event tags and the unique default tags. no duplicates.
            {
                new Dictionary<string, string>{
                    { "key-1", "value-1" },
                    { "key-2", "value-2" },
                    { "key-3", "value-3" }
                },
                new Dictionary<string, string>
                {
                    { "key-1", "value-1" }, // Duplicate (will be ignored).
                    { "key-4", "value-4" }, // Unique.
                    { "key-3", "value-3" }  // Duplicate (will be ignored).
                },
                new Dictionary<string, string>
                {
                    { "key-1", "value-1" }, // Event tag.
                    { "key-2", "value-2" }, // Event tag.
                    { "key-3", "value-3" }, // Event tag.
                    { "key-4", "value-4" }  // Default tag.
                }
            }
        };

    [Theory]
    [MemberData(nameof(AppliesDefaultTagTheoryData))]
    public void Process_AppliesDefaultTags(IDictionary<string, string> eventTags,
        IDictionary<string, string> defaultTags,
        IDictionary<string, string> expectedTags)
    {
        //Arrange
        var evt = new SentryEvent();

        // Any scoped tags for this event?
        if (eventTags != null)
        {
            evt.SetTags(eventTags);
        }

        foreach (var defaultTag in defaultTags)
        {
            _fixture.SentryOptions.DefaultTags[defaultTag.Key] = defaultTag.Value;
        }

        var sut = _fixture.GetSut();

        //Act
        _ = sut.Process(evt);

        //Assert
        foreach (var expectedTag in expectedTags)
        {
            Assert.Equal(expectedTag.Value, evt.Tags[expectedTag.Key]);
        }
    }

    public static IEnumerable<object[]> CultureInfoTestCase()
    {
        yield return new object[]
        {
            new Action<CultureInfo>(c => CultureInfo.CurrentUICulture = c),
            new Func<CultureInfo>(() => CultureInfo.CurrentUICulture),
            MainSentryEventProcessor.CurrentUiCultureKey
        };
        yield return new object[]
        {
            new Action<CultureInfo>(c => CultureInfo.CurrentCulture = c),
            new Func<CultureInfo>(() => CultureInfo.CurrentCulture),
            MainSentryEventProcessor.CultureInfoKey
        };
    }
}
