#if ANDROID
namespace Sentry.Tests.Platforms.Android;

public class SentrySdkTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void CreateBeforeSendCallback_NoEnabledFilter_ReturnsNull(bool enabled, bool hasFilter)
    {
        var options = new SentryOptions();
        options.Android.SuppressSegfaults = false;
        options.Native.EnableBeforeSend = enabled;
        if (hasFilter)
        {
            options.SetBeforeSend((evt, _) => evt);
        }
        using var javaOptions = new JavaSdk.SentryOptions();

        SentrySdk.CreateBeforeSendCallback(options, javaOptions).Should().BeNull();
    }

    [Fact]
    public void CreateBeforeSendCallback_SuppressionWithDisabledFilter_ReturnsUnconvertedEventWithoutCallingFilter()
    {
        var options = new SentryOptions();
        options.Android.SuppressSegfaults = true;
        options.Native.EnableBeforeSend = false;
        var called = false;
        options.SetBeforeSend((evt, _) =>
        {
            called = true;
            return evt;
        });
        using var javaOptions = new JavaSdk.SentryOptions();
        using var callback = SentrySdk.CreateBeforeSendCallback(options, javaOptions);
        using var evt = new JavaSdk.SentryEvent
        {
            EventId = new JavaSdk.Protocol.SentryId(new string('z', 32))
        };
        using var hint = new JavaSdk.Hint();

        var result = callback!.Execute(evt, hint);

        called.Should().BeFalse();
        result.Should().BeSameAs(evt);
    }

    [Fact]
    public void CreateBeforeSendCallback_BeforeSendThrows_ReturnsNullAndLogsError()
    {
        // Arrange
        var exception = new InvalidOperationException("callback failed");
        var logger = new InMemoryDiagnosticLogger();
        var options = new SentryOptions { Debug = true, DiagnosticLogger = logger };
        options.Native.EnableBeforeSend = true;
        options.SetBeforeSend((SentryEvent _, SentryHint _) => throw exception);
        options.Android.SuppressSegfaults = false;
        using var javaOptions = new JavaSdk.SentryOptions();
        using var callback = SentrySdk.CreateBeforeSendCallback(options, javaOptions);
        using var evt = new JavaSdk.SentryEvent();
        using var hint = new JavaSdk.Hint();

        // Act
        var result = callback!.Execute(evt, hint);

        // Assert
        result.Should().BeNull();
        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == SentryLevel.Error &&
            entry.Exception == exception &&
            entry.Message == "Android BeforeSend callback failed.");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ApplyDataCollection_NotConfigured_ForwardsOnlySendDefaultPii(bool sendDefaultPii)
    {
        var options = new SentryOptions { SendDefaultPii = sendDefaultPii };
        using var javaOptions = new JavaSdk.SentryOptions();

        SentrySdk.ApplyDataCollection(options, javaOptions);

#pragma warning disable CS0618
        javaOptions.SendDefaultPii.Should().Be(sendDefaultPii);
#pragma warning restore CS0618
        javaOptions.DataCollection.IsExplicitlyConfigured.Should().BeFalse();
        javaOptions.DataCollectionResolver.IsUserInfo.Should().Be(sendDefaultPii);
    }

    [Fact]
    public void ApplyDataCollection_ConfiguredWithDefaults_SetsEveryMappedCategory()
    {
        var options = new SentryOptions();
        options.ConfigureDataCollection(_ => { });
        using var javaOptions = new JavaSdk.SentryOptions();

        SentrySdk.ApplyDataCollection(options, javaOptions);

        var native = javaOptions.DataCollection;
        native.IsExplicitlyConfigured.Should().BeTrue();
        native.UserInfo.BooleanValue().Should().BeTrue();
        AssertBehavior(native.Cookies, "DENY_LIST");
        AssertBehavior(native.UrlQueryParams, "DENY_LIST");
        AssertBehavior(native.GetHttpHeaders().Request, "DENY_LIST");
        AssertBehavior(native.GetHttpHeaders().Response, "DENY_LIST");
        native.HttpBodies.Select(bodyType => bodyType.Name()).Should().BeEquivalentTo(
            "INCOMING_REQUEST", "OUTGOING_REQUEST", "INCOMING_RESPONSE", "OUTGOING_RESPONSE");
        native.GetGraphql().Document.BooleanValue().Should().BeTrue();
        native.GetGraphql().Variables.BooleanValue().Should().BeTrue();
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void ApplyDataCollection_Configured_SetsUserInfoExplicitly(bool userInfo, bool sendDefaultPii)
    {
        var options = new SentryOptions { SendDefaultPii = sendDefaultPii };
        options.ConfigureDataCollection(dataCollection => dataCollection.UserInfo = userInfo);
        using var javaOptions = new JavaSdk.SentryOptions();

        SentrySdk.ApplyDataCollection(options, javaOptions);

        javaOptions.DataCollection.UserInfo.BooleanValue().Should().Be(userInfo);
        javaOptions.DataCollectionResolver.IsUserInfo.Should().Be(userInfo);
    }

    [Theory]
    [InlineData(KeyValueCollectionMode.Off, "OFF")]
    [InlineData(KeyValueCollectionMode.DenyList, "DENY_LIST")]
    [InlineData(KeyValueCollectionMode.AllowList, "ALLOW_LIST")]
    public void ApplyDataCollection_KeyValueBehavior_MapsModeAndTerms(KeyValueCollectionMode mode, string expectedMode)
    {
        var behavior = mode switch
        {
            KeyValueCollectionMode.Off => KeyValueCollectionBehavior.Off,
            KeyValueCollectionMode.DenyList => KeyValueCollectionBehavior.DenyList("x-secret", "token"),
            _ => KeyValueCollectionBehavior.AllowList("x-request-id", "page")
        };
        var options = new SentryOptions();
        options.ConfigureDataCollection(dataCollection =>
        {
            dataCollection.Cookies = behavior;
            dataCollection.UrlQueryParams = behavior;
            dataCollection.HttpHeaders.Request = behavior;
            dataCollection.HttpHeaders.Response = behavior;
        });
        using var javaOptions = new JavaSdk.SentryOptions();

        SentrySdk.ApplyDataCollection(options, javaOptions);

        var native = javaOptions.DataCollection;
        AssertBehavior(native.Cookies, expectedMode, behavior.Terms);
        AssertBehavior(native.UrlQueryParams, expectedMode, behavior.Terms);
        AssertBehavior(native.GetHttpHeaders().Request, expectedMode, behavior.Terms);
        AssertBehavior(native.GetHttpHeaders().Response, expectedMode, behavior.Terms);
    }

    [Fact]
    public void ApplyDataCollection_DistinctCategories_MapsEachToItsNativeCounterpart()
    {
        var options = new SentryOptions();
        options.ConfigureDataCollection(dataCollection =>
        {
            dataCollection.Cookies = KeyValueCollectionBehavior.Off;
            dataCollection.UrlQueryParams = KeyValueCollectionBehavior.AllowList("page");
            dataCollection.HttpHeaders.Request = KeyValueCollectionBehavior.DenyList("x-secret");
            dataCollection.HttpHeaders.Response = KeyValueCollectionBehavior.AllowList("x-request-id");
            dataCollection.GraphQL.Document = false;
            dataCollection.GraphQL.Variables = true;
        });
        using var javaOptions = new JavaSdk.SentryOptions();

        SentrySdk.ApplyDataCollection(options, javaOptions);

        var resolver = javaOptions.DataCollectionResolver;
        AssertBehavior(resolver.Cookies, "OFF");
        AssertBehavior(resolver.UrlQueryParams, "ALLOW_LIST", ["page"]);
        AssertBehavior(resolver.HttpRequestHeaders, "DENY_LIST", ["x-secret"]);
        AssertBehavior(resolver.HttpResponseHeaders, "ALLOW_LIST", ["x-request-id"]);
        resolver.IsGraphqlDocument.Should().BeFalse();
        resolver.IsGraphqlVariables.Should().BeTrue();
    }

    [Theory]
    [InlineData(HttpBodyTypes.None)]
    [InlineData(HttpBodyTypes.IncomingRequest)]
    [InlineData(HttpBodyTypes.OutgoingRequest | HttpBodyTypes.IncomingResponse)]
    [InlineData(HttpBodyTypes.OutgoingResponse)]
    [InlineData(HttpBodyTypes.All)]
    public void ApplyDataCollection_HttpBodies_MapsEachBodyType(HttpBodyTypes bodies)
    {
        var options = new SentryOptions();
        options.ConfigureDataCollection(dataCollection => dataCollection.HttpBodies = bodies);
        using var javaOptions = new JavaSdk.SentryOptions();

        SentrySdk.ApplyDataCollection(options, javaOptions);

        var resolver = javaOptions.DataCollectionResolver;
        resolver.IsIncomingRequestBody.Should().Be(bodies.HasFlag(HttpBodyTypes.IncomingRequest));
        resolver.IsOutgoingRequestBody.Should().Be(bodies.HasFlag(HttpBodyTypes.OutgoingRequest));
        resolver.IsIncomingResponseBody.Should().Be(bodies.HasFlag(HttpBodyTypes.IncomingResponse));
        resolver.IsOutgoingResponseBody.Should().Be(bodies.HasFlag(HttpBodyTypes.OutgoingResponse));
    }

    [Fact]
    public void ApplyDataCollection_Configured_KeepsNativeOnlyCategories()
    {
        var options = new SentryOptions();
        options.ConfigureDataCollection(_ => { });
        using var javaOptions = new JavaSdk.SentryOptions();
        javaOptions.DataCollection.FilePaths = Java.Lang.Boolean.False;
        javaOptions.DataCollection.DatabaseQueryData = Java.Lang.Boolean.False;

        SentrySdk.ApplyDataCollection(options, javaOptions);

        javaOptions.DataCollectionResolver.IsFilePaths.Should().BeFalse();
        javaOptions.DataCollectionResolver.IsDatabaseQueryData.Should().BeFalse();
    }

    [Fact]
    public void ApplyDataCollection_Configured_LeavesSessionReplayUntouched()
    {
        var options = new SentryOptions();
        options.ConfigureDataCollection(dataCollection =>
        {
            dataCollection.UserInfo = false;
            dataCollection.Cookies = KeyValueCollectionBehavior.Off;
            dataCollection.UrlQueryParams = KeyValueCollectionBehavior.Off;
            dataCollection.HttpHeaders.Request = KeyValueCollectionBehavior.Off;
            dataCollection.HttpHeaders.Response = KeyValueCollectionBehavior.Off;
            dataCollection.HttpBodies = HttpBodyTypes.None;
            dataCollection.GraphQL.Document = false;
            dataCollection.GraphQL.Variables = false;
        });
        using var javaOptions = new JavaSdk.SentryOptions();
        var replay = javaOptions.SessionReplay;
        replay.SessionSampleRate = (Java.Lang.Double)1.0;
        replay.OnErrorSampleRate = (Java.Lang.Double)1.0;
        replay.NetworkDetailAllowUrls = ["https://api.example.com"];
        replay.NetworkRequestHeaders = ["Authorization"];
        replay.NetworkResponseHeaders = ["Set-Cookie"];
        replay.SetMaskAllText(false);
        replay.SetMaskAllImages(false);
        var expected = ReplaySnapshot(replay);

        SentrySdk.ApplyDataCollection(options, javaOptions);

        ReplaySnapshot(javaOptions.SessionReplay).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void TryAddIgnoredExceptionForType_ExistingThrowableClass_AddsToIgnoredExceptionsForType()
    {
        using var javaOptions = new JavaSdk.SentryOptions();

        SentrySdk.TryAddIgnoredExceptionForType(javaOptions, "java/lang/RuntimeException");

        javaOptions.IgnoredExceptionsForType.Should().Contain(Java.Lang.Class.ForName("java.lang.RuntimeException"));
    }

    [Fact]
    public void TryAddIgnoredExceptionForType_MissingClass_DoesNotThrowOrAdd()
    {
        using var javaOptions = new JavaSdk.SentryOptions();

        SentrySdk.TryAddIgnoredExceptionForType(javaOptions, "non/existent/JavaProxyThrowable");

        javaOptions.IgnoredExceptionsForType.Should().BeEmpty();
    }

    private static void AssertBehavior(
        JavaSdk.KeyValueCollectionBehavior behavior, string expectedMode, IEnumerable<string> expectedTerms = null)
    {
        behavior.Should().NotBeNull();
        behavior.GetMode().Name().Should().Be(expectedMode);
        behavior.Terms.Should().Equal(expectedTerms ?? []);
    }

    private static object ReplaySnapshot(JavaSdk.SentryReplayOptions replay) => new
    {
        SessionSampleRate = replay.SessionSampleRate?.DoubleValue(),
        OnErrorSampleRate = replay.OnErrorSampleRate?.DoubleValue(),
        replay.NetworkCaptureBodies,
        NetworkDetailAllowUrls = replay.NetworkDetailAllowUrls.ToList(),
        NetworkDetailDenyUrls = replay.NetworkDetailDenyUrls.ToList(),
        NetworkRequestHeaders = replay.NetworkRequestHeaders.ToList(),
        NetworkResponseHeaders = replay.NetworkResponseHeaders.ToList(),
        MaskViewClasses = replay.MaskViewClasses.OrderBy(name => name).ToList(),
        UnmaskViewClasses = replay.UnmaskViewClasses.OrderBy(name => name).ToList()
    };
}
#endif
